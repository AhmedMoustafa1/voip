using System;
using System.Threading;
using Dissonance.Config;
using UnityEngine;

namespace Dissonance.Audio.Playback
{
    /// <summary>
    /// Plays back an ISampleProvider to an AudioSource
    /// <remarks>Uses OnAudioFilterRead, so the source it is playing back on will be whichever the filter attaches itself to.</remarks>
    /// </summary>
    /// ReSharper disable once InheritdocConsiderUsage
    public class SamplePlaybackComponent
        : MonoBehaviour
    {
        #region fields
        private static readonly Log Log = Logs.Create(LogCategory.Playback, typeof(SamplePlaybackComponent).Name);
        private static readonly TimeSpan ResetDesync = TimeSpan.FromSeconds(1);
        private static readonly float[] DesyncFixBuffer = new float[1024];

        private DesyncCalculator _desync;

        private long _totalSamplesRead;

        /// <summary>
        /// Temporary buffer to hold data read from source
        /// </summary>
        private float[] _temp;

        [CanBeNull]private AudioFileWriter _diagnosticOutput;

        /// <summary>
        /// Configure this playback component to either overwrite the input audio, or to multiply by it
        /// </summary>
        internal bool MultiplyBySource { get; set; }

        public bool HasActiveSession
        {
            get { return Session.HasValue; }
        }

        private SessionContext _lastPlayedSessionContext;
        private readonly ReaderWriterLockSlim _sessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        public SpeechSession? Session { get; private set; }

        public TimeSpan PlaybackPosition
        {
            get
            {
                var session = Session;
                if (session == null)
                    return TimeSpan.Zero;

                return TimeSpan.FromSeconds(Interlocked.Read(ref _totalSamplesRead) / (double)session.Value.OutputWaveFormat.SampleRate);
            }
        }
        public TimeSpan IdealPlaybackPosition
        {
            get
            {
                var session = Session;
                if (session == null)
                    return TimeSpan.Zero;

                return DateTime.UtcNow - session.Value.ActivationTime;
            }
        }

        public TimeSpan Desync { get { return TimeSpan.FromMilliseconds(_desync.DesyncMilliseconds); } }
        public float CorrectedPlaybackSpeed { get { return _desync.CorrectedPlaybackSpeed; } }

        private volatile float _arv;
        /// <summary>
        /// Average rectified value of the audio signal currently playing (a measure of amplitude)
        /// </summary>
        public float ARV { get { return _arv; } }
        #endregion

        public void Play(SpeechSession session)
        {
            if (Session != null)
                throw Log.CreatePossibleBugException("Attempted to play a session when one is already playing", "C4F19272-994D-4025-AAEF-37BB62685C2E");

            Log.Debug("Began playback of speech session. id={0}", session.Context.Id);

            if (DebugSettings.Instance.EnablePlaybackDiagnostics && DebugSettings.Instance.RecordFinalAudio)
            {
                var filename = string.Format("Dissonance_Diagnostics/Output_{0}_{1}_{2}", session.Context.PlayerName, session.Context.Id, DateTime.UtcNow.ToFileTime());
                Interlocked.Exchange(ref _diagnosticOutput, new AudioFileWriter(filename, session.OutputWaveFormat));
            }

            _sessionLock.EnterWriteLock();
            try
            {
                ApplyReset();
                Session = session;
            }
            finally
            {
                _sessionLock.ExitWriteLock();
            }
        }

        public void Start()
        {
            //Create a temporary buffer to hold audio. We don't know how big the buffer needs to be,
            //but this buffer is *one second long* which is way larger than we could ever need!
            _temp = new float[AudioSettings.outputSampleRate];
        }

        public void OnEnable()
        {
            Session = null;
            ApplyReset();
        }

        public void OnDisable()
        {
            Session = null;
            ApplyReset();
        }

        public void OnAudioFilterRead(float[] data, int channels)
        {
            //If there is no session, clear filter and early exit
            var maybeSession = Session;
            if (!maybeSession.HasValue)
            {
                Array.Clear(data, 0, data.Length);
                return;
            }

            _sessionLock.EnterUpgradeableReadLock();
            try
            {
                //Check if there is no session again, this time protected by a lock
                maybeSession = Session;
                if (!maybeSession.HasValue)
                {
                    Array.Clear(data, 0, data.Length);
                    return;
                }

                //Detect if the session has changed since the last call to this method, if so reset
                var session = maybeSession.Value;
                if (!session.Context.Equals(_lastPlayedSessionContext))
                {
                    _lastPlayedSessionContext = maybeSession.Value.Context;
                    ApplyReset();
                }

                //Calculate the difference between where we should be and where we are (in samples)
                _desync.Update(IdealPlaybackPosition, PlaybackPosition);

                //If necessary skip samples to bring us back in sync
                int deltaDesync, deltaSamples;
                var complete = Skip(session, _desync.DesyncMilliseconds, out deltaSamples, out deltaDesync);
                Interlocked.Add(ref _totalSamplesRead, deltaSamples);
                _desync.Skip(deltaDesync);

                //If the session wasn't completed by the skip, keep playing
                if (!complete)
                {
                    int samples;
                    float arv;
                    complete = Filter(session, data, channels, _temp, _diagnosticOutput, out arv, out samples, MultiplyBySource);
                    _arv = arv;
                    Interlocked.Add(ref _totalSamplesRead, samples);
                }

                //Clean up now that this session is complete
                if (complete)
                {
                    Log.Debug("Finished playback of speech session. id={0}", session.Context.Id);

                    //Clear the session
                    _sessionLock.EnterWriteLock();
                    try
                    {
                        Session = null;
                    }
                    finally
                    {
                        _sessionLock.ExitWriteLock();
                    }

                    //Reset the state
                    ApplyReset();

                    //Discard the diagnostic recorder if necessary
                    if (_diagnosticOutput != null)
                    {
                        _diagnosticOutput.Dispose();
                        _diagnosticOutput = null;
                    }
                }
            }
            finally
            {
                _sessionLock.ExitUpgradeableReadLock();
            }
        }

        private void ApplyReset()
        {
            Log.Debug("Resetting playback component");

            Interlocked.Exchange(ref _totalSamplesRead, 0);
            _arv = 0;
            _desync = new DesyncCalculator();
        }

        internal static bool Skip(SpeechSession session, int desyncMilliseconds, out int deltaSamples, out int deltaDesyncMilliseconds)
        {
            //We're too far behind where we ought to be to resync with speed adjustment. Skip ahead to where we should be.
            if (desyncMilliseconds > ResetDesync.TotalMilliseconds)
            {
                Log.Warn("Playback desync ({0}ms) beyond recoverable threshold; resetting stream to current time", desyncMilliseconds);

                deltaSamples = desyncMilliseconds * session.OutputWaveFormat.SampleRate / 1000;
                deltaDesyncMilliseconds = -desyncMilliseconds;

                //Read out a load of data and discard it, forcing ourselves back into sync
                //If reading completes the session exit out.
                var toRead = deltaSamples;
                while (toRead > 0)
                {
                    var count = Math.Min(toRead, DesyncFixBuffer.Length);
                    if (session.Read(new ArraySegment<float>(DesyncFixBuffer, 0, count)))
                        return true;
                    toRead -= count;
                }

                //We completed all the reads so obviously none of the reads finished the session
                return false;
            }

            //We're too far ahead of where we ought to be to resync with speed adjustment. Insert silent frames to resync
            if (desyncMilliseconds < -ResetDesync.TotalMilliseconds)
            {
                Log.Error("Playback desync ({0}ms) AHEAD beyond recoverable threshold", desyncMilliseconds);
            }

            deltaSamples = 0;
            deltaDesyncMilliseconds = 0;
            return false;
        }

        internal static bool Filter(SpeechSession session, [NotNull] float[] output, int channels, [NotNull] float[] temp, [CanBeNull]AudioFileWriter diagnosticOutput, out float arv, out int samplesRead, bool multiply)
        {
            //Read out data from source (exactly as much as we need for one channel)
            var samplesRequired = output.Length / channels;
            var complete = session.Read(new ArraySegment<float>(temp, 0, samplesRequired));

            if (diagnosticOutput != null)
                diagnosticOutput.WriteSamples(new ArraySegment<float>(temp, 0, samplesRequired));

            float accumulator = 0;

            //Step through samples, stretching them (i.e. play mono input in all output channels)
            var sampleIndex = 0;
            for (var i = 0; i < output.Length; i += channels)
            {
                //Get a single sample from the source data
                var sample = temp[sampleIndex++];

                //Accumulate the sum of the audio signal
                accumulator += Mathf.Abs(sample);

                //Copy data into all channels
                for (var c = 0; c < channels; c++)
                {
                    if (multiply)
                        output[i + c] *= sample;
                    else
                        output[i + c] = sample;
                }
            }

            arv = accumulator / output.Length;
            samplesRead = samplesRequired;

            return complete;
        }
    }
}

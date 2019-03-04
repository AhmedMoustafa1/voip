using System;
using System.Collections.Generic;
using Dissonance.Datastructures;
using Dissonance.Networking;

namespace Dissonance.Audio.Playback
{
    internal interface IJitterEstimator
    {
        float Jitter { get; }

        float Confidence { get; }
    }

    /// <summary>
    ///     Converts the sequence of stream start/stop and packet delivery events from the network into a sequence of
    ///     <see cref="SpeechSession" />.
    /// </summary>
    internal class SpeechSessionStream
        : IJitterEstimator
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Playback, typeof (SpeechSessionStream).Name);
        
        private readonly Queue<SpeechSession> _awaitingActivation;
        private readonly IVolumeProvider _volumeProvider;

        /// <summary>
        /// The time when the current head of the queue was first attempted to be dequeued
        /// </summary>
        private DateTime? _queueHeadFirstDequeueAttempt = null;

        private DecoderPipeline _active;
        private uint _currentId;

        private string _playerName;
        public string PlayerName
        {
            get { return _playerName; }
            set
            {
                if (_playerName != value)
                {
                    _playerName = value;
                    _arrivalJitterMeter.Clear();
                }
            }
        }

        private readonly WindowDeviationCalculator _arrivalJitterMeter = new WindowDeviationCalculator(128);
        float IJitterEstimator.Jitter
        {
            get { return _arrivalJitterMeter.StdDev; }
        }

        float IJitterEstimator.Confidence
        {
            get { return _arrivalJitterMeter.Confidence; }
        }
        #endregion

        public SpeechSessionStream(IVolumeProvider volumeProvider)
        {
            _volumeProvider = volumeProvider;
            _awaitingActivation = new Queue<SpeechSession>();
        }

        /// <summary>
        ///     Starts a new speech session and adds it to the queue for playback
        /// </summary>
        /// <param name="format">The frame format.</param>
        /// <param name="now">Current time, or null for DateTime.UtcNow</param>
        /// <param name="jitter">Jitter estimator, or null for this stream to estimate it's own jitter</param>
        public void StartSession(FrameFormat format, DateTime? now = null, [CanBeNull] IJitterEstimator jitter = null)
        {
            if (PlayerName == null)
                throw Log.CreatePossibleBugException("Attempted to `StartSession` but `PlayerName` is null", "0C0F3731-8D6B-43F6-87C1-33CEC7A26804");

            _active = GetOrCreateDecoderPipeline(format, _volumeProvider);

            var session = SpeechSession.Create(new SessionContext(PlayerName, unchecked(_currentId++)), jitter ?? this, _active, _active, now ?? DateTime.UtcNow);
            _awaitingActivation.Enqueue(session);

            Log.Debug("Created new speech session with buffer time of {0}ms", session.Delay.TotalMilliseconds);
        }

        /// <summary>
        /// Attempt to dequeue a session for immediate playback
        /// </summary>
        /// <param name="now">The current time (or null, to use DateTime.UtcNow)</param>
        /// <returns></returns>
        public SpeechSession? TryDequeueSession(DateTime? now = null)
        {
            var rNow = now ?? DateTime.UtcNow;

            if (_awaitingActivation.Count > 0)
            {
                //Save the time when we first saw this item at the head of the queue
                if (!_queueHeadFirstDequeueAttempt.HasValue)
                    _queueHeadFirstDequeueAttempt = rNow;

                var next = _awaitingActivation.Peek();
                if (next.TargetActivationTime < rNow)
                {
                    next.Prepare(rNow, _queueHeadFirstDequeueAttempt.Value);

                    _awaitingActivation.Dequeue();
                    _queueHeadFirstDequeueAttempt = null;

                    return next;
                }
            }

            return null;
        }

        /// <summary>
        ///     Queues an encoded audio frame for playback in the current session.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="now">The current time (or null, to use DateTime.Now)</param>
        public void ReceiveFrame(VoicePacket packet, DateTime? now = null)
        {
            if (packet.SenderPlayerId != PlayerName)
                throw Log.CreatePossibleBugException(string.Format("Attempted to deliver voice from player {0} to playback queue for player {1}", packet.SenderPlayerId, PlayerName), "F55DB7D5-621B-4F5B-8C19-700B1FBC9871");

            var delay = _active.Push(packet, now ?? DateTime.UtcNow);

            _arrivalJitterMeter.Update(delay);
        }

        /// <summary>
        ///     Stops the current session.
        /// </summary>
        /// <param name="logNoSessionError">If true and no session is currently active this method will log a warning</param>
        public void StopSession(bool logNoSessionError = true)
        {
            if (_active != null)
                _active.Stop();
            else if (logNoSessionError)
                Log.Warn(Log.PossibleBugMessage("Attempted to stop a session, but there is no active session", "6DB702AA-D683-47AA-9544-BE4857EF8160"));
        }

        #region decoder pipeline pooling
        private static readonly Dictionary<FrameFormat, ConcurrentPool<DecoderPipeline>> FreePipelines = new Dictionary<FrameFormat, ConcurrentPool<DecoderPipeline>>();

        [NotNull] private static DecoderPipeline GetOrCreateDecoderPipeline(FrameFormat format, [NotNull] IVolumeProvider volume)
        {
            if (volume == null) throw new ArgumentNullException("volume");

            ConcurrentPool<DecoderPipeline> pool;
            if (!FreePipelines.TryGetValue(format, out pool))
            {
                pool = new ConcurrentPool<DecoderPipeline>(3, () => {
                    var decoder = DecoderFactory.Create(format);

                    return new DecoderPipeline(decoder, format.FrameSize, p =>
                    {
                        p.Reset();
                        Recycle(format, p);
                    });
                });
                FreePipelines[format] = pool;
            }

            var pipeline = pool.Get();
            pipeline.Reset();

            pipeline.VolumeProvider = volume;

            return pipeline;
        }

        private static void Recycle(FrameFormat format, DecoderPipeline pipeline)
        {
            ConcurrentPool<DecoderPipeline> pool;
            if (!FreePipelines.TryGetValue(format, out pool))
                Log.Warn(Log.PossibleBugMessage("Tried to recycle a pipeline but the pool for this pipeline format does not exist", "A6212BCF-9318-4224-B69F-BA4B5A651785"));
            else
                pool.Put(pipeline);
        }
        #endregion
    }
}
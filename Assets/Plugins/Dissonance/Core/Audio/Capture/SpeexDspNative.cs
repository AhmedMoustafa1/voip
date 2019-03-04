//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Runtime.InteropServices;
//using Dissonance.Config;
//using Dissonance.Extensions;
//using NAudio.Wave;

//namespace Dissonance.Audio.Capture
//{
//    internal static class SpeexDspNative
//    {
//        private static class SpeexDspNativeMethods
//        {
//#if UNITY_IOS && !UNITY_EDITOR
//            [DllImport("__Internal")]
//#else
//            [DllImport("speexdsp", CallingConvention = CallingConvention.Cdecl)]
//#endif
//            public static extern IntPtr speex_preprocess_state_init(int frameSize, int sampleRate);

//#if UNITY_IOS && !UNITY_EDITOR
//            [DllImport("__Internal")]
//#else
//            [DllImport("speexdsp", CallingConvention = CallingConvention.Cdecl)]
//#endif
//            public static extern int speex_preprocess_ctl(IntPtr st, int id, ref int val);

//#if UNITY_IOS && !UNITY_EDITOR
//            [DllImport("__Internal")]
//#else
//            [DllImport("speexdsp", CallingConvention = CallingConvention.Cdecl)]
//#endif
//            public static extern int speex_preprocess_ctl(IntPtr st, int id, ref float val);

//#if UNITY_IOS && !UNITY_EDITOR
//            [DllImport("__Internal")]
//#else
//            [DllImport("speexdsp", CallingConvention = CallingConvention.Cdecl)]
//#endif
//            public static extern int speex_preprocess_run(IntPtr st, IntPtr ptr);

//#if UNITY_IOS && !UNITY_EDITOR
//            [DllImport("__Internal")]
//#else
//            [DllImport("speexdsp", CallingConvention = CallingConvention.Cdecl)]
//#endif
//            public static extern void speex_preprocess_state_destroy(IntPtr st);
//        }

//        private enum SpeexDspCtl
//        {
//            // ReSharper disable InconsistentNaming
//            // ReSharper disable UnusedMember.Local

//            /** Set preprocessor denoiser state */
//            SPEEX_PREPROCESS_SET_DENOISE = 0,
//            /** Get preprocessor denoiser state */
//            SPEEX_PREPROCESS_GET_DENOISE = 1,

//            /** Set preprocessor Automatic Gain Control state */
//            SPEEX_PREPROCESS_SET_AGC = 2,
//            /** Get preprocessor Automatic Gain Control state */
//            SPEEX_PREPROCESS_GET_AGC = 3,

//            ///** Set preprocessor Voice Activity Detection state */
//            //SPEEX_PREPROCESS_SET_VAD = 4,
//            ///** Get preprocessor Voice Activity Detection state */
//            //SPEEX_PREPROCESS_GET_VAD = 5,

//            /** Set preprocessor Automatic Gain Control level (float) */
//            SPEEX_PREPROCESS_SET_AGC_LEVEL = 6,
//            /** Get preprocessor Automatic Gain Control level (float) */
//            SPEEX_PREPROCESS_GET_AGC_LEVEL = 7,

//            //Dereverb is disabled in the preprocessor!
//            ///** Set preprocessor Dereverb state */
//            //SPEEX_PREPROCESS_SET_DEREVERB = 8,
//            ///** Get preprocessor Dereverb state */
//            //SPEEX_PREPROCESS_GET_DEREVERB = 9,

//            ///** Set probability required for the VAD to go from silence to voice */
//            //SPEEX_PREPROCESS_SET_PROB_START = 14,
//            ///** Get probability required for the VAD to go from silence to voice */
//            //SPEEX_PREPROCESS_GET_PROB_START = 15,

//            ///** Set probability required for the VAD to stay in the voice state (integer percent) */
//            //SPEEX_PREPROCESS_SET_PROB_CONTINUE = 16,
//            ///** Get probability required for the VAD to stay in the voice state (integer percent) */
//            //SPEEX_PREPROCESS_GET_PROB_CONTINUE = 17,

//            /** Set maximum attenuation of the noise in dB (negative number) */
//            SPEEX_PREPROCESS_SET_NOISE_SUPPRESS = 18,
//            /** Get maximum attenuation of the noise in dB (negative number) */
//            SPEEX_PREPROCESS_GET_NOISE_SUPPRESS = 19,

//            ///** Set maximum attenuation of the residual echo in dB (negative number) */
//            //SPEEX_PREPROCESS_SET_ECHO_SUPPRESS = 20,
//            ///** Get maximum attenuation of the residual echo in dB (negative number) */
//            //SPEEX_PREPROCESS_GET_ECHO_SUPPRESS = 21,

//            ///** Set maximum attenuation of the residual echo in dB when near end is active (negative number) */
//            //SPEEX_PREPROCESS_SET_ECHO_SUPPRESS_ACTIVE = 22,
//            ///** Get maximum attenuation of the residual echo in dB when near end is active (negative number) */
//            //SPEEX_PREPROCESS_GET_ECHO_SUPPRESS_ACTIVE = 23,

//            ///** Set the corresponding echo canceller state so that residual echo suppression can be performed (NULL for no residual echo suppression) */
//            //SPEEX_PREPROCESS_SET_ECHO_STATE = 24,
//            ///** Get the corresponding echo canceller state */
//            //SPEEX_PREPROCESS_GET_ECHO_STATE = 25,

//            /** Set maximal gain increase in dB/second (int32) */
//            SPEEX_PREPROCESS_SET_AGC_INCREMENT = 26,

//            /** Get maximal gain increase in dB/second (int32) */
//            SPEEX_PREPROCESS_GET_AGC_INCREMENT = 27,

//            /** Set maximal gain decrease in dB/second (int32) */
//            SPEEX_PREPROCESS_SET_AGC_DECREMENT = 28,

//            /** Get maximal gain decrease in dB/second (int32) */
//            SPEEX_PREPROCESS_GET_AGC_DECREMENT = 29,

//            /** Set maximal gain in dB (int32) */
//            SPEEX_PREPROCESS_SET_AGC_MAX_GAIN = 30,

//            /** Get maximal gain in dB (int32) */
//            SPEEX_PREPROCESS_GET_AGC_MAX_GAIN = 31,

//            /*  Can't set loudness */
//            /** Get loudness */
//            SPEEX_PREPROCESS_GET_AGC_LOUDNESS = 33,

//            /*  Can't set gain */
//            /** Get current gain (int32 percent) */
//            SPEEX_PREPROCESS_GET_AGC_GAIN = 35,

//            /*  Can't set spectrum size */
//            /** Get spectrum size for power spectrum (int32) */
//            SPEEX_PREPROCESS_GET_PSD_SIZE = 37,

//            /*  Can't set power spectrum */
//            /** Get power spectrum (int32[] of squared values) */
//            SPEEX_PREPROCESS_GET_PSD = 39,

//            /*  Can't set noise size */
//            /** Get spectrum size for noise estimate (int32)  */
//            SPEEX_PREPROCESS_GET_NOISE_PSD_SIZE = 41,

//            /*  Can't set noise estimate */
//            /** Get noise estimate (int32[] of squared values) */
//            SPEEX_PREPROCESS_GET_NOISE_PSD = 43,

//            /* Can't set speech probability */
//            /** Get speech probability in last frame (int32).  */
//            SPEEX_PREPROCESS_GET_PROB = 45,

//            /** Set preprocessor Automatic Gain Control level (int32) */
//            SPEEX_PREPROCESS_SET_AGC_TARGET = 46,
//            /** Get preprocessor Automatic Gain Control level (int32) */
//            SPEEX_PREPROCESS_GET_AGC_TARGET = 47

//            // ReSharper restore UnusedMember.Local
//            // ReSharper restore InconsistentNaming
//        }

//        /// <summary>
//        /// A preprocessor for microphone input which performs denoising and automatic gain control
//        /// </summary>
//        public sealed class Preprocessor
//            : IDisposable
//        {
//            #region fields
//            private IntPtr _preprocessor;

//            private readonly int _frameSize;
//            public int FrameSize
//            {
//                get { return _frameSize; }
//            }

//            private readonly WaveFormat _format;
//            [NotNull] public WaveFormat Format
//            {
//                get { return _format; }
//            }
//            #endregion

//            #region denoise
//            /// <summary>
//            /// Get or Set if denoise filter is enabled
//            /// </summary>
//            public bool Denoise
//            {
//                get
//                {
//                    return CTL_Int(SpeexDspCtl.SPEEX_PREPROCESS_GET_DENOISE) != 0;
//                }
//                set
//                {
//                    var input = value ? 1 : 0;
//                    CTL(SpeexDspCtl.SPEEX_PREPROCESS_SET_DENOISE, ref input);
//                }
//            }

//            /// <summary>
//            /// Get or Set maximum attenuation of the noise in dB (negative number)
//            /// </summary>
//            public int DenoiseAttenuation
//            {
//                get
//                {
//                    return CTL_Int(SpeexDspCtl.SPEEX_PREPROCESS_GET_NOISE_SUPPRESS);
//                }
//                set
//                {
//                    CTL(SpeexDspCtl.SPEEX_PREPROCESS_SET_NOISE_SUPPRESS, ref value);
//                }
//            }
//            #endregion

//            #region AGC
//            public bool AutomaticGainControl
//            {
//                get { return CTL_Int(SpeexDspCtl.SPEEX_PREPROCESS_GET_AGC) != 0; }
//                set
//                {
//                    var input = value ? 1 : 0;
//                    CTL(SpeexDspCtl.SPEEX_PREPROCESS_SET_AGC, ref input);
//                }
//            }

//            public float AutomaticGainControlLevel
//            {
//                get
//                {
//                    return CTL_Float(SpeexDspCtl.SPEEX_PREPROCESS_GET_AGC_LEVEL);
//                }
//                set
//                {
//                    CTL(SpeexDspCtl.SPEEX_PREPROCESS_SET_AGC_LEVEL, ref value);
//                }
//            }

//            public int AutomaticGainControlLevelMax
//            {
//                get
//                {
//                    return CTL_Int(SpeexDspCtl.SPEEX_PREPROCESS_GET_AGC_MAX_GAIN);
//                }
//                set
//                {
//                    CTL(SpeexDspCtl.SPEEX_PREPROCESS_SET_AGC_MAX_GAIN, ref value);
//                }
//            }

//            public int AutomaticGainControlIncrement
//            {
//                get
//                {
//                    return CTL_Int(SpeexDspCtl.SPEEX_PREPROCESS_GET_AGC_INCREMENT);
//                }
//                set
//                {
//                    CTL(SpeexDspCtl.SPEEX_PREPROCESS_SET_AGC_INCREMENT, ref value);
//                }
//            }

//            public int AutomaticGainControlDecrement
//            {
//                get
//                {
//                    return CTL_Int(SpeexDspCtl.SPEEX_PREPROCESS_GET_AGC_DECREMENT);
//                }
//                set
//                {
//                    CTL(SpeexDspCtl.SPEEX_PREPROCESS_SET_AGC_DECREMENT, ref value);
//                }
//            }

//            /// <summary>
//            /// Get the current amount of AGC applied (0-1 indicating none -> max)
//            /// </summary>
//            public float AutomaticGainControlCurrent
//            {
//                get { return CTL_Int(SpeexDspCtl.SPEEX_PREPROCESS_GET_AGC_GAIN) / 100f; }
//            }
//            #endregion

//            public Preprocessor(int frameSize, int sampleRate)
//            {
//                _frameSize = frameSize;
//                _format = new WaveFormat(1, sampleRate);

//                Reset();
//            }

//            /// <summary>
//            /// Process a frame of data captured from the microphone
//            /// </summary>
//            /// <param name="frame"></param>
//            /// <returns>Returns true iff VAD is enabled and speech is detected</returns>
//            public void Process(ArraySegment<short> frame)
//            {
//                if (frame.Count != _frameSize)
//                    throw new ArgumentException(string.Format("Incorrect frame size, expected {0} but given {1}", _frameSize, frame.Count), "frame");

//                using (var handle = frame.Pin())
//                    SpeexDspNativeMethods.speex_preprocess_run(_preprocessor, handle.Ptr);
//            }

//            public void Reset()
//            {
//                if (_preprocessor != IntPtr.Zero)
//                {
//                    SpeexDspNativeMethods.speex_preprocess_state_destroy(_preprocessor);
//                    _preprocessor = IntPtr.Zero;
//                }

//                _preprocessor = SpeexDspNativeMethods.speex_preprocess_state_init(_frameSize, _format.SampleRate);
//            }

//            #region CTL
//            private void CTL(SpeexDspCtl ctl, ref int value)
//            {
//                var code = SpeexDspNativeMethods.speex_preprocess_ctl(_preprocessor, (int)ctl, ref value);
//                if (code != 0)
//                    throw new InvalidOperationException(string.Format("Failed Speex CTL '{0}' Code='{1}'", ctl, code));
//            }

//            private void CTL(SpeexDspCtl ctl, ref float value)
//            {
//                var code = SpeexDspNativeMethods.speex_preprocess_ctl(_preprocessor, (int)ctl, ref value);
//                if (code != 0)
//                    throw new InvalidOperationException(string.Format("Failed Speex CTL '{0}' Code='{1}'", ctl, code));
//            }

//            private int CTL_Int(SpeexDspCtl ctl)
//            {
//                var result = 0;

//                var code = SpeexDspNativeMethods.speex_preprocess_ctl(_preprocessor, (int)ctl, ref result);
//                if (code != 0)
//                    throw new InvalidOperationException(string.Format("Failed Speex CTL '{0}' Code='{1}'", ctl, code));

//                return result;
//            }

//            private float CTL_Float(SpeexDspCtl ctl)
//            {
//                var result = 0f;

//                var code = SpeexDspNativeMethods.speex_preprocess_ctl(_preprocessor, (int)ctl, ref result);
//                if (code != 0)
//                    throw new InvalidOperationException(string.Format("Failed Speex CTL '{0}' Code='{1}'", ctl, code));

//                return result;
//            }
//            #endregion

//            #region disposal
//            ~Preprocessor()
//            {
//                Dispose();
//            }

//            private bool _disposed;
//            public void Dispose()
//            {
//                if (_disposed)
//                    return;

//                GC.SuppressFinalize(this);

//                if (_preprocessor != IntPtr.Zero)
//                {
//                    SpeexDspNativeMethods.speex_preprocess_state_destroy(_preprocessor);
//                    _preprocessor = IntPtr.Zero;
//                }

//                _disposed = true;
//            }
//            #endregion
//        }
//    }

//    public class Preprocessor
//        : IDisposable
//    {
//        private readonly SpeexDspNative.Preprocessor _preprocessor;
//        private readonly VoiceSettings _settings;

//        private readonly List<PropertyChangedEventHandler> _subscribed = new List<PropertyChangedEventHandler>();

//        public Preprocessor(int frameSize, int sampleRate)
//        {
//            _preprocessor = new SpeexDspNative.Preprocessor(frameSize, sampleRate);

//            _settings = VoiceSettings.Instance;

//            Bind(() => _settings.Denoise, "Denoise", v => _preprocessor.Denoise = v);
//            Bind(() => _settings.DenoiseMaxAttenuation, "DenoiseMaxAttenuation", v => _preprocessor.DenoiseAttenuation = v);

//            Bind(() => _settings.AGC, "AGC", v => _preprocessor.AutomaticGainControl = v);
//            Bind(() => _settings.AgcTargetLevel, "AgcTargetLevel", v => _preprocessor.AutomaticGainControlLevel = v);
//            Bind(() => _settings.AgcMaxGain, "AgcMaxGain", v => _preprocessor.AutomaticGainControlLevelMax = v);
//            Bind(() => _settings.AgcGainIncrement, "AgcGainIncrement", v => _preprocessor.AutomaticGainControlIncrement = v);
//            Bind(() => _settings.AgcGainDecrement, "AgcGainDecrement", v => _preprocessor.AutomaticGainControlDecrement = v);
//        }

//        private void Bind<T>(Func<T> getValue, string propertyName, Action<T> setValue)
//        {
//            //Bind for value changes in the future
//            PropertyChangedEventHandler subbed;
//            _settings.PropertyChanged += subbed = (sender, args) => {
//                if (args.PropertyName == propertyName)
//                    setValue(getValue());
//            };

//            //Save this subscription so we can *unsub* later
//            _subscribed.Add(subbed);

//            //Invoke immediately to pull the current value
//            subbed.Invoke(_settings, new PropertyChangedEventArgs(propertyName));
//        }

//        public void Process(ArraySegment<short> frame)
//        {
//            _preprocessor.Process(frame);
//        }

//        public void Reset()
//        {
//            _preprocessor.Reset();
//        }

//        public void Dispose()
//        {
//            _preprocessor.Dispose();

//            for (var i = 0; i < _subscribed.Count; i++)
//                _settings.PropertyChanged -= _subscribed[i];
//            _subscribed.Clear();
//        }
//    }
//}
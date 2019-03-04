using System;
using Dissonance.Audio.Codecs;
using Dissonance.Audio.Codecs.Identity;
using Dissonance.Audio.Codecs.Opus;
using Dissonance.Config;

namespace Dissonance.Audio.Playback
{
    internal class DecoderFactory
    {
        [NotNull] public static IVoiceDecoder Create(FrameFormat format)
        {
            switch (format.Codec)
            {
                case Codec.Identity:
                    return new IdentityDecoder(format.WaveFormat);

                //ncrunch: no coverage start (Justification: Don't want to pull opus binaries into test context)
                case Codec.Opus:
                    return new OpusDecoder(format.WaveFormat, VoiceSettings.Instance.ForwardErrorCorrection);
                //ncrunch: no coverage end

                default:
                    throw new ArgumentOutOfRangeException("format", "Codec not supported");
            }
        }
    }
}
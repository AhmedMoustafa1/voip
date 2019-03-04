namespace Dissonance.Networking.Client
{
    internal struct VoicePacketOptions
    {
        public const int ChannelSessionRange = 4;
        public byte ChannelSession
        {
            get { return (byte)(_bitfield & 3); }
        }

        private readonly byte _bitfield;
        public byte Bitfield { get { return _bitfield; } }

        private VoicePacketOptions(byte bitfield)
        {
            _bitfield = bitfield;
        }

        public static VoicePacketOptions Unpack(byte bitfield)
        {
            return new VoicePacketOptions(
                bitfield
            );
        }

        public static VoicePacketOptions Pack(byte channelSession)
        {
            //8 bits of metadata (e.g. quality, codec)
            // 0 0 - Unused
            // 0 0 - Unused
            // 0 0 - Unused
            // 0 0 - channel Session number (as a 2 bit number)
            var bitfield = (byte)(
                0 << 6 |
                0 << 4 |
                0 << 2 |
                (channelSession % 4)
            );

            return new VoicePacketOptions(
                bitfield
            );
        }
    }
}

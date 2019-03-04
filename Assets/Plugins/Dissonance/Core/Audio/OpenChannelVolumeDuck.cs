using Dissonance.Audio.Playback;
using Dissonance.Config;

namespace Dissonance.Audio
{
    /// <summary>
    /// Automatically ducks the volume when a channel is open
    /// </summary>
    public class OpenChannelVolumeDuck
        : IVolumeProvider
    {
        #region fields and properties
        private readonly RoomChannels _rooms;
        private readonly PlayerChannels _players;

        private volatile float _targetVolume = 1;
        public float TargetVolume
        {
            get { return _targetVolume; }
        }
        #endregion

        public OpenChannelVolumeDuck(RoomChannels rooms, PlayerChannels players)
        {
            _rooms = rooms;
            _players = players;
        }

        public void Update(bool isMuted)
        {
            UpdateTargetVolume(isMuted);
        }

        private void UpdateTargetVolume(bool isMuted)
        {
            var talking = !isMuted && (_rooms.Count > 0 || _players.Count > 0);

            _targetVolume = talking
                          ? VoiceSettings.Instance.VoiceDuckLevel
                          : 1;
        }
    }
}

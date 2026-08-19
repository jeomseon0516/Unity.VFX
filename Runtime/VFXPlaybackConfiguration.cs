using System;

namespace Jeomseon.Unity.VFX
{
    public sealed class VFXPlaybackConfiguration
    {
        public static VFXPlaybackConfiguration Default { get; } = new();

        public bool PlayChildren { get; }
        public bool ClearBeforePlay { get; }
        public bool ClearOnRelease { get; }
        public float PlaybackSpeed { get; }

        public VFXPlaybackConfiguration(
            bool playChildren = true,
            bool clearBeforePlay = true,
            bool clearOnRelease = true,
            float playbackSpeed = 1f)
        {
            if (playbackSpeed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(playbackSpeed));
            }

            PlayChildren = playChildren;
            ClearBeforePlay = clearBeforePlay;
            ClearOnRelease = clearOnRelease;
            PlaybackSpeed = playbackSpeed;
        }
    }
}

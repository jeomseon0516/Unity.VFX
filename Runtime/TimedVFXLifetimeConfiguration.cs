using System;

namespace Jeomseon.Unity.VFX
{
    public sealed class TimedVFXLifetimeConfiguration : IVFXLifetimeConfiguration
    {
        public float Duration { get; }
        public VFXTimeMode TimeMode { get; }

        public TimedVFXLifetimeConfiguration(float duration, VFXTimeMode timeMode)
        {
            if (duration < 0f) throw new ArgumentOutOfRangeException(nameof(duration));

            Duration = duration;
            TimeMode = timeMode;
        }
    }
}

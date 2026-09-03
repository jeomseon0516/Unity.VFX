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

        public IVFXLifetimeSession CreateSession(in VFXLifetimeContext context) =>
            new Session(Duration, TimeMode);

        private sealed class Session : IVFXLifetimeSession
        {
            private readonly VFXTimeMode _timeMode;
            private float _remaining;

            internal Session(float duration, VFXTimeMode timeMode)
            {
                _remaining = duration;
                _timeMode = timeMode;
            }

            public bool Tick(
                in VFXLifetimeContext context,
                float deltaTime,
                float unscaledDeltaTime)
            {
                _remaining -= _timeMode == VFXTimeMode.Scaled
                    ? deltaTime
                    : unscaledDeltaTime;
                return _remaining <= 0f;
            }

            public void Dispose()
            {
            }
        }
    }
}

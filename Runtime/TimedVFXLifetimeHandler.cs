using System;

namespace Jeomseon.Unity.VFX
{
    public sealed class TimedVFXLifetimeHandler : IVFXLifetimeHandler
    {
        public bool CanHandle(IVFXLifetimeConfiguration configuration) =>
            configuration is TimedVFXLifetimeConfiguration;

        public IVFXLifetimeSession Begin(
            IVFXLifetimeConfiguration configuration,
            in VFXLifetimeContext context)
        {
            return configuration is TimedVFXLifetimeConfiguration timed
                ? new Session(timed.Duration, timed.TimeMode)
                : throw new ArgumentException(
                    "Unsupported VFX lifetime configuration.",
                    nameof(configuration));
        }

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

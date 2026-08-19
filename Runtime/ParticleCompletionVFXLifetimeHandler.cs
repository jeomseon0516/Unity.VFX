using System;

namespace Jeomseon.Unity.VFX
{
    public sealed class ParticleCompletionVFXLifetimeHandler : IVFXLifetimeHandler
    {
        public bool CanHandle(IVFXLifetimeConfiguration configuration) =>
            configuration is ParticleCompletionVFXLifetimeConfiguration;

        public IVFXLifetimeSession Begin(
            IVFXLifetimeConfiguration configuration,
            in VFXLifetimeContext context)
        {
            return configuration is ParticleCompletionVFXLifetimeConfiguration particle
                ? new Session(particle.IncludeChildren)
                : throw new ArgumentException(
                    "Unsupported VFX lifetime configuration.",
                    nameof(configuration));
        }

        private sealed class Session : IVFXLifetimeSession
        {
            private readonly bool _includeChildren;
            private bool _observedFirstFrame;

            internal Session(bool includeChildren)
            {
                _includeChildren = includeChildren;
            }

            public bool Tick(
                in VFXLifetimeContext context,
                float deltaTime,
                float unscaledDeltaTime)
            {
                if (!_observedFirstFrame)
                {
                    _observedFirstFrame = true;
                    return false;
                }

                return context.HasParticleSystems &&
                       !context.IsAnyParticleAlive(_includeChildren);
            }

            public void Dispose()
            {
            }
        }
    }
}

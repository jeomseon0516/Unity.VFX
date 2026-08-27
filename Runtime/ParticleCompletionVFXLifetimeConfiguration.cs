namespace Jeomseon.Unity.VFX
{
    public sealed class ParticleCompletionVFXLifetimeConfiguration :
        IVFXLifetimeConfiguration
    {
        public static ParticleCompletionVFXLifetimeConfiguration Default { get; } = new();

        public bool IncludeChildren { get; }

        public ParticleCompletionVFXLifetimeConfiguration(bool includeChildren = true)
        {
            IncludeChildren = includeChildren;
        }

        public IVFXLifetimeSession CreateSession(in VFXLifetimeContext context) =>
            new Session(IncludeChildren);

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

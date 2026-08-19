namespace Jeomseon.Unity.VFX
{
    public sealed class ManualVFXLifetimeHandler : IVFXLifetimeHandler
    {
        public bool CanHandle(IVFXLifetimeConfiguration configuration) =>
            configuration is ManualVFXLifetimeConfiguration;

        public IVFXLifetimeSession Begin(
            IVFXLifetimeConfiguration configuration,
            in VFXLifetimeContext context) =>
            Session.Instance;

        private sealed class Session : IVFXLifetimeSession
        {
            internal static Session Instance { get; } = new();

            private Session()
            {
            }

            public bool Tick(
                in VFXLifetimeContext context,
                float deltaTime,
                float unscaledDeltaTime) => false;

            public void Dispose()
            {
            }
        }
    }
}

namespace Jeomseon.Unity.VFX
{
    public sealed class ManualVFXLifetimeConfiguration : IVFXLifetimeConfiguration
    {
        public static ManualVFXLifetimeConfiguration Instance { get; } = new();

        private ManualVFXLifetimeConfiguration()
        {
        }

        public IVFXLifetimeSession CreateSession(in VFXLifetimeContext context) =>
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

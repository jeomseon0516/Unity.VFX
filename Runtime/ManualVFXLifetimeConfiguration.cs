namespace Jeomseon.Unity.VFX
{
    public sealed class ManualVFXLifetimeConfiguration : IVFXLifetimeConfiguration
    {
        public static ManualVFXLifetimeConfiguration Instance { get; } = new();

        private ManualVFXLifetimeConfiguration()
        {
        }
    }
}

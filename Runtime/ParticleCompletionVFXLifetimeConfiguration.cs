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
    }
}

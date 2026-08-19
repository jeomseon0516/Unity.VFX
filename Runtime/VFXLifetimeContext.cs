namespace Jeomseon.Unity.VFX
{
    public readonly struct VFXLifetimeContext
    {
        private readonly VFXInstance _instance;

        internal VFXLifetimeContext(VFXInstance instance)
        {
            _instance = instance;
        }

        public bool HasParticleSystems => _instance && _instance.HasParticleSystems;

        public bool IsAnyParticleAlive(bool includeChildren = true) =>
            _instance && _instance.IsAnyParticleAlive(includeChildren);
    }
}

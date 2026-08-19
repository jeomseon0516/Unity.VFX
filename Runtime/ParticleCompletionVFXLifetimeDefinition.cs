using UnityEngine;

namespace Jeomseon.Unity.VFX
{
    [CreateAssetMenu(
        fileName = nameof(ParticleCompletionVFXLifetimeDefinition),
        menuName = "Jeomseon/VFX/Lifetime/Particle Completion")]
    public sealed class ParticleCompletionVFXLifetimeDefinition : VFXLifetimeDefinition
    {
        [SerializeField] private bool includeChildren = true;

        public override IVFXLifetimeConfiguration CreateConfiguration() =>
            new ParticleCompletionVFXLifetimeConfiguration(includeChildren);
    }
}

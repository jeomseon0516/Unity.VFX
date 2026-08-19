using UnityEngine;

namespace Jeomseon.Unity.VFX
{
    [CreateAssetMenu(
        fileName = nameof(ManualVFXLifetimeDefinition),
        menuName = "Jeomseon/VFX/Lifetime/Manual")]
    public sealed class ManualVFXLifetimeDefinition : VFXLifetimeDefinition
    {
        public override IVFXLifetimeConfiguration CreateConfiguration() =>
            ManualVFXLifetimeConfiguration.Instance;
    }
}

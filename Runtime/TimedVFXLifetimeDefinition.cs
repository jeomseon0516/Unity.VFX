using UnityEngine;

namespace Jeomseon.Unity.VFX
{
    [CreateAssetMenu(
        fileName = nameof(TimedVFXLifetimeDefinition),
        menuName = "Jeomseon/VFX/Lifetime/Timed")]
    public sealed class TimedVFXLifetimeDefinition : VFXLifetimeDefinition
    {
        [SerializeField, Min(0f)] private float duration = 1f;
        [SerializeField] private VFXTimeMode timeMode;

        public override IVFXLifetimeConfiguration CreateConfiguration() =>
            new TimedVFXLifetimeConfiguration(duration, timeMode);
    }
}

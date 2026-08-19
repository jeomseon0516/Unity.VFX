using UnityEngine;

namespace Jeomseon.Unity.VFX
{
    public abstract class VFXLifetimeDefinition : ScriptableObject
    {
        public abstract IVFXLifetimeConfiguration CreateConfiguration();
    }
}

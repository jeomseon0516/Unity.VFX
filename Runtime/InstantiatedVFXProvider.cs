using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jeomseon.Unity.VFX
{
    public sealed class InstantiatedVFXProvider : VFXProviderBase
    {
        private readonly VFXInstance _prefab;

        public InstantiatedVFXProvider(
            GameObject prefab,
            IVFXLifetimeConfiguration lifetimeConfiguration,
            VFXPlaybackConfiguration playbackConfiguration = null)
            : base(lifetimeConfiguration, playbackConfiguration)
        {
            if (!prefab) throw new ArgumentNullException(nameof(prefab));
            _prefab = prefab.TryGetComponent(out VFXInstance instance)
                ? instance
                : throw new ArgumentException(
                    $"The VFX prefab must contain {nameof(VFXInstance)}.",
                    nameof(prefab));
        }

        public override VFXHandle Spawn(in VFXSpawnOptions options)
        {
            VFXInstance instance = Object.Instantiate(
                _prefab,
                options.Position,
                options.Rotation,
                options.Parent);
            instance.gameObject.SetActive(true);
            return Activate(instance, options);
        }

        protected override void ReleaseInstance(VFXInstance instance)
        {
            if (instance) Object.Destroy(instance.gameObject);
        }
    }
}

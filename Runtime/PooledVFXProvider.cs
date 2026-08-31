using Jeomseon.Unity.GameObjectPooling.Contracts;
using Jeomseon.Unity.GameObjectPooling.Handles;
using Jeomseon.Unity.GameObjectPooling.Providers;
using UnityEngine;

namespace Jeomseon.Unity.VFX
{
    public sealed class PooledVFXProvider : VFXProviderBase
    {
        private readonly GameObjectPoolHandle _handle;
        private readonly ComponentPoolProvider<VFXInstance> _poolProvider;

        public PooledVFXProvider(
            GameObjectPoolHandle handle,
            IVFXLifetimeConfiguration lifetimeConfiguration,
            VFXPlaybackConfiguration playbackConfiguration = null)
            : base(lifetimeConfiguration, playbackConfiguration)
        {
            _handle = handle;
            _poolProvider = new ComponentPoolProvider<VFXInstance>(handle);
        }

        public override VFXHandle Spawn(in VFXSpawnOptions options)
        {
            VFXInstance instance = _poolProvider.Spawn(
                PoolSpawnOptions.At(
                    options.Position,
                    options.Rotation,
                    options.Parent));
            return Activate(instance, options);
        }

        protected override void ReleaseInstance(VFXInstance instance)
        {
            if (_handle.IsValid)
            {
                _poolProvider.Despawn(instance);
                return;
            }

            if (instance) Object.Destroy(instance.gameObject);
        }
    }
}

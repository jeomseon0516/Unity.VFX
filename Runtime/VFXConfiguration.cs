using System;
using Jeomseon.Unity.GameObjectPooling.Registrations;
using UnityEngine;

namespace Jeomseon.Unity.VFX
{
    public sealed class VFXConfiguration
    {
        public VFXReuseMode ReuseMode { get; }
        public GameObject Prefab { get; }
        public GameObjectPoolRegistration PoolRegistration { get; }
        public IVFXLifetimeConfiguration LifetimeConfiguration { get; }
        public VFXPlaybackConfiguration PlaybackConfiguration { get; }

        private VFXConfiguration(
            VFXReuseMode reuseMode,
            GameObject prefab,
            GameObjectPoolRegistration poolRegistration,
            IVFXLifetimeConfiguration lifetimeConfiguration,
            VFXPlaybackConfiguration playbackConfiguration)
        {
            ReuseMode = reuseMode;
            Prefab = prefab;
            PoolRegistration = poolRegistration;
            LifetimeConfiguration = lifetimeConfiguration ??
                throw new ArgumentNullException(nameof(lifetimeConfiguration));
            PlaybackConfiguration = playbackConfiguration ?? VFXPlaybackConfiguration.Default;
        }

        public static VFXConfiguration Instantiate(
            GameObject prefab,
            IVFXLifetimeConfiguration lifetimeConfiguration,
            VFXPlaybackConfiguration playbackConfiguration = null)
        {
            if (!prefab) throw new ArgumentNullException(nameof(prefab));
            ValidatePrefab(prefab);
            return new VFXConfiguration(
                VFXReuseMode.Instantiate,
                prefab,
                null,
                lifetimeConfiguration,
                playbackConfiguration);
        }

        public static VFXConfiguration Pool(
            GameObjectPoolRegistration poolRegistration,
            IVFXLifetimeConfiguration lifetimeConfiguration,
            VFXPlaybackConfiguration playbackConfiguration = null)
        {
            return new VFXConfiguration(
                VFXReuseMode.Pool,
                null,
                poolRegistration ?? throw new ArgumentNullException(nameof(poolRegistration)),
                lifetimeConfiguration,
                playbackConfiguration);
        }

        private static void ValidatePrefab(GameObject prefab)
        {
            if (!prefab.TryGetComponent<VFXInstance>(out _))
            {
                throw new ArgumentException(
                    $"The VFX prefab must contain {nameof(VFXInstance)}.",
                    nameof(prefab));
            }
        }
    }
}

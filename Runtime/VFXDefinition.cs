using System;
using Jeomseon.Unity.GameObjectPooling.Definitions;
using UnityEngine;

namespace Jeomseon.Unity.VFX
{
    [CreateAssetMenu(
        fileName = nameof(VFXDefinition),
        menuName = "Jeomseon/VFX/VFX Definition")]
    public sealed class VFXDefinition : ScriptableObject
    {
        [SerializeField] private VFXReuseMode reuseMode;
        [SerializeField] private GameObject prefab;
        [SerializeField] private GameObjectPoolDefinition poolDefinition;
        [SerializeField] private VFXLifetimeDefinition lifetimeDefinition;
        [SerializeField] private bool playChildren = true;
        [SerializeField] private bool clearBeforePlay = true;
        [SerializeField] private bool clearOnRelease = true;
        [SerializeField, Min(0f)] private float playbackSpeed = 1f;

        public VFXConfiguration CreateConfiguration()
        {
            if (lifetimeDefinition == null)
            {
                throw new InvalidOperationException(
                    $"{name} requires a VFX lifetime definition.");
            }

            var playback = new VFXPlaybackConfiguration(
                playChildren,
                clearBeforePlay,
                clearOnRelease,
                playbackSpeed);
            IVFXLifetimeConfiguration lifetime =
                lifetimeDefinition.CreateConfiguration();

            return reuseMode switch
            {
                VFXReuseMode.Instantiate =>
                    VFXConfiguration.Instantiate(prefab, lifetime, playback),
                VFXReuseMode.Pool when poolDefinition != null =>
                    VFXConfiguration.Pool(
                        poolDefinition.CreateRegistration(),
                        lifetime,
                        playback),
                VFXReuseMode.Pool => throw new InvalidOperationException(
                    $"{name} requires a GameObject pool definition."),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void OnValidate()
        {
            playbackSpeed = Mathf.Max(0f, playbackSpeed);
        }
    }
}

using System.Collections;
using Jeomseon.Unity.GameObjectPooling.Configurations;
using Jeomseon.Unity.GameObjectPooling.Registrations;
using Jeomseon.Unity.GameObjectPooling.Scopes;
using Jeomseon.Unity.VFX;
using UnityEngine;

namespace Jeomseon.Samples.VFX
{
    public sealed class VFXBasicUsageSample : MonoBehaviour
    {
        [Header("Runtime configuration")]
        [SerializeField] private VFXEmitter runtimeEmitter;
        [SerializeField] private GameObjectPoolScope runtimePoolScope;
        [SerializeField] private GameObject runtimePrefab;

        [Header("ScriptableObject configuration")]
        [SerializeField] private VFXEmitter definitionEmitter;

        [Header("Presentation")]
        [SerializeField, Min(0.1f)] private float spawnInterval = 0.8f;
        [SerializeField] private Vector3 runtimePosition = new(-2f, 0f, 0f);
        [SerializeField] private Vector3 definitionPosition = new(2f, 0f, 0f);

        private int _runtimeSpawnCount;
        private int _definitionSpawnCount;

        private void Awake()
        {
            var poolConfiguration = new UnityGameObjectPoolConfiguration(
                runtimePrefab,
                "Runtime-configured VFX",
                prewarmCount: 3,
                maxInactiveCount: 8);
            var registration = new GameObjectPoolRegistration(
                poolConfiguration,
                PoolLifetimeConfiguration.Scope,
                "Runtime-configured VFX Pool");
            var lifetime = new ParticleCompletionVFXLifetimeConfiguration(
                includeChildren: true);

            runtimeEmitter.Initialize(
                VFXConfiguration.Pool(registration, lifetime),
                runtimePoolScope);
        }

        private IEnumerator Start()
        {
            while (enabled)
            {
                runtimeEmitter.Spawn(runtimePosition, Quaternion.identity);
                _runtimeSpawnCount++;
                yield return new WaitForSeconds(spawnInterval * 0.5f);

                definitionEmitter.Spawn(definitionPosition, Quaternion.identity);
                _definitionSpawnCount++;
                yield return new WaitForSeconds(spawnInterval * 0.5f);
            }
        }

        private void OnGUI()
        {
            const float width = 460f;
            GUI.Box(new Rect(16f, 16f, width, 82f), GUIContent.none);
            GUI.Label(new Rect(28f, 24f, width - 24f, 24f),
                $"Runtime VFXConfiguration (left): {_runtimeSpawnCount} spawned");
            GUI.Label(new Rect(28f, 48f, width - 24f, 24f),
                $"VFXDefinition ScriptableObject (right): {_definitionSpawnCount} spawned");
            GUI.Label(new Rect(28f, 72f, width - 24f, 20f),
                "Both use Particle Completion lifetime and the same pooled prefab.");
        }
    }
}

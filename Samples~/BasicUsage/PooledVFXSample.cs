using System.Collections;
using Jeomseon.Unity.GameObjectPooling.Configurations;
using Jeomseon.Unity.GameObjectPooling.Registrations;
using Jeomseon.Unity.GameObjectPooling.Scopes;
using Jeomseon.Unity.VFX;
using UnityEngine;

namespace Jeomseon.Samples.VFX
{
    public sealed class PooledVFXSample : MonoBehaviour
    {
        [SerializeField] private VFXReuseMode reuseMode;

        private GameObject _template;
        private GameObjectPoolScope _scope;
        private VFXEmitter _emitter;
        private int _spawnCount;

        private IEnumerator Start()
        {
            CreateTemplate();
            _emitter = gameObject.AddComponent<VFXEmitter>();
            InitializeEmitter();

            while (enabled)
            {
                Vector3 position = new(Mathf.Sin(_spawnCount) * 2.5f, 0f, 0f);
                _emitter.Spawn(position, Quaternion.identity);
                _spawnCount++;
                yield return new WaitForSeconds(0.6f);
            }
        }

        private void OnDestroy()
        {
            if (_scope) Destroy(_scope.gameObject);
            if (_template) Destroy(_template);
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(20f, 20f, 700f, 28f),
                $"VFX Provider Sample | mode: {reuseMode} | spawned: {_spawnCount}");
            GUI.Label(new Rect(20f, 48f, 700f, 28f),
                "Change Reuse Mode before Play to compare pooled reuse and Instantiate/Destroy.");
        }

        private void CreateTemplate()
        {
            GameObject templateObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            templateObject.name = "Runtime VFX Template";
            templateObject.transform.localScale = Vector3.one * 0.65f;
            templateObject.AddComponent<VFXInstance>();
            _template = templateObject;
            templateObject.SetActive(false);
        }

        private void InitializeEmitter()
        {
            var lifetime = new TimedVFXLifetimeConfiguration(
                1.5f,
                VFXTimeMode.Scaled);
            if (reuseMode == VFXReuseMode.Instantiate)
            {
                _emitter.Initialize(VFXConfiguration.Instantiate(_template, lifetime));
                return;
            }

            var scopeObject = new GameObject("VFX Sample Pool Scope");
            _scope = scopeObject.AddComponent<GameObjectPoolScope>();
            var configuration = new UnityGameObjectPoolConfiguration(
                _template,
                "Sample VFX",
                prewarmCount: 3,
                maxInactiveCount: 3);
            var registration = new GameObjectPoolRegistration(
                configuration,
                PoolLifetimeConfiguration.Scope,
                "Sample VFX Pool");
            _emitter.Initialize(
                VFXConfiguration.Pool(registration, lifetime),
                _scope);
        }
    }
}

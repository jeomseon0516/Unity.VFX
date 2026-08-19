using System;
using System.Collections;
using Jeomseon.Unity.GameObjectPooling.Configurations;
using Jeomseon.Unity.GameObjectPooling.Registrations;
using Jeomseon.Unity.GameObjectPooling.Scopes;
using Jeomseon.Unity.VFX;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Jeomseon.Tests
{
    public sealed class VFXProviderPlayModeTests
    {
        private GameObject _prefab;
        private GameObject _host;
        private GameObjectPoolScope _scope;
        private VFXEmitter _emitter;

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("VFX Test Prefab");
            _prefab.AddComponent<VFXInstance>();
            _prefab.SetActive(false);

            _host = new GameObject("VFX Test Host");
            _scope = _host.AddComponent<GameObjectPoolScope>();
            _emitter = _host.AddComponent<VFXEmitter>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host) Object.Destroy(_host);
            if (_prefab) Object.Destroy(_prefab);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PooledTimedLifetime_ReturnsAndInvalidatesHandle()
        {
            InitializePooled(new TimedVFXLifetimeConfiguration(0.05f, VFXTimeMode.Scaled));
            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

            Assert.That(handle.IsValid, Is.True);
            yield return new WaitForSeconds(0.08f);

            Assert.That(handle.IsValid, Is.False);
            Assert.That(handle.TryRelease(), Is.False);
        }

        [UnityTest]
        public IEnumerator PooledTimedLifetime_ZeroDuration_HandleIsValidImmediatelyAfterSpawn()
        {
            InitializePooled(new TimedVFXLifetimeConfiguration(0f, VFXTimeMode.Scaled));
            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

            Assert.That(handle.IsValid, Is.True);
            yield return null;

            Assert.That(handle.IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator ReusedInstance_OldGenerationHandleCannotReleaseCurrentLease()
        {
            InitializePooled(ManualVFXLifetimeConfiguration.Instance);
            VFXHandle oldHandle = _emitter.Spawn(Vector3.zero, Quaternion.identity);
            Assert.That(oldHandle.TryRelease(), Is.True);

            VFXHandle currentHandle = _emitter.Spawn(Vector3.one, Quaternion.identity);

            Assert.That(oldHandle.IsValid, Is.False);
            Assert.That(oldHandle.TryRelease(), Is.False);
            Assert.That(currentHandle.IsValid, Is.True);

            yield return null;
        }

        [UnityTest]
        public IEnumerator InstantiatedManualLifetime_RequiresExplicitRelease()
        {
            _emitter.Initialize(VFXConfiguration.Instantiate(
                _prefab,
                ManualVFXLifetimeConfiguration.Instance));
            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

            yield return new WaitForSeconds(0.05f);
            Assert.That(handle.IsValid, Is.True);

            Assert.That(handle.TryRelease(), Is.True);
            yield return null;
            Assert.That(handle.IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator UnscaledTimedLifetime_CompletesWhileTimeScaleIsZero()
        {
            var previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                _emitter.Initialize(VFXConfiguration.Instantiate(
                    _prefab,
                    new TimedVFXLifetimeConfiguration(0.03f, VFXTimeMode.Unscaled)));
                var handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

                yield return new WaitForSecondsRealtime(0.06f);
                Assert.That(handle.IsValid, Is.False);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
            }
        }

        [Test]
        public void PooledRelease_ClearsTrailRenderer()
        {
            _prefab.AddComponent<TrailRenderer>();
            InitializePooled(ManualVFXLifetimeConfiguration.Instance);
            var handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

            var trailRenderer = Object.FindAnyObjectByType<TrailRenderer>(FindObjectsInactive.Exclude);
            Assert.That(trailRenderer, Is.Not.Null);
            // SetPositions only overwrites existing points and cannot grow positionCount;
            // AddPositions is required to add new ones.
            trailRenderer.AddPositions(new[] { Vector3.zero, Vector3.one });
            Assert.That(trailRenderer.positionCount, Is.EqualTo(2));

            Assert.That(handle.TryRelease(), Is.True);

            Assert.That(trailRenderer.positionCount, Is.EqualTo(0));
        }

        private void InitializePooled(IVFXLifetimeConfiguration lifetime)
        {
            var poolConfiguration = new UnityGameObjectPoolConfiguration(
                _prefab,
                prewarmCount: 1,
                maxInactiveCount: 2);
            var registration = new GameObjectPoolRegistration(
                poolConfiguration,
                PoolLifetimeConfiguration.Scope,
                "VFX Test Pool");
            _emitter.Initialize(
                VFXConfiguration.Pool(registration, lifetime),
                _scope);
        }
    }
}

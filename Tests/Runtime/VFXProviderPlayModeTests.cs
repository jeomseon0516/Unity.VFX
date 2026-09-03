using System;
using System.Collections;
using System.Text.RegularExpressions;
using Jeomseon.Unity.GameObjectPooling.Configurations;
using Jeomseon.Unity.GameObjectPooling.Registrations;
using Jeomseon.Unity.GameObjectPooling.Scopes;
using Jeomseon.Unity.VFX;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Jeomseon.Tests
{
    public sealed class VFXProviderPlayModeTests
    {
        private GameObject _prefab;
        private GameObject _host;
        private GameObject _poolHost;
        private GameObjectPoolScope _scope;
        private VFXEmitter _emitter;

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("VFX Test Prefab");
            _prefab.AddComponent<VFXInstance>();
            _prefab.SetActive(false);

            _poolHost = new GameObject("VFX Test Pool Host");
            _scope = _poolHost.AddComponent<GameObjectPoolScope>();
            _host = new GameObject("VFX Test Emitter Host");
            _emitter = _host.AddComponent<VFXEmitter>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host) Object.Destroy(_host);
            if (_poolHost) Object.Destroy(_poolHost);
            if (_prefab) Object.Destroy(_prefab);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PooledTimedLifetime_ReturnsAndInvalidatesHandle()
        {
            InitializePooled(new TimedVFXLifetimeConfiguration(0.05f, VFXTimeMode.Scaled));
            var handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

            Assert.That(handle.IsValid, Is.True);
            yield return new WaitForSeconds(0.08f);

            Assert.That(handle.IsValid, Is.False);
            Assert.That(handle.TryRelease(), Is.False);
        }

        [UnityTest]
        public IEnumerator PooledTimedLifetime_ZeroDuration_HandleIsValidImmediatelyAfterSpawn()
        {
            InitializePooled(new TimedVFXLifetimeConfiguration(0f, VFXTimeMode.Scaled));
            var handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

            Assert.That(handle.IsValid, Is.True);
            yield return null;

            Assert.That(handle.IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator ReusedInstance_OldGenerationHandleCannotReleaseCurrentLease()
        {
            InitializePooled(ManualVFXLifetimeConfiguration.Instance);
            var oldHandle = _emitter.Spawn(Vector3.zero, Quaternion.identity);
            Assert.That(oldHandle.TryRelease(), Is.True);

            var currentHandle = _emitter.Spawn(Vector3.one, Quaternion.identity);

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
            var handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

            yield return new WaitForSeconds(0.05f);
            Assert.That(handle.IsValid, Is.True);

            Assert.That(handle.TryRelease(), Is.True);
            yield return null;
            Assert.That(handle.IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator DestroyingEmitter_ReleasesActiveInstantiatedVFX()
        {
            _emitter.Initialize(VFXConfiguration.Instantiate(
                _prefab,
                ManualVFXLifetimeConfiguration.Instance));
            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);
            VFXInstance instance = FindActiveVFXInstance();

            Object.Destroy(_host);
            yield return null;

            Assert.That(instance == null, Is.True);
            Assert.That(handle.IsValid, Is.False);
            Assert.That(handle.TryRelease(), Is.False);
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

        [UnityTest]
        public IEnumerator ScaledTimedLifetime_RemainsActiveWhileTimeScaleIsZero()
        {
            float previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                _emitter.Initialize(VFXConfiguration.Instantiate(
                    _prefab,
                    new TimedVFXLifetimeConfiguration(0.03f, VFXTimeMode.Scaled)));
                VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

                yield return new WaitForSecondsRealtime(0.06f);

                Assert.That(handle.IsValid, Is.True);
                Assert.That(handle.TryRelease(), Is.True);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
            }
        }

        [UnityTest]
        public IEnumerator LifetimeSessionException_LogsAndReleasesVFX()
        {
            var lifetime = new ThrowingLifetimeConfiguration();
            _emitter.Initialize(VFXConfiguration.Instantiate(_prefab, lifetime));
            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);
            LogAssert.Expect(LogType.Exception, new Regex("Lifetime tick failed"));

            yield return null;

            Assert.That(handle.IsValid, Is.False);
            Assert.That(lifetime.DisposedSessionCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator LifetimeAndReleaseExceptions_AreBothLoggedWithoutEscaping()
        {
            _emitter.Initialize(VFXConfiguration.Instantiate(
                _prefab,
                new ThrowingTickAndDisposeLifetimeConfiguration()));
            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);
            LogAssert.Expect(LogType.Exception, new Regex("Lifetime tick failed"));
            LogAssert.Expect(LogType.Exception, new Regex("Lifetime dispose failed"));

            yield return null;

            Assert.That(handle.IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator DestroyingEmitter_ReleasesActivePooledVFX()
        {
            var lifetime = new TrackingLifetimeConfiguration();
            InitializePooled(lifetime);
            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

            Object.Destroy(_host);
            yield return null;

            Assert.That(handle.IsValid, Is.False);
            Assert.That(handle.TryRelease(), Is.False);
            Assert.That(lifetime.DisposedSessionCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DestroyingPoolBeforeEmitter_CleansActiveVFXOnce()
        {
            var lifetime = new TrackingLifetimeConfiguration();
            InitializePooled(lifetime);
            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

            Object.Destroy(_poolHost);
            yield return null;

            Assert.That(handle.IsValid, Is.False);
            Assert.That(handle.TryRelease(), Is.False);
            Assert.That(lifetime.DisposedSessionCount, Is.EqualTo(1));

            Object.Destroy(_host);
            yield return null;

            Assert.That(lifetime.DisposedSessionCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PreservedVFX_RemainsUntilOwnerReleasesItAfterPoolIsDestroyed()
        {
            var poolConfiguration = new UnityGameObjectPoolConfiguration(
                _prefab,
                prewarmCount: 1,
                maxInactiveCount: 2,
                activeInstanceShutdownPolicy: ActiveInstanceShutdownPolicy.Preserve);
            var registration = new GameObjectPoolRegistration(
                poolConfiguration,
                PoolLifetimeConfiguration.Scope,
                "Preserved VFX Test Pool");
            _emitter.Initialize(
                VFXConfiguration.Pool(
                    registration,
                    ManualVFXLifetimeConfiguration.Instance),
                _scope);
            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);
            VFXInstance instance = FindActiveVFXInstance();

            Object.Destroy(_poolHost);
            yield return null;

            Assert.That(instance, Is.Not.Null);
            Assert.That(handle.IsValid, Is.True);

            Assert.That(handle.TryRelease(), Is.True);
            yield return null;

            Assert.That(instance == null, Is.True);
            Assert.That(handle.IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator UnloadingAdditiveScene_CleansActiveVFXOnce()
        {
            Scene scene = SceneManager.CreateScene("VFX Additive Lifetime Test");
            SceneManager.MoveGameObjectToScene(_poolHost, scene);
            SceneManager.MoveGameObjectToScene(_host, scene);

            var lifetime = new TrackingLifetimeConfiguration();
            InitializePooled(lifetime);
            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            Assert.That(unload, Is.Not.Null);
            yield return unload;

            Assert.That(handle.IsValid, Is.False);
            Assert.That(handle.TryRelease(), Is.False);
            Assert.That(lifetime.DisposedSessionCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator LoopingParticleSystem_RemainsActiveUntilManualRelease()
        {
            AddParticleSystem(_prefab, loop: true, 0.05f, 0.08f);
            InitializePooled(ParticleCompletionVFXLifetimeConfiguration.Default);

            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);
            yield return new WaitForSeconds(0.2f);

            Assert.That(handle.IsValid, Is.True);
            Assert.That(FindActiveVFXInstance().GetComponent<ParticleSystem>().IsAlive(), Is.True);
            Assert.That(handle.TryRelease(), Is.True);
        }

        [UnityTest]
        public IEnumerator ChildParticleSystem_DelaysReturnUntilItFinishes()
        {
            var child = new GameObject("Child Particle System");
            child.transform.SetParent(_prefab.transform, false);
            AddParticleSystem(child, loop: false, 0.05f, 0.15f);
            InitializePooled(new ParticleCompletionVFXLifetimeConfiguration(includeChildren: true));

            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);
            yield return new WaitForSeconds(0.08f);

            Assert.That(handle.IsValid, Is.True);
            yield return new WaitForSeconds(0.25f);

            Assert.That(handle.IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator ManualSubEmitter_DelaysReturnUntilSpawnedParticlesFinish()
        {
            ParticleSystem parent = AddParticleSystem(_prefab, loop: false, 0.04f, 0.04f);
            ParticleSystem.EmissionModule parentEmission = parent.emission;
            parentEmission.rateOverTime = 0f;
            parentEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            var child = new GameObject("Manual Sub Emitter");
            child.transform.SetParent(_prefab.transform, false);
            ParticleSystem subEmitter = AddParticleSystem(child, loop: false, 0.04f, 0.2f);
            ParticleSystem.EmissionModule subEmitterEmission = subEmitter.emission;
            subEmitterEmission.rateOverTime = 0f;
            subEmitterEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            ParticleSystem.SubEmittersModule subEmitters = parent.subEmitters;
            subEmitters.enabled = true;
            subEmitters.AddSubEmitter(
                subEmitter,
                ParticleSystemSubEmitterType.Manual,
                ParticleSystemSubEmitterProperties.InheritNothing);
            InitializePooled(new ParticleCompletionVFXLifetimeConfiguration(includeChildren: true));

            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);
            ParticleSystem activeParent = FindActiveVFXInstance().GetComponent<ParticleSystem>();
            var sourceParticle = new ParticleSystem.Particle
            {
                position = Vector3.zero,
                startLifetime = 1f,
                remainingLifetime = 1f,
                startSize = 1f,
                randomSeed = 1
            };
            activeParent.TriggerSubEmitter(0, ref sourceParticle);
            ParticleSystem activeSubEmitter =
                activeParent.subEmitters.GetSubEmitterSystem(0);
            Assert.That(activeSubEmitter.particleCount, Is.GreaterThan(0));
            yield return new WaitForSeconds(0.1f);

            Assert.That(handle.IsValid, Is.True);
            yield return new WaitForSeconds(0.3f);

            Assert.That(handle.IsValid, Is.False);
        }

        [Test]
        public void ManualRelease_ReturnsActiveVFXExactlyOnce()
        {
            var lifetime = new TrackingLifetimeConfiguration();
            InitializePooled(lifetime);
            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

            Assert.That(handle.TryRelease(), Is.True);
            Assert.That(handle.TryRelease(), Is.False);
            Assert.That(handle.IsValid, Is.False);
            Assert.That(lifetime.DisposedSessionCount, Is.EqualTo(1));
        }

        [Test]
        public void ReusedInstance_StartsWithoutPreviousParticlesOrTrail()
        {
            ParticleSystem prefabParticleSystem =
                AddParticleSystem(_prefab, loop: true, 1f, 1f);
            ParticleSystem.EmissionModule emission = prefabParticleSystem.emission;
            emission.enabled = false;
            _prefab.AddComponent<TrailRenderer>();
            InitializePooled(ManualVFXLifetimeConfiguration.Instance);
            VFXHandle firstHandle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

            VFXInstance firstInstance = FindActiveVFXInstance();
            ParticleSystem particleSystem = firstInstance.GetComponent<ParticleSystem>();
            TrailRenderer trailRenderer = firstInstance.GetComponent<TrailRenderer>();
            particleSystem.Emit(4);
            trailRenderer.AddPositions(new[] { Vector3.zero, Vector3.one });
            Assert.That(particleSystem.particleCount, Is.EqualTo(4));
            Assert.That(trailRenderer.positionCount, Is.EqualTo(2));

            Assert.That(firstHandle.TryRelease(), Is.True);
            Assert.That(particleSystem.particleCount, Is.EqualTo(0));
            Assert.That(trailRenderer.positionCount, Is.EqualTo(0));

            VFXHandle secondHandle = _emitter.Spawn(Vector3.one, Quaternion.identity);
            VFXInstance secondInstance = FindActiveVFXInstance();

            Assert.That(secondInstance, Is.SameAs(firstInstance));
            Assert.That(particleSystem.particleCount, Is.EqualTo(0));
            Assert.That(trailRenderer.positionCount, Is.EqualTo(0));
            Assert.That(secondHandle.TryRelease(), Is.True);
        }

        [Test]
        public void ReusedInstance_StartsWithoutPreviousSubEmitterParticles()
        {
            ParticleSystem parent = AddParticleSystem(_prefab, loop: true, 1f, 1f);
            ParticleSystem.EmissionModule parentEmission = parent.emission;
            parentEmission.enabled = false;
            var child = new GameObject("Manual Sub Emitter");
            child.transform.SetParent(_prefab.transform, false);
            ParticleSystem subEmitter = AddParticleSystem(child, loop: false, 1f, 1f);
            ParticleSystem.EmissionModule subEmitterEmission = subEmitter.emission;
            subEmitterEmission.rateOverTime = 0f;
            subEmitterEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            ParticleSystem.SubEmittersModule subEmitters = parent.subEmitters;
            subEmitters.enabled = true;
            subEmitters.AddSubEmitter(
                subEmitter,
                ParticleSystemSubEmitterType.Manual,
                ParticleSystemSubEmitterProperties.InheritNothing);
            InitializePooled(ManualVFXLifetimeConfiguration.Instance);
            VFXHandle firstHandle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

            ParticleSystem activeParent = FindActiveVFXInstance().GetComponent<ParticleSystem>();
            var sourceParticle = new ParticleSystem.Particle
            {
                position = Vector3.zero,
                startLifetime = 1f,
                remainingLifetime = 1f,
                startSize = 1f,
                randomSeed = 1
            };
            activeParent.TriggerSubEmitter(0, ref sourceParticle);
            ParticleSystem activeSubEmitter = activeParent.subEmitters.GetSubEmitterSystem(0);
            Assert.That(activeSubEmitter.particleCount, Is.GreaterThan(0));

            Assert.That(firstHandle.TryRelease(), Is.True);
            Assert.That(activeSubEmitter.particleCount, Is.EqualTo(0));

            VFXHandle secondHandle = _emitter.Spawn(Vector3.one, Quaternion.identity);

            Assert.That(activeSubEmitter.particleCount, Is.EqualTo(0));
            Assert.That(secondHandle.TryRelease(), Is.True);
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

        [Test]
        public void CustomLifetimeConfiguration_CreatesItsOwnSession()
        {
            var lifetime = new TrackingLifetimeConfiguration();
            _emitter.Initialize(VFXConfiguration.Instantiate(_prefab, lifetime));

            VFXHandle handle = _emitter.Spawn(Vector3.zero, Quaternion.identity);

            Assert.That(lifetime.CreatedSessionCount, Is.EqualTo(1));
            Assert.That(handle.IsValid, Is.True);
            Assert.That(handle.TryRelease(), Is.True);
        }

        [Test]
        public void ManualLifetimeConfiguration_CreatesDistinctSessions()
        {
            var context = new VFXLifetimeContext(_prefab.GetComponent<VFXInstance>());

            IVFXLifetimeSession first =
                ManualVFXLifetimeConfiguration.Instance.CreateSession(context);
            IVFXLifetimeSession second =
                ManualVFXLifetimeConfiguration.Instance.CreateSession(context);

            Assert.That(second, Is.Not.SameAs(first));
        }

        [Test]
        public void ProviderDispose_IsIdempotent()
        {
            var provider = new InstantiatedVFXProvider(
                _prefab,
                ManualVFXLifetimeConfiguration.Instance);
            VFXHandle handle = provider.Spawn(VFXSpawnOptions.At(
                Vector3.zero,
                Quaternion.identity));

            Assert.DoesNotThrow(provider.Dispose);
            Assert.DoesNotThrow(provider.Dispose);
            Assert.That(handle.IsValid, Is.False);
            Assert.That(handle.TryRelease(), Is.False);
        }

        [Test]
        public void LifetimeConfiguration_ReturningNullSession_RejectsSpawn()
        {
            _emitter.Initialize(VFXConfiguration.Instantiate(
                _prefab,
                new NullSessionLifetimeConfiguration()));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                _emitter.Spawn(Vector3.zero, Quaternion.identity));

            Assert.That(exception.Message, Does.Contain("null session"));
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

        private VFXInstance FindActiveVFXInstance()
        {
            VFXInstance[] instances = Object.FindObjectsByType<VFXInstance>(
                FindObjectsInactive.Exclude);
            Assert.That(instances, Has.Length.EqualTo(1));
            return instances[0];
        }

        private static ParticleSystem AddParticleSystem(
            GameObject target,
            bool loop,
            float duration,
            float startLifetime)
        {
            ParticleSystem particleSystem = target.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = loop;
            main.duration = duration;
            main.startLifetime = startLifetime;
            main.playOnAwake = false;
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = 50f;
            return particleSystem;
        }

        private sealed class TrackingLifetimeConfiguration : IVFXLifetimeConfiguration
        {
            internal int CreatedSessionCount { get; private set; }
            internal int DisposedSessionCount { get; private set; }

            public IVFXLifetimeSession CreateSession(in VFXLifetimeContext context)
            {
                CreatedSessionCount++;
                return new ManualSession(() => DisposedSessionCount++);
            }
        }

        private sealed class NullSessionLifetimeConfiguration : IVFXLifetimeConfiguration
        {
            public IVFXLifetimeSession CreateSession(in VFXLifetimeContext context) => null;
        }

        private sealed class ThrowingLifetimeConfiguration : IVFXLifetimeConfiguration
        {
            internal int DisposedSessionCount { get; private set; }

            public IVFXLifetimeSession CreateSession(in VFXLifetimeContext context) =>
                new ThrowingSession(() => DisposedSessionCount++);
        }

        private sealed class ThrowingTickAndDisposeLifetimeConfiguration :
            IVFXLifetimeConfiguration
        {
            public IVFXLifetimeSession CreateSession(in VFXLifetimeContext context) =>
                new ThrowingTickAndDisposeSession();
        }

        private sealed class ThrowingTickAndDisposeSession : IVFXLifetimeSession
        {
            public bool Tick(
                in VFXLifetimeContext context,
                float deltaTime,
                float unscaledDeltaTime) =>
                throw new InvalidOperationException("Lifetime tick failed.");

            public void Dispose() =>
                throw new InvalidOperationException("Lifetime dispose failed.");
        }

        private sealed class ThrowingSession : IVFXLifetimeSession
        {
            private readonly Action _onDispose;

            internal ThrowingSession(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public bool Tick(
                in VFXLifetimeContext context,
                float deltaTime,
                float unscaledDeltaTime) =>
                throw new InvalidOperationException("Lifetime tick failed.");

            public void Dispose()
            {
                _onDispose();
            }
        }

        private sealed class ManualSession : IVFXLifetimeSession
        {
            private readonly Action _onDispose;

            internal ManualSession(Action onDispose = null)
            {
                _onDispose = onDispose;
            }

            public bool Tick(
                in VFXLifetimeContext context,
                float deltaTime,
                float unscaledDeltaTime) => false;

            public void Dispose()
            {
                _onDispose?.Invoke();
            }
        }
    }
}

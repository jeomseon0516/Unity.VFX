using System;
using System.Collections.Generic;
using Jeomseon.Unity.GameObjectPooling.Handles;
using Jeomseon.Unity.GameObjectPooling.Scopes;
using UnityEngine;

namespace Jeomseon.Unity.VFX
{
    [DisallowMultipleComponent]
    public sealed class VFXEmitter : MonoBehaviour
    {
        [SerializeField] private VFXDefinition definition;
        [SerializeField] private GameObjectPoolScope poolScope;

        private readonly List<IVFXLifetimeHandler> _lifetimeHandlers = new();
        private IVFXProvider _provider;
        private bool _ownsProvider;

        public bool IsInitialized => _provider != null;

        private void Awake()
        {
            if (_provider != null || definition == null) return;
            Initialize(definition.CreateConfiguration());
        }

        public void RegisterLifetimeHandler(IVFXLifetimeHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_provider != null)
            {
                throw new InvalidOperationException(
                    "Lifetime handlers must be registered before initialization.");
            }

            EnsureBuiltInLifetimeHandlers();
            _lifetimeHandlers.Add(handler);
        }

        public void Initialize(VFXDefinition sourceDefinition)
        {
            if (sourceDefinition == null)
            {
                throw new ArgumentNullException(nameof(sourceDefinition));
            }

            Initialize(sourceDefinition.CreateConfiguration());
        }

        public void Initialize(VFXConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            EnsureNotInitialized();
            EnsureBuiltInLifetimeHandlers();
            IVFXLifetimeHandler lifetimeHandler = ResolveLifetimeHandler(
                configuration.LifetimeConfiguration);

            _provider = configuration.ReuseMode switch
            {
                VFXReuseMode.Instantiate => new InstantiatedVFXProvider(
                    configuration.Prefab,
                    configuration.LifetimeConfiguration,
                    lifetimeHandler,
                    configuration.PlaybackConfiguration),
                VFXReuseMode.Pool => new PooledVFXProvider(
                    RegisterPool(configuration),
                    configuration.LifetimeConfiguration,
                    lifetimeHandler,
                    configuration.PlaybackConfiguration),
                _ => throw new ArgumentOutOfRangeException()
            };
            _ownsProvider = true;
        }

        public void Initialize(
            VFXConfiguration configuration,
            GameObjectPoolScope runtimePoolScope)
        {
            if (runtimePoolScope == null)
            {
                throw new ArgumentNullException(nameof(runtimePoolScope));
            }

            poolScope = runtimePoolScope;
            Initialize(configuration);
        }

        public void Initialize(IVFXProvider provider, bool takeOwnership = false)
        {
            EnsureNotInitialized();
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _ownsProvider = takeOwnership;
        }

        public VFXHandle Spawn(in VFXSpawnOptions options)
        {
            if (_provider == null)
            {
                throw new InvalidOperationException("The VFX emitter is not initialized.");
            }

            return _provider.Spawn(options);
        }

        public VFXHandle Spawn(
            Vector3 position,
            Quaternion rotation,
            Transform parent = null) =>
            Spawn(VFXSpawnOptions.At(position, rotation, parent));

        private GameObjectPoolHandle RegisterPool(VFXConfiguration configuration)
        {
            if (poolScope == null)
            {
                throw new InvalidOperationException(
                    "A pooled VFX configuration requires a GameObjectPoolScope.");
            }

            return poolScope.Register(configuration.PoolRegistration);
        }

        private IVFXLifetimeHandler ResolveLifetimeHandler(
            IVFXLifetimeConfiguration configuration)
        {
            for (int i = _lifetimeHandlers.Count - 1; i >= 0; i--)
            {
                IVFXLifetimeHandler handler = _lifetimeHandlers[i];
                if (handler.CanHandle(configuration)) return handler;
            }

            throw new NotSupportedException(
                $"No VFX lifetime handler supports {configuration.GetType().FullName}.");
        }

        private void EnsureBuiltInLifetimeHandlers()
        {
            if (_lifetimeHandlers.Count > 0) return;
            _lifetimeHandlers.Add(new ManualVFXLifetimeHandler());
            _lifetimeHandlers.Add(new TimedVFXLifetimeHandler());
            _lifetimeHandlers.Add(new ParticleCompletionVFXLifetimeHandler());
        }

        private void EnsureNotInitialized()
        {
            if (_provider != null)
            {
                throw new InvalidOperationException(
                    "The VFX emitter is already initialized.");
            }
        }

        private void OnDestroy()
        {
            if (_ownsProvider && _provider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _provider = null;
        }
    }
}

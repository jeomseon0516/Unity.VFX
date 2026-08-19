using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.VFX
{
    public abstract class VFXProviderBase :
        IVFXProvider,
        IVFXHandleOwner,
        IDisposable
    {
        private readonly Dictionary<int, VFXInstance> _instances = new();
        private readonly IVFXLifetimeConfiguration _lifetimeConfiguration;
        private readonly IVFXLifetimeHandler _lifetimeHandler;
        private readonly VFXPlaybackConfiguration _playbackConfiguration;
        private int _nextInstanceId;
        private bool _disposed;

        protected VFXProviderBase(
            IVFXLifetimeConfiguration lifetimeConfiguration,
            IVFXLifetimeHandler lifetimeHandler,
            VFXPlaybackConfiguration playbackConfiguration)
        {
            _lifetimeConfiguration = lifetimeConfiguration ??
                throw new ArgumentNullException(nameof(lifetimeConfiguration));
            _lifetimeHandler = lifetimeHandler ??
                throw new ArgumentNullException(nameof(lifetimeHandler));
            _playbackConfiguration = playbackConfiguration ??
                VFXPlaybackConfiguration.Default;
        }

        public abstract VFXHandle Spawn(in VFXSpawnOptions options);

        public bool TryRelease(VFXHandle handle) => handle.TryRelease(this);

        protected VFXHandle Activate(VFXInstance instance, in VFXSpawnOptions options)
        {
            ThrowIfDisposed();
            if (!instance) throw new ArgumentNullException(nameof(instance));

            instance.transform.localScale = options.Scale;
            int instanceId = NextInstanceId();
            _instances.Add(instanceId, instance);
            try
            {
                uint generation = instance.BeginLease(
                    instanceId,
                    _lifetimeHandler,
                    _lifetimeConfiguration,
                    _playbackConfiguration,
                    TryReleaseFromLifetime);
                return new VFXHandle(this, instanceId, generation);
            }
            catch
            {
                _instances.Remove(instanceId);
                instance.EndLease(instance.Generation, _playbackConfiguration);
                ReleaseInstance(instance);
                throw;
            }
        }

        protected abstract void ReleaseInstance(VFXInstance instance);

        bool IVFXHandleOwner.IsValid(int instanceId, uint generation) =>
            TryResolve(instanceId, generation, out _);

        bool IVFXHandleOwner.TryRelease(int instanceId, uint generation) =>
            TryReleaseCore(instanceId, generation);

        bool IVFXHandleOwner.TrySetPose(
            int instanceId,
            uint generation,
            Vector3 position,
            Quaternion rotation)
        {
            if (!TryResolve(instanceId, generation, out VFXInstance instance)) return false;
            instance.transform.SetPositionAndRotation(position, rotation);
            return true;
        }

        bool IVFXHandleOwner.TrySetScale(
            int instanceId,
            uint generation,
            Vector3 scale)
        {
            if (!TryResolve(instanceId, generation, out VFXInstance instance)) return false;
            instance.transform.localScale = scale;
            return true;
        }

        bool IVFXHandleOwner.TrySetParent(
            int instanceId,
            uint generation,
            Transform parent,
            bool worldPositionStays)
        {
            if (!TryResolve(instanceId, generation, out VFXInstance instance)) return false;
            instance.transform.SetParent(parent, worldPositionStays);
            return true;
        }

        bool IVFXHandleOwner.TryPause(int instanceId, uint generation)
        {
            if (!TryResolve(instanceId, generation, out VFXInstance instance)) return false;
            instance.Pause();
            return true;
        }

        bool IVFXHandleOwner.TryResume(int instanceId, uint generation)
        {
            if (!TryResolve(instanceId, generation, out VFXInstance instance)) return false;
            instance.Resume();
            return true;
        }

        bool IVFXHandleOwner.TryStopEmission(
            int instanceId,
            uint generation,
            bool clearParticles)
        {
            if (!TryResolve(instanceId, generation, out VFXInstance instance)) return false;
            instance.StopEmission(clearParticles);
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            var active = new List<VFXInstance>(_instances.Values);
            _instances.Clear();
            foreach (VFXInstance instance in active)
            {
                if (!instance) continue;
                instance.EndLease(instance.Generation, _playbackConfiguration);
                ReleaseInstance(instance);
            }
        }

        private void TryReleaseFromLifetime(int instanceId, uint generation)
        {
            TryReleaseCore(instanceId, generation);
        }

        private bool TryReleaseCore(int instanceId, uint generation)
        {
            if (!TryResolve(instanceId, generation, out VFXInstance instance)) return false;
            if (!instance.EndLease(generation, _playbackConfiguration)) return false;

            _instances.Remove(instanceId);
            ReleaseInstance(instance);
            return true;
        }

        private bool TryResolve(
            int instanceId,
            uint generation,
            out VFXInstance instance)
        {
            if (_disposed ||
                !_instances.TryGetValue(instanceId, out instance) ||
                !instance ||
                !instance.IsCurrentLease(instanceId, generation))
            {
                instance = null;
                return false;
            }

            return true;
        }

        private int NextInstanceId()
        {
            do
            {
                _nextInstanceId = unchecked(_nextInstanceId + 1);
                if (_nextInstanceId == 0) _nextInstanceId = 1;
            } while (_instances.ContainsKey(_nextInstanceId));

            return _nextInstanceId;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);
        }
    }
}

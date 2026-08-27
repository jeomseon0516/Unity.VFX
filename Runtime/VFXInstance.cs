using System;
using System.Collections.Generic;
using System.Threading;
using Jeomseon.Unity.GameObjectPooling.Lifecycle;
using UnityEngine;

namespace Jeomseon.Unity.VFX
{
    [DisallowMultipleComponent]
    public sealed class VFXInstance : MonoBehaviour, IPoolReleaseHandler
    {
        private ParticleSystem[] _particleSystems;
        private IVFXLifetimeSession _lifetimeSession;
        private CancellationTokenSource _lifetimeCancellation;
        private Action<int, uint> _requestRelease;

        internal int RuntimeId { get; private set; }
        internal uint Generation { get; private set; }
        internal bool IsLeased { get; private set; }
        internal bool HasParticleSystems => _particleSystems is { Length: > 0 };

        internal uint BeginLease(
            int runtimeId,
            IVFXLifetimeConfiguration lifetimeConfiguration,
            VFXPlaybackConfiguration playbackConfiguration,
            Action<int, uint> requestRelease)
        {
            if (IsLeased)
            {
                throw new InvalidOperationException("The VFX instance is already leased.");
            }

            RuntimeId = runtimeId;
            Generation = NextGeneration(Generation);
            IsLeased = true;
            _requestRelease = requestRelease ??
                throw new ArgumentNullException(nameof(requestRelease));
            _particleSystems = playbackConfiguration.PlayChildren
                ? GetComponentsInChildren<ParticleSystem>(includeInactive: true)
                : GetComponents<ParticleSystem>();

            PlayParticleSystems(playbackConfiguration);

            var context = new VFXLifetimeContext(this);
            _lifetimeSession = lifetimeConfiguration.CreateSession(context) ??
                throw new InvalidOperationException(
                    "The VFX lifetime configuration returned a null session.");
            _lifetimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                destroyCancellationToken);
            RunLifetimeAsync(_lifetimeCancellation.Token);
            return Generation;
        }

        internal bool EndLease(uint generation, VFXPlaybackConfiguration playbackConfiguration)
        {
            if (!IsLeased || Generation != generation) return false;

            IsLeased = false;
            _requestRelease = null;
            CancelLifetime();
            _lifetimeSession?.Dispose();
            _lifetimeSession = null;
            StopParticleSystems(playbackConfiguration.ClearOnRelease);
            return true;
        }

        internal bool IsCurrentLease(int runtimeId, uint generation) =>
            this && IsLeased && RuntimeId == runtimeId && Generation == generation;

        internal bool IsAnyParticleAlive(bool includeChildren)
        {
            if (_particleSystems == null) return false;
            foreach (ParticleSystem particleSystem in _particleSystems)
            {
                if (particleSystem && particleSystem.IsAlive(includeChildren)) return true;
            }

            return false;
        }

        internal void Pause()
        {
            if (_particleSystems == null) return;
            foreach (ParticleSystem particleSystem in _particleSystems)
            {
                if (particleSystem) particleSystem.Pause(withChildren: false);
            }
        }

        internal void Resume()
        {
            if (_particleSystems == null) return;
            foreach (ParticleSystem particleSystem in _particleSystems)
            {
                if (particleSystem) particleSystem.Play(withChildren: false);
            }
        }

        internal void StopEmission(bool clearParticles)
        {
            StopParticleSystems(clearParticles);
        }

        /// <summary>
        /// Called by <c>Jeomseon.Unity.GameObjectPooling</c> right before a pooled instance is
        /// stored, so the next reuse never observes trail/animation state left over from a
        /// previous spawn. Only fires for the pooled path — <see cref="InstantiatedVFXProvider"/>
        /// always creates a fresh instance, so there is nothing to reset there. A user component
        /// that needs the same guarantee should implement <see cref="IPoolReleaseHandler"/> (or
        /// <see cref="IPoolGetHandler"/>) itself and sit on the VFX prefab's root, matching where
        /// this pooling package looks for handlers.
        /// </summary>
        void IPoolReleaseHandler.OnReleaseToPool()
        {
            ResetTrailRenderers();
            ResetAnimator();
        }

        private void ResetTrailRenderers()
        {
            foreach (TrailRenderer trailRenderer in
                     GetComponentsInChildren<TrailRenderer>(includeInactive: true))
            {
                if (trailRenderer) trailRenderer.Clear();
            }
        }

        private void ResetAnimator()
        {
            foreach (Animator animator in GetComponentsInChildren<Animator>(includeInactive: true))
            {
                if (!animator) continue;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private async void RunLifetimeAsync(CancellationToken cancellationToken)
        {
            try
            {
                // An async method also runs synchronously up to its first await, so without this
                // the very first Tick (e.g. a Timed lifetime with Duration 0) could fire before
                // BeginLease even returns, handing the caller an already-invalid VFXHandle.
                await Awaitable.NextFrameAsync(cancellationToken);

                var context = new VFXLifetimeContext(this);
                while (IsLeased)
                {
                    // Unlike a Coroutine, this loop keeps running while the GameObject is inactive.
                    // Skip ticking in that case so an externally deactivated instance can't be read
                    // as "particles finished" by the particle-completion session merely because
                    // simulation is paused.
                    if (isActiveAndEnabled &&
                        _lifetimeSession.Tick(context, Time.deltaTime, Time.unscaledDeltaTime))
                    {
                        _requestRelease?.Invoke(RuntimeId, Generation);
                        return;
                    }

                    await Awaitable.NextFrameAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // The lease ended (EndLease/OnDestroy) before the lifetime completed naturally.
            }
        }

        private void CancelLifetime()
        {
            if (_lifetimeCancellation == null) return;
            _lifetimeCancellation.Cancel();
            _lifetimeCancellation.Dispose();
            _lifetimeCancellation = null;
        }

        private void OnDestroy()
        {
            IsLeased = false;
            _requestRelease = null;
            CancelLifetime();
            _lifetimeSession?.Dispose();
            _lifetimeSession = null;
        }

        private void PlayParticleSystems(VFXPlaybackConfiguration configuration)
        {
            HashSet<ParticleSystem> subEmitterSystems = CollectSubEmitterSystems();
            foreach (ParticleSystem particleSystem in _particleSystems)
            {
                if (!particleSystem) continue;
                ParticleSystem.MainModule main = particleSystem.main;
                main.simulationSpeed = configuration.PlaybackSpeed;
                if (configuration.ClearBeforePlay)
                {
                    particleSystem.Clear(withChildren: false);
                }

                if (!subEmitterSystems.Contains(particleSystem))
                {
                    particleSystem.Play(withChildren: false);
                }
            }
        }

        private HashSet<ParticleSystem> CollectSubEmitterSystems()
        {
            var result = new HashSet<ParticleSystem>();
            foreach (ParticleSystem particleSystem in _particleSystems)
            {
                if (!particleSystem) continue;
                ParticleSystem.SubEmittersModule subEmitters = particleSystem.subEmitters;
                for (int index = 0; index < subEmitters.subEmittersCount; index++)
                {
                    ParticleSystem subEmitter = subEmitters.GetSubEmitterSystem(index);
                    if (subEmitter) result.Add(subEmitter);
                }
            }

            return result;
        }

        private void StopParticleSystems(bool clearParticles)
        {
            if (_particleSystems == null) return;
            ParticleSystemStopBehavior behavior = clearParticles
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting;
            foreach (ParticleSystem particleSystem in _particleSystems)
            {
                if (particleSystem) particleSystem.Stop(withChildren: false, behavior);
            }
        }

        private static uint NextGeneration(uint generation)
        {
            generation = unchecked(generation + 1);
            return generation == 0 ? 1 : generation;
        }
    }
}

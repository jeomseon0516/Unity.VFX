# Jeomseon Unity VFX

[한국어](Overview.md) · [Installation](../README.en.md)

## Purpose

Combines ParticleSystem effect creation, reuse, completion, and release with GameObjectPooling.

## Usage flow

Put VFXInstance on the Prefab root, configure VFXConfiguration/Definition, initialize an Emitter, Spawn a VFXHandle, then complete automatically or TryRelease. Callers own external Providers unless ownership is transferred.

## Contracts and limitations

Choose manual, timed, or particle-completion policies. Looping particles do not finish. Stale Handles cannot affect a new reuse generation. Create an independent custom lifetime Session per Spawn.

## Package requirements

- Package ID: `com.jeomseon.unity.vfx`
- Version: `0.4.0`
- Unity: `6000.6` / `0f1`
- `com.jeomseon.unity.game-object-pooling`: `0.4.0`

## Samples and verification

Import the following from Package Manager > Samples. Open the included Scene or Editor tool; follow any Setup step in its sample README.

- [Basic Usage](../Samples~/BasicUsage/README.md)
  - [VFXBasicUsageSample](../Samples~/BasicUsage/VFXBasicUsageSample.unity)

[Test sources](../Tests)

Add the package ID to manifest `testables`, then run the relevant Unity Test Runner assemblies. These links describe available tests, not a new successful test run.

## Source map

- [InstantiatedVFXProvider](../Runtime/InstantiatedVFXProvider.cs)
- [IVFXHandleOwner](../Runtime/IVFXHandleOwner.cs)
- [IVFXLifetimeConfiguration](../Runtime/IVFXLifetimeConfiguration.cs)
- [IVFXLifetimeSession](../Runtime/IVFXLifetimeSession.cs)
- [IVFXProvider](../Runtime/IVFXProvider.cs)
- [ManualVFXLifetimeConfiguration](../Runtime/ManualVFXLifetimeConfiguration.cs)
- [ManualVFXLifetimeDefinition](../Runtime/ManualVFXLifetimeDefinition.cs)
- [ParticleCompletionVFXLifetimeConfiguration](../Runtime/ParticleCompletionVFXLifetimeConfiguration.cs)
- [ParticleCompletionVFXLifetimeDefinition](../Runtime/ParticleCompletionVFXLifetimeDefinition.cs)
- [PooledVFXProvider](../Runtime/PooledVFXProvider.cs)
- [TimedVFXLifetimeConfiguration](../Runtime/TimedVFXLifetimeConfiguration.cs)
- [TimedVFXLifetimeDefinition](../Runtime/TimedVFXLifetimeDefinition.cs)
- [VFXConfiguration](../Runtime/VFXConfiguration.cs)
- [VFXDefinition](../Runtime/VFXDefinition.cs)
- [VFXEmitter](../Runtime/VFXEmitter.cs)
- [VFXHandle](../Runtime/VFXHandle.cs)
- [VFXInstance](../Runtime/VFXInstance.cs)
- [VFXLifetimeContext](../Runtime/VFXLifetimeContext.cs)
- [VFXLifetimeDefinition](../Runtime/VFXLifetimeDefinition.cs)
- [VFXPlaybackConfiguration](../Runtime/VFXPlaybackConfiguration.cs)
- [VFXProviderBase](../Runtime/VFXProviderBase.cs)
- [VFXReuseMode](../Runtime/VFXReuseMode.cs)
- [VFXSpawnOptions](../Runtime/VFXSpawnOptions.cs)
- [VFXTimeMode](../Runtime/VFXTimeMode.cs)

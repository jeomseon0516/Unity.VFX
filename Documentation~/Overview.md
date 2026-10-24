# Jeomseon Unity VFX

[English](Overview.en.md) · [설치](../README.md)

## 목적

ParticleSystem 효과의 생성·재사용·완료·반환을 GameObjectPooling과 조합합니다.

## 사용 흐름

Prefab root에 VFXInstance → VFXConfiguration 또는 Definition → Emitter 초기화 → Spawn으로 VFXHandle 획득 → 자동 완료 또는 TryRelease. 외부 Provider를 소유하지 않으면 호출자가 정리합니다.

## 계약과 제한

수동·시간·입자완료 정책을 구분합니다. 무한 반복 입자는 완료되지 않습니다. 오래된 Handle은 재사용된 새 효과를 조작하지 않습니다. 사용자 lifetime Session은 매 Spawn마다 독립 생성해야 합니다.

## 패키지 요구 사항

- Package ID: `com.jeomseon.unity.vfx`
- Version: `0.4.0`
- Unity: `6000.6` / `0f1`
- `com.jeomseon.unity.game-object-pooling`: `0.4.0`

## 샘플과 검증

Package Manager > Samples에서 다음 예제를 Import하세요. 포함된 Scene 또는 Editor 도구를 열고 샘플 README의 Setup 절차를 먼저 수행합니다.

- [Basic Usage](../Samples~/BasicUsage/README.md)
  - [VFXBasicUsageSample](../Samples~/BasicUsage/VFXBasicUsageSample.unity)

[테스트 코드](../Tests)

소비 프로젝트 manifest의 `testables`에 패키지 ID를 추가하고 Unity Test Runner의 관련 어셈블리를 실행합니다. 위 링크는 테스트 위치이며 이번 문서 수정으로 새 테스트 실행을 주장하지 않습니다.

## 코드 지도

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

# 변경 기록

## [Unreleased]

- **(렌더 파이프라인)** 워크스페이스 Unity `6000.6` + URP `17.6` 전환. 패키지 런타임은
  `ParticleSystem` 기반이라 셰이더·SRP 코드가 없어 코드 변경은 없습니다.
- Basic Usage 샘플의 `SampleVFXMaterial.mat`이 Built-in `Particles/Standard Unlit`(shader fileID
  211)을 참조해 URP에서 magenta로 렌더되던 문제를 고쳤습니다. 파이프라인 비종속인 `Sprites/Default`
  (fileID 10753)로 바꿔 파티클 vertex color를 그대로 표시합니다. Unity 6000.6 + URP 17.6 Play
  Mode에서 좌우 청록색 파티클의 반복 생성·반환을 확인했습니다.
- `package.json` 최소 Unity 버전 `6000.6.0f1`.

## [0.4.0] - 2026-09-01

- **(Breaking)** 빈 marker였던 `IVFXLifetimeConfiguration`에 lease별
  `CreateSession(in VFXLifetimeContext)` 실행 계약을 추가하고 별도 `IVFXLifetimeHandler` 계층과
  `VFXEmitter.RegisterLifetimeHandler`를 제거했습니다. 사용자 수명 확장은 Configuration 하나가
  불변 설정과 Session 생성을 함께 책임지므로 Handler 등록 누락에 따른 런타임 탐색 실패가 없습니다.
- **(Breaking)** 자체 저장소와 Singleton을 사용하던 `VFXPool`을 제거했습니다.
- **(Breaking)** `PooledVFX`를 풀링 여부와 무관한 `VFXInstance`로 교체했습니다.
- `VFXDefinition`과 불변 런타임 `VFXConfiguration`이 Inspector·하드코딩 설정 경로를
  같은 `VFXEmitter` 초기화 계약으로 통합합니다.
- `IVFXProvider` 도메인 계약과 `PooledVFXProvider`, `InstantiatedVFXProvider` 구현을 추가했습니다.
- 풀링 구현은 `Jeomseon.Unity.GameObjectPooling` 최상위 Provider API에 위임합니다.
- 사용자에게 Unity `GameObject`/`MonoBehaviour`를 반환하지 않고 generation을 검증하는
  `readonly struct VFXHandle`을 반환합니다. 이전 대여의 Handle은 재사용된 VFX를
  종료하거나 조작할 수 없습니다.
- Particle 완료·수동·시간(Scaled/Unscaled) 수명 Configuration과 lease별 Session을 추가했습니다.
- 마이그레이션: `VFXPool.Configure/Spawn/Despawn`을 `GameObjectPoolScope`에서 구성한
  `VFXEmitter.Spawn`으로 이전하고 반환된 `VFXHandle.TryRelease()`로 명시적 종료합니다.
  풀이 필요 없으면 `VFXConfiguration.Instantiate`를 사용합니다.
- 두 생성 전략의 자동 종료와 수동 반환을 확인하는 PlayMode 테스트와 Scene Sample을
  추가했습니다.
- `Duration 0`인 Timed 수명처럼 첫 Tick에서 즉시 만료되는 설정이 리스 시작 전에 만료를
  확정해 `Spawn()`이 이미 무효화된 `VFXHandle`을 돌려주는 경합을 고쳤습니다. 첫 Tick을
  항상 다음 프레임으로 미룹니다.
- `VFXInstance`의 수명 Tick 루프를 `Coroutine`에서 `Awaitable`
  (`Awaitable.NextFrameAsync` + `CancellationTokenSource`)로 교체했습니다. 리스 중인 인스턴스를
  외부에서 `SetActive(false)`로 끄는 것은 지원 대상 사용법이 아니지만, `Awaitable` 루프는
  GameObject 비활성 중에도 계속 진행되므로 그렇게 오용됐을 때 Particle Completion Session이
  "시뮬레이션 멈춤"을 "재생 완료"로 오판해 조기 반환하지 않도록 `isActiveAndEnabled`일 때만
  Tick을 진행하는 최소 방어를 추가했습니다.
- 활성 VFX보다 Pool Scope가 먼저 파괴되는 경우에도 `VFXInstance.OnDestroy()`가 진행 중인 수명
  Session을 정리합니다. Emitter 우선 제거, Scope 우선 제거, Additive Scene 종료 순서를 검사하는
  PlayMode 테스트를 추가했습니다.
- 풀링된 `VFXInstance`가 GameObjectPooling의 `IPoolReleaseHandler`를 구현해 풀에 반환되기
  직전 `TrailRenderer.Clear()`와 `Animator.Rebind()`를 자동 호출합니다 — 이전 스폰의 잔상
  궤적·애니메이션 포즈가 다음 재사용에 남지 않습니다. 리셋이 필요한 사용자 컴포넌트는 같은
  `IPoolGetHandler`/`IPoolReleaseHandler`를 VFX 프리팹 **루트**에 직접 구현하면 됩니다(자식에
  두면 GameObjectPooling이 호출하지 않습니다).
- 모든 자식 `Animator`를 초기화하도록 보강하고, 반복 Particle의 유지, 자식 Particle 완료 대기,
  수동 반환의 중복 방지, 재사용 시 Particle/Trail 초기화를 자동 테스트로 고정했습니다.
- 부모 `ParticleSystem`이 관리하는 Sub Emitter를 일반 자식처럼 직접 `Play()`하지 않도록 구분합니다.
  Sub Emitter는 완료 확인 대상에는 계속 포함돼, 발생한 자식 입자가 끝난 뒤 VFX가 반환됩니다.
- Basic Usage Sample을 실제 one-shot ParticleSystem Prefab으로 교체했습니다. 한 Scene에서
  `VFXConfiguration.Pool(...)` 런타임 구성과 `VFXDefinition`/Pool/Lifetime ScriptableObject 구성을
  나란히 실행하며 Particle Completion 자동 반환과 Pool 재사용을 확인합니다.

## [0.3.0] - 2026-08-13

- **(Breaking)** Runtime 네임스페이스를 패키지 규칙에 맞춰 `Jeomseon.Unity.VFX`로 변경했습니다.
  이전 `Jeomseon.VFX` 호환 별칭은 제공하지 않습니다.

## [0.2.0] - 2026-08-11

- **(Breaking)** 워크스페이스 명명 규칙에 맞춰 `PooledVFX.fallbackLifetime`(public 필드)을
  `FallbackLifetime`(PascalCase)로 정리했습니다. 기존 이름을 `[FormerlySerializedAs]`로 보존해
  기존 Scene·Prefab의 직렬화된 값은 그대로 유지되지만, 코드에서 이 필드를 직접 참조하던 외부
  소비처가 있다면 이름을 갱신해야 합니다.

## [0.1.2] - 2026-07-29

- asmdef의 `rootNamespace`와 VFX 파일 위치를 namespace에 맞게 정리했습니다.

## [0.1.1] - 2026-07-29

- PooledVFX 자동 회수 흐름을 확인하는 `Basic Usage` 샘플을 추가했습니다.

## [0.1.0] - 2026-07-29

- JeomseonScriptPack의 관련 모듈을 독립 UPM 패키지로 분리했습니다.


## [0.1.3] - 2026-08-05

- Unity 6000.5.7f1을 최소 지원 버전으로 상향했습니다.

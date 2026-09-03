# VFX 로드맵

우선순위: `P0` 결함·안전성 → `P1` 핵심 구조 → `P2` API·성능 → `P3` 장기 확장

## 작업 순서

1. **완료 — P1-01 — GameObjectPooling 고수준 API 통합**
   - 자체 `VFXPool`을 제거하고 `ComponentPoolProvider<VFXInstance>` 합성 기반으로 전환했습니다.
   - prefab, prewarm, max inactive, overflow 및 scene lifetime은 GameObjectPooling Definition·Scope가 담당합니다.
   - 풀을 사용하지 않는 `InstantiatedVFXProvider` 경로도 같은 `IVFXProvider`로 제공합니다.
   - `VFXDefinition`과 `VFXConfiguration`으로 Inspector·코드 설정 경로를 동등하게 제공합니다.
   - `VFXEmitter` 외의 DI Service·Installer·프로젝트 VFX Catalog는 패키지 경계에 포함하지 않습니다.
2. **완료 — P0-01 — Provider 수명과 씬 전환 안정화** (2026-08-27)
   - Domain Reload 비활성화, Additive Scene, Scope dispose 및 종료 중 활성 VFX 상태를 검증합니다.
   - **완료(테스트 통과) — Scope/Emitter 제거 및 Additive Scene 종료.** 활성 VFX가
     있는 상태에서 Emitter가 먼저 제거되는 경우, Pool Scope가 먼저 제거되는 경우, 별도로 불러온
     Scene 전체가 내려가는 경우를 PlayMode 테스트로 추가했습니다. Scope가 먼저 제거하면 풀 안의
     `VFXInstance`가 Provider보다 먼저 파괴되므로, `VFXInstance.OnDestroy()`가 진행 중인 수명 Session을
     직접 `Dispose()`하도록 보강했습니다. 이후 Emitter가 제거돼도 두 번 정리되지 않는 것까지 검사합니다.
     사용자가 Unity 6000.5.7f1 Test Runner에서 신규 3개를 포함한 **VFX PlayMode 테스트 11개 전체
     통과**를 확인했습니다(2026-08-27). 현재 `VFXBasicUsageSample` Scene도 저장 완료했습니다.
   - **완료(사용자 실측) — Domain Reload 비활성화 반복 실행.** Domain Reload를 끈 상태에서
     `VFXBasicUsageSample`의 Play/Stop을 반복해도 두 Emitter와 Scope가 다시 초기화되고 오류가 발생하지
     않는 것을 사용자가 확인했습니다(2026-08-27). P0-01에 남은 검증 항목은 없습니다.
3. **완료 — P1-02 — 재설정 계약** (2026-08-27 자동 검증 완료)
   - `ParticleSystem`은 이미 `PlayParticleSystems`/`StopParticleSystems`(`EndLease` 시
     `ClearOnRelease` 설정에 따라 Clear 여부 선택)로 구현돼 있었습니다.
   - **`TrailRenderer`·`Animator`는 풀 재사용 시 무조건 리셋합니다**(Particle의 `ClearOnRelease`처럼
     선택형이 아닙니다 — 이전 스폰의 잔상 궤적·애니메이션 포즈를 그대로 재사용하는 것은 항상
     잘못된 상태라고 판단했습니다). `VFXInstance`가 GameObjectPooling의
     `Lifecycle.IPoolReleaseHandler`를 구현해 풀에 반환되기 직전(`OnReleaseToPool`)
     `GetComponentsInChildren<TrailRenderer>().Clear()`와
     모든 `GetComponentsInChildren<Animator>()`에 `Rebind()`+`Update(0f)`를 호출합니다. **재사용하지 않는
     `InstantiatedVFXProvider` 경로는 애초에 매번 새 인스턴스라 대상이 아닙니다.**
   - **사용자 컴포넌트는 VFX 전용 훅을 새로 만들지 않고 GameObjectPooling이 이미 제공하는
     `IPoolGetHandler`/`IPoolReleaseHandler`를 그대로 재사용하는 것이 계약입니다** —
     `UnityGameObjectPool`이 풀 Get/Release 시 자동으로 호출해줍니다(AGENTS.md: 동일 기능
     재구현 금지). **제약**: 이 스캔은 루트의 `GetComponents<T>()`만 보고 자식은 보지 않습니다
     (`PooledGameObjectState.Initialize`). `VFXInstance` 자신은 프리팹 루트에 있어야 하므로
     (`VFXConfiguration`의 프리팹 검증 조건) 문제 없지만, 리셋이 필요한 사용자 컴포넌트는
     **VFX 프리팹의 루트**에 둬야 자동 호출됩니다 — 자식에 두면 훅이 호출되지 않는다는 점을
     한·영 README의 프리팹 준비 및 재사용 계약에 기록했습니다(2026-08-27).
   - Particle/Trail은 PlayMode 재사용 테스트로, Animator는 실제 AnimationClip과
     RuntimeAnimatorController를 만드는 Editor 테스트로 검증했습니다. PlayMode 16/16과 VFX Editor
     1/1에 포함되어 통과했습니다.
4. **완료 — P2-01 — 자동 반환 신뢰성** (2026-08-27 자동 검증 완료)
   - loop, sub-emitter, timeScale, 비활성화 및 수동 Despawn 시나리오를 검증합니다.
   - Manual·Timed(Scaled/Unscaled)·Particle Completion Configuration과 generation Handle은 구현했습니다.
   - **완료 — Timed `Duration 0` 경합 수정.** `StartCoroutine`이 첫 `yield`까지 동기 실행되는
     Unity 특성 때문에 즉시 만료되는 수명이 `BeginLease` 반환 전에 만료를 확정해 `Spawn()`이
     이미 무효화된 `VFXHandle`을 반환할 수 있었습니다(Particle Completion Session은
     `_observedFirstFrame`으로 이미 방어하고 있었지만 Timed Session은 방어가 없었음).
     `VFXInstance.RunLifetime()`의 첫 Tick을 프레임 1회 미루는 방식으로 프레임워크 레벨에서
     고쳤고(개별 수명 Session이 각자 방어할 필요 없음), 회귀 테스트
     `PooledTimedLifetime_ZeroDuration_HandleIsValidImmediatelyAfterSpawn`을 추가했습니다.
     PlayMode 16/16에 포함되어 통과했습니다.
   - **완료(코드) — Lifetime 확장 계약 단순화 (2026-08-26).** 빈 marker였던
     `IVFXLifetimeConfiguration`이 lease별 `IVFXLifetimeSession`을 직접 생성하도록 변경했습니다.
     별도 `IVFXLifetimeHandler`/`CanHandle` 탐색과 `VFXEmitter.RegisterLifetimeHandler`를 제거해,
     사용자 확장은 Configuration 하나만 구현하며 등록 누락이 `NotSupportedException`으로 늦게
     드러나지 않습니다. 사용자 정의 Configuration 경로와 null Session 방어 테스트를 추가했습니다.
     사용자 정의 Configuration과 null Session 방어를 포함한 PlayMode 16/16이 통과했습니다.
   - **완료 — 반복·자식 Particle·수동 반환·재사용 상태 검증 (2026-08-27).** 반복 재생 중인
     `ParticleSystem`은 자동 완료로 오판하지 않고, 자식 `ParticleSystem`은 실제 재생이 끝날 때까지
     `VFXHandle`을 유지하며, `VFXHandle.TryRelease()`는 첫 호출만 성공하고 수명 Session을 한 번만
     정리합니다. 같은 `VFXInstance`를 다시 꺼냈을 때 이전 Particle과 Trail이 남지 않는 것도
     검사합니다.
   - `VFXInstance`의 풀 반환 처리가 첫 번째 자식 `Animator`만 초기화하던 누락을 수정해 모든 자식
     `Animator`에 `Rebind()`와 `Update(0f)`를 적용합니다. 실제 AnimationClip과 AnimatorController를
     임시 생성해 두 Animator가 모두 초기 위치로 돌아오는 **Editor 테스트 1/1이 통과**했으며,
     테스트가 만든 임시 자산은 종료 시 제거됩니다.
   - **완료 — 실제 Sub Emitter 검증.** `VFXInstance`가 부모 `ParticleSystem`이 관리해야 하는
     Sub Emitter까지 일반 자식처럼 직접 `Play()`하던 문제를 발견했습니다. 등록된 Sub Emitter는 직접
     재생 대상에서 제외하되 완료 확인 대상에는 유지하도록 수정했습니다. Manual Sub Emitter를 실제로
     발생시켜 자식 입자가 살아 있는 동안 Handle이 유지되고 종료 뒤 반환되는 테스트를 추가했으며,
     최종 **PlayMode 16/16이 통과**했습니다.
5. **P3-01 — Visual Effect Graph 지원**
   - VFX Graph가 설치된 경우에만 동작하는 선택 어댑터를 검토합니다.
6. **완료 — P3-02 — 수명 Tick 루프를 `Coroutine`에서 `Awaitable`로 전환** (사용자 지시,
   2026-08-27 PlayMode 검증 완료)
   - Runtime의 유일한 코루틴이던 `VFXInstance.RunLifetime()`을
     `async void RunLifetimeAsync(CancellationToken)` + `Awaitable.NextFrameAsync`로 교체했습니다.
     Sample의 주기적 스폰 루프(`PooledVFXSample.Start()`)와 Tests의 `UnityTest`는 전환 대상이
     아니었습니다 — 전자는 임의 예제 코드, 후자는 Unity Test Framework 자체 요구사항입니다.
   - `Coroutine` 참조 대신 `MonoBehaviour.destroyCancellationToken`에 연결한
     `CancellationTokenSource`로 취소를 표현합니다. `EndLease`가 `Cancel()`+`Dispose()`로
     정지시키고, `OnDestroy`가 `EndLease`를 거치지 않고 GameObject가 파괴되는
     경로(예: Scope가 우리 Provider보다 먼저 자신의 풀 인스턴스를 직접 파괴하는 경우)까지
     안전망으로 커버합니다. 이 경로에서도 CancellationToken뿐 아니라 사용자 수명 Session을
     정리해야 하므로 `Dispose()`를 함께 호출합니다. 코루틴 시절엔 이런 `OnDestroy` 정리가 없어도
     Unity가 자동으로 코루틴을 멈춰줬지만, 이제는 명시적 취소와 Session 정리가 모두 필요합니다.
   - "첫 프레임 지연" 방어(구 `yield return null`)는 `await Awaitable.NextFrameAsync(token)`으로
     그대로 유지했습니다 — `Awaitable`도 첫 `await`까지 동기 실행되는 동일한 특성이 있습니다.
   - **완료 — 외부 비활성화(`SetActive(false)`) 오용에 대한 최소 방어.** 리스 중인
     `VFXInstance`를 외부에서 `SetActive(false)`로 끄는 것은 **애초에 지원 대상 사용법이
     아닙니다**(사용자 확정, 2026-08-19) — "숨긴 동안 일시정지"류 기능을 제공하려는 목적이
     아닙니다. 다만 코루틴 시절엔 그렇게 오용해도 Unity가 자동으로 코루틴을 멈춰 최소한
     조용히 멎기라도 했던 반면, `Awaitable` 기반 루프는 GameObject 활성 상태와 무관하게 계속
     진행되어 Particle Completion Session이 "시뮬레이션이 멈춰서 살아있는 파티클이
     없음"을 "재생 완료"로 오판해 숨겨진 채로 조기 반환될 수 있었습니다. 정식 지원 계약을
     추가하는 대신, `VFXInstance.RunLifetimeAsync()`에서 `isActiveAndEnabled`일 때만
     `_lifetimeSession.Tick(...)`을 호출하는 최소 가드만 추가해 이 오판을 막았습니다(비활성
     중엔 Tick 자체를 건너뜁니다). 사용자가 이 경로를 실제로 쓰지 않는 한 동작하지 않는
     방어 코드이며, 별도의 검증·문서화 대상 기능으로 승격하지 않습니다.
   - **API 설계 원칙 확정(사용자, 2026-08-19): 외부 사용자와의 API 계약은 native C# 객체로만
     공개하고, `VFXInstance`/`GameObject`/`Transform` 등 Unity Object는 넘기지 않습니다.**
     이렇게 하면 사용자가 `SetActive(false)`·`Destroy()` 등 Unity Object 수명 조작을 우리
     Try* 계약 밖에서 직접 호출할 수 있는 통로 자체가 API 표면에 없습니다. 현재 공개 API
     (`VFXHandle`의 `Try*` 메서드, `IVFXProvider.Spawn/TryRelease`, `VFXEmitter.Spawn`)를 다시
     확인한 결과 이미 이 원칙을 지키고 있습니다 — `Spawn()`은 `VFXHandle` 구조체만 반환하고
     내부 `VFXInstance`/`GameObject`는 어디서도 사용자에게 노출되지 않습니다.
     **단, 이 원칙으로도 막을 수 없는 간접 경로가 하나 남습니다**: `VFXHandle.TrySetParent(Transform
     parent, ...)`는 호출자가 소유한 부모 `Transform`을 받는데, 사용자가 나중에 그 **부모**를
     직접 `SetActive(false)`로 끄면 자식인 VFX 인스턴스도 `activeInHierarchy`가 false가
     됩니다(우리 API로 직접 인스턴스를 끈 게 아니라 사용자가 이미 소유한 오브젝트를 끈 것뿐이라
     원칙적으로 막을 수 없음). 위 `isActiveAndEnabled` Tick 가드는 바로 이 간접 경로를 위한
     방어이며, 그래서 "지원 대상 사용법이 아님"에도 계속 유효합니다. 이후 `VFXProviderBase`/
     `IVFXHandleOwner` 등 기반 타입 공개 범위를 다시 검토할 때도 이 원칙(native C# 계약만
     노출)을 기준으로 판단합니다.
   - 관련 경로는 최종 PlayMode 16/16에 포함되어 통과했습니다.
   - **Kilo PR 리뷰 후속 보강 (2026-08-31).** Manual Configuration도 lease마다 독립 Session을
     생성하도록 singleton Session을 제거했습니다. 사용자 Session의 `Tick()`이 예외를 던지면
     예외를 Unity Console에 기록하고 해당 VFX lease를 정리하도록 `RunLifetimeAsync`에 방어를
     추가했습니다. Instantiate 방식 Emitter 제거, Scaled 수명의 `Time.timeScale = 0`, Provider의
     중복 `Dispose()`, Session 예외 정리 및 Manual Session 독립성을 검증하는 테스트 5개를
     추가했습니다. 생성 Unity csproj 보조 빌드는 경고·오류 0건입니다. Unity CLI Test Runner는
     샌드박스와 승인 환경 모두 Licensing Client 연결 실패로 테스트 시작 전에 중단돼 결과 XML이
     생성되지 않았습니다.
   - **Kilo 재검토 후속 보강 (2026-08-31).** GameObjectPooling에 Pool 종료 시 활성 인스턴스를
     함께 파괴하는 기본 정책과 남기는 Preserve 정책을 추가했습니다. Preserve를 선택한 VFX는 Scope
     종료 후에도 계속 살아 있고, 사용자가 `ManualVFXLifetimeConfiguration`과
     `VFXHandle.TryRelease()`로 직접 종료하면 무효 Pool Handle 대신 GameObject를 파괴합니다.
     이 연동 경로를 PlayMode 테스트로 추가했습니다. 하위 Sub Emitter 입자가 반환 시 제거되고 같은
     인스턴스 재사용 시 남지 않는 회귀 테스트도 추가했습니다. 또한 수명 Tick 실패 후 반환 처리나
     사용자 Session `Dispose()`까지 실패하더라도 각 예외를 기록하고 `async void` 밖으로 전파하지
     않으며, Session 참조를 먼저 제거해 중복 Dispose하지 않도록 보강했습니다.
     테스트 준비의 Unity Object null 비교와 Sub Emitter Emission 설정을 바로잡은 뒤, 사용자가 신규
     3개를 포함한 **VFX PlayMode Test Runner 24개 전체 통과**를 확인했습니다.
7. **완료 — Basic Usage Scene Sample 보강** (2026-08-26)
   - 런타임에 Sphere를 생성하던 구형 `VFXPoolBasicUsageSample`을 실제 one-shot ParticleSystem
     Prefab 기반 `VFXBasicUsageSample`로 교체했습니다.
   - 같은 Scene의 왼쪽은 코드 기반 `VFXConfiguration.Pool(...)`, 오른쪽은
     `VFXDefinition` + `UnityGameObjectPoolDefinition` +
     `ParticleCompletionVFXLifetimeDefinition` SO 경로를 사용합니다.
   - Unity 6000.5.7f1 Play Mode에서 양쪽 Pool이 생성 3개를 반복 재사용하고 Particle Completion 후
     자동 반환하며 Console error 0건임을 확인했습니다. 기존 전체 VFX Test Runner와 신규 수명 계약
     테스트 8개도 사용자가 통과를 확인했습니다. 최신 import Sample에서 좌우 Particle 생성·완료·재사용
     동작 역시 사용자가 직접 확인했습니다.
8. **완료 — 공개 문서 최종 보강** (2026-08-27)
   - 한·영 README에 설치 버전, 주요 공개 타입, 코드/SO 설정, 생성·반환 순서, 수명 선택, 소유권,
     초기화 규칙, 오류 조건, 흔한 오용과 0.x 이전 방법을 실제 API에 맞춰 동등하게 기록했습니다.
   - Basic Usage 안내에 Import 절차와 Domain Reload 비활성화 반복 실행 결과를 기록했습니다.
   - 현재 Unity 6000.5.7f1 Sample의 `ScriptableObject Configuration` Inspector를 직접 캡처해
     `Documentation~/Images`에 보관하고 한·영 README 및 Sample 안내에서 참조했습니다.
9. **완료 — Unity 6000.6 + URP 17.6 Sample 육안 검증** (2026-09-03)
   - `SampleVFXMaterial`을 `Sprites/Default`로 이전한 뒤 `VFXBasicUsageSample` Play Mode에서 좌우
     청록색 파티클이 정상 표시되고 반복 생성·반환되는 것을 사용자가 확인했습니다.

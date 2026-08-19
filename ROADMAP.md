# VFX 로드맵

우선순위: `P0` 결함·안전성 → `P1` 핵심 구조 → `P2` API·성능 → `P3` 장기 확장

## 작업 순서

1. **완료 — P1-01 — GameObjectPooling 고수준 API 통합**
   - 자체 `VFXPool`을 제거하고 `ComponentPoolProvider<VFXInstance>` 합성 기반으로 전환했습니다.
   - prefab, prewarm, max inactive, overflow 및 scene lifetime은 GameObjectPooling Definition·Scope가 담당합니다.
   - 풀을 사용하지 않는 `InstantiatedVFXProvider` 경로도 같은 `IVFXProvider`로 제공합니다.
   - `VFXDefinition`과 `VFXConfiguration`으로 Inspector·코드 설정 경로를 동등하게 제공합니다.
   - `VFXEmitter` 외의 DI Service·Installer·프로젝트 VFX Catalog는 패키지 경계에 포함하지 않습니다.
2. **P0-01 — Provider 수명과 씬 전환 안정화**
   - Domain Reload 비활성화, Additive Scene, Scope dispose 및 종료 중 활성 VFX 상태를 검증합니다.
3. **완료(코드) — P1-02 — 재설정 계약** (2026-08-19, Unity 실측은 아직 안 됨)
   - `ParticleSystem`은 이미 `PlayParticleSystems`/`StopParticleSystems`(`EndLease` 시
     `ClearOnRelease` 설정에 따라 Clear 여부 선택)로 구현돼 있었습니다.
   - **`TrailRenderer`·`Animator`는 풀 재사용 시 무조건 리셋합니다**(Particle의 `ClearOnRelease`처럼
     선택형이 아닙니다 — 이전 스폰의 잔상 궤적·애니메이션 포즈를 그대로 재사용하는 것은 항상
     잘못된 상태라고 판단했습니다). `VFXInstance`가 GameObjectPooling의
     `Lifecycle.IPoolReleaseHandler`를 구현해 풀에 반환되기 직전(`OnReleaseToPool`)
     `GetComponentsInChildren<TrailRenderer>().Clear()`와
     `GetComponentInChildren<Animator>().Rebind()`+`Update(0f)`를 호출합니다. **재사용하지 않는
     `InstantiatedVFXProvider` 경로는 애초에 매번 새 인스턴스라 대상이 아닙니다.**
   - **사용자 컴포넌트는 VFX 전용 훅을 새로 만들지 않고 GameObjectPooling이 이미 제공하는
     `IPoolGetHandler`/`IPoolReleaseHandler`를 그대로 재사용하는 것이 계약입니다** —
     `UnityGameObjectPool`이 풀 Get/Release 시 자동으로 호출해줍니다(AGENTS.md: 동일 기능
     재구현 금지). **제약**: 이 스캔은 루트의 `GetComponents<T>()`만 보고 자식은 보지 않습니다
     (`PooledGameObjectState.Initialize`). `VFXInstance` 자신은 프리팹 루트에 있어야 하므로
     (`VFXConfiguration`의 프리팹 검증 조건) 문제 없지만, 리셋이 필요한 사용자 컴포넌트는
     **VFX 프리팹의 루트**에 둬야 자동 호출됩니다 — 자식에 두면 훅이 호출되지 않는다는 점을
     README/Documentation~에 기록해야 합니다(아직 안 함, 최종 문서화 단계 대상).
   - 회귀 테스트 `PooledRelease_ClearsTrailRenderer` 추가(`TrailRenderer.SetPositions`로 궤적을
     주입한 뒤 반환 시 `positionCount == 0`을 확인). `Animator.Rebind()`는 유효한
     `RuntimeAnimatorController` 애셋이 있어야 의미 있게 검증되므로 PlayMode 테스트로 만들지
     않았습니다 — 사용자가 Sample/Scene에서 눈으로 확인하는 항목으로 남깁니다.
4. **P2-01 — 자동 반환 신뢰성**
   - loop, sub-emitter, timeScale, 비활성화 및 수동 Despawn 시나리오를 검증합니다.
   - Manual·Timed(Scaled/Unscaled)·Particle Completion 기본 Handler와 generation Handle은 구현했습니다.
   - **완료 — Timed `Duration 0` 경합 수정.** `StartCoroutine`이 첫 `yield`까지 동기 실행되는
     Unity 특성 때문에 즉시 만료되는 수명이 `BeginLease` 반환 전에 만료를 확정해 `Spawn()`이
     이미 무효화된 `VFXHandle`을 반환할 수 있었습니다(`ParticleCompletionVFXLifetimeHandler`는
     `_observedFirstFrame`으로 이미 방어하고 있었지만 `TimedVFXLifetimeHandler`는 방어가 없었음).
     `VFXInstance.RunLifetime()`의 첫 Tick을 프레임 1회 미루는 방식으로 프레임워크 레벨에서
     고쳤고(개별 `IVFXLifetimeHandler` 구현이 각자 방어할 필요 없음), 회귀 테스트
     `PooledTimedLifetime_ZeroDuration_HandleIsValidImmediatelyAfterSpawn`을 추가했습니다.
     Unity Test Runner 실행으로 아직 실측 확인은 안 됨(라이선싱 차단, 아래 참고).
5. **P3-01 — Visual Effect Graph 지원**
   - VFX Graph가 설치된 경우에만 동작하는 선택 어댑터를 검토합니다.
6. **완료(코드) — P3-02 — 수명 Tick 루프를 `Coroutine`에서 `Awaitable`로 전환** (사용자 지시,
   2026-08-19, Unity 실측은 아직 안 됨)
   - Runtime의 유일한 코루틴이던 `VFXInstance.RunLifetime()`을
     `async void RunLifetimeAsync(CancellationToken)` + `Awaitable.NextFrameAsync`로 교체했습니다.
     Sample의 주기적 스폰 루프(`PooledVFXSample.Start()`)와 Tests의 `UnityTest`는 전환 대상이
     아니었습니다 — 전자는 임의 예제 코드, 후자는 Unity Test Framework 자체 요구사항입니다.
   - `Coroutine` 참조 대신 `MonoBehaviour.destroyCancellationToken`에 연결한
     `CancellationTokenSource`로 취소를 표현합니다. `EndLease`가 `Cancel()`+`Dispose()`로
     정지시키고, `OnDestroy`(신규 추가)가 `EndLease`를 거치지 않고 GameObject가 파괴되는
     경로(예: Scope가 우리 Provider보다 먼저 자신의 풀 인스턴스를 직접 파괴하는 경우)까지
     안전망으로 커버합니다 — 코루틴 시절엔 이런 `OnDestroy` 정리가 없어도 Unity가 자동으로
     코루틴을 멈춰줬지만, 이제는 명시적 취소가 필요해 추가했습니다.
   - "첫 프레임 지연" 방어(구 `yield return null`)는 `await Awaitable.NextFrameAsync(token)`으로
     그대로 유지했습니다 — `Awaitable`도 첫 `await`까지 동기 실행되는 동일한 특성이 있습니다.
   - **완료 — 외부 비활성화(`SetActive(false)`) 오용에 대한 최소 방어.** 리스 중인
     `VFXInstance`를 외부에서 `SetActive(false)`로 끄는 것은 **애초에 지원 대상 사용법이
     아닙니다**(사용자 확정, 2026-08-19) — "숨긴 동안 일시정지"류 기능을 제공하려는 목적이
     아닙니다. 다만 코루틴 시절엔 그렇게 오용해도 Unity가 자동으로 코루틴을 멈춰 최소한
     조용히 멎기라도 했던 반면, `Awaitable` 기반 루프는 GameObject 활성 상태와 무관하게 계속
     진행되어 `ParticleCompletionVFXLifetimeHandler`가 "시뮬레이션이 멈춰서 살아있는 파티클이
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
   - Unity Test Runner로 아직 실측하지 않았습니다(라이선싱 차단, `handoffs/current.md` 참고).

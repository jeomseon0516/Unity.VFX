# VFX 기본 예제

`VFXPoolBasicUsageSample` Scene을 Play합니다.

- `Reuse Mode: Pool`은 `GameObjectPoolScope`와 `PooledVFXProvider`를 사용합니다.
- `Reuse Mode: Instantiate`는 `InstantiatedVFXProvider`로 생성·파괴하며 런타임 풀을 만들지 않습니다.
- 두 모드 모두 구가 1.5초 후 자동 종료됩니다. Play 전 Inspector에서 모드를 바꿔 비교합니다.
- Sample은 ScriptableObject 없이 `VFXConfiguration` 런타임 경로로 `VFXEmitter`를
  초기화합니다. 생성 결과는 Unity 오브젝트가 아닌 `VFXHandle`입니다.

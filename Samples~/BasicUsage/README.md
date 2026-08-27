# VFX 기본 예제

Package Manager에서 **Basic Usage**를 Import하고 `VFXBasicUsageSample` Scene을 연 뒤 Play합니다.

`VFXBasicUsageSample` Scene을 Play하면 동일한 ParticleSystem Prefab을 구성하는 두 공개 경로를
나란히 확인할 수 있습니다.

- 왼쪽은 `VFXBasicUsageSample.Awake()`에서 `VFXConfiguration.Pool(...)`을 직접 생성하는 순수
  런타임 구성입니다.
- 오른쪽은 Scene의 `VFXEmitter`에 `SampleVFXDefinition`과 `GameObjectPoolScope`를 연결한
  ScriptableObject 구성입니다. `VFXEmitter.Awake()`가 Definition을 불변 런타임 Configuration으로
  변환합니다.
- 두 경로 모두 Particle Completion 계약을 사용하므로 모든 Particle이 끝난 뒤 자동으로 Pool에
  반환됩니다.
- Game View 왼쪽과 오른쪽의 spawn 횟수가 계속 증가하고, Hierarchy의 활성 VFX가 완료 후 사라지며
  다시 재사용되는지 확인합니다.
- Domain Reload를 끈 상태의 반복 Play/Stop에서도 두 경로가 다시 초기화되고 오류가 발생하지 않는 것을
  Unity 6000.5.7f1에서 확인했습니다.

## ScriptableObject 구성 자산

- `SampleVFXDefinition.asset`: 재사용 방식, 재생 옵션, Pool/Lifetime Definition을 묶습니다.
- `SampleVFXPoolDefinition.asset`: Prefab, prewarm 수, inactive 한도와 Scope 수명을 정의합니다.
- `SampleParticleCompletionLifetime.asset`: 자식 Particle까지 완료 여부를 확인합니다.
- `SampleVFX.prefab`: 루트 `VFXInstance`와 one-shot ParticleSystem을 포함합니다.

![ScriptableObject Configuration 오브젝트의 VFXEmitter 연결 상태](../../Documentation~/Images/VFXBasicUsage-ScriptableObject-Setup.jpg)

런타임 사용자는 내부 `GameObject`를 소유하지 않습니다. `VFXEmitter.Spawn()`이 반환하는
`VFXHandle`의 `TryRelease`, `TryPause`, `TryResume`, `TryStopEmission` 및 pose/scale/parent 조작 API만
사용합니다.

`VFXHandle.TryStopEmission()`은 방출만 멈추고 즉시 반환하지 않습니다. 바로 반환하려면
`TryRelease()`를 사용합니다. 내부 VFX GameObject를 직접 끄거나 파괴하지 않습니다.

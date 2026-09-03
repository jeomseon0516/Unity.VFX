# Jeomseon Unity VFX

ParticleSystem 효과를 생성하고, 재생이 끝난 효과를 자동으로 정리하거나 풀에 반환하는 Unity 패키지입니다.
코드 설정과 ScriptableObject를 이용한 Inspector 설정을 모두 제공합니다.

## 요구 사항

- Unity 6000.6.0f1 이상
- `com.jeomseon.unity.game-object-pooling` 0.4.0 이상 (`package.json`이 의존성으로 선언)

## OpenUPM으로 설치

프로젝트의 `Packages/manifest.json`에 OpenUPM scoped registry를 한 번 등록합니다.

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.jeomseon"
      ]
    }
  ],
  "dependencies": {
    "com.jeomseon.unity.vfx": "0.4.0"
  }
}
```

## Git URL로 설치

Unity Package Manager의 `Install package from git URL`에 다음 주소를 사용합니다.

```text
https://github.com/jeomseon0516/Unity.VFX.git#v0.4.0
```

## 주요 구성 요소

| 이름 | 역할 |
| --- | --- |
| `VFXEmitter` | 설정을 초기화하고 효과를 생성합니다. |
| `VFXConfiguration` | 코드에서 프리팹, 풀, 재생 방식, 자동 반환 시점을 묶습니다. |
| `VFXDefinition` | 같은 설정을 Inspector에서 편집하는 ScriptableObject입니다. |
| `VFXInstance` | 프리팹 최상단에 붙는 필수 실행 부품입니다. 직접 조작하지 않습니다. |
| `VFXHandle` | 생성한 효과를 반환·일시정지·재개·이동하는 안전한 손잡이입니다. |
| `IVFXLifetimeConfiguration` | 효과를 언제 반환할지 정하는 생성별 상태를 만듭니다. |

## 프리팹 준비

1. 효과 프리팹의 최상단 GameObject에 `VFXInstance`를 붙입니다.
2. 그 아래에 `ParticleSystem`, `TrailRenderer`, `Animator` 등 효과 부품을 둡니다.
3. 풀에서 다시 꺼낼 때 직접 초기화해야 하는 사용자 부품은 프리팹 **최상단**에 두고
   GameObjectPooling의 `IPoolGetHandler` 또는 `IPoolReleaseHandler`를 구현합니다. 풀은 자식의 사용자
   부품까지 찾지 않습니다.

최상단에 `VFXInstance`가 없으면 설정 생성 또는 첫 생성 시 예외가 발생합니다.

## 코드에서 풀 사용

```csharp
using Jeomseon.Unity.GameObjectPooling.Configurations;
using Jeomseon.Unity.GameObjectPooling.Registrations;
using Jeomseon.Unity.GameObjectPooling.Scopes;
using Jeomseon.Unity.VFX;
using UnityEngine;

public sealed class HitVFXSpawner : MonoBehaviour
{
    [SerializeField] private VFXEmitter emitter;
    [SerializeField] private GameObjectPoolScope poolScope;
    [SerializeField] private GameObject prefab;

    private void Awake()
    {
        var pool = new UnityGameObjectPoolConfiguration(prefab, prewarmCount: 4, maxInactiveCount: 16);
        var registration = new GameObjectPoolRegistration(
            pool,
            PoolLifetimeConfiguration.Scope,
            "Hit VFX");
        var configuration = VFXConfiguration.Pool(
            registration,
            ParticleCompletionVFXLifetimeConfiguration.Default);

        emitter.Initialize(configuration, poolScope);
    }

    public VFXHandle Spawn(Vector3 position) =>
        emitter.Spawn(position, Quaternion.identity);
}
```

`PoolLifetimeConfiguration.Scope` 풀은 지정한 `GameObjectPoolScope`와 함께 정리됩니다. 풀 없이 매번
새로 만들려면 `VFXConfiguration.Instantiate(prefab, lifetime)`를 사용합니다. 이 방식은 반환할 때
인스턴스를 파괴합니다.

## ScriptableObject로 설정

1. **Assets > Create > Jeomseon > VFX > VFX Definition**으로 `VFXDefinition`을 만듭니다.
2. `Reuse Mode`를 `Pool`로 정하고 `UnityGameObjectPoolDefinition`과 수명 Definition을 연결합니다.
3. Scene의 `VFXEmitter`에 이 Definition과 `GameObjectPoolScope`를 연결합니다.
4. 다른 스크립트에서 `VFXEmitter.Spawn(...)`을 호출합니다. `Awake()`에서 Definition이 실행 설정으로
   변환됩니다.

풀을 쓰지 않으려면 `Reuse Mode`를 `Instantiate`로 정하고 `Prefab`을 연결합니다. 수명 Definition은
**Jeomseon > VFX > Lifetime** 아래의 `Manual`, `Timed`, `Particle Completion` 메뉴에서 만듭니다.

![VFXEmitter에 ScriptableObject Definition과 Pool Scope를 연결한 Basic Usage Scene](Documentation~/Images/VFXBasicUsage-ScriptableObject-Setup.jpg)

위 화면의 `ScriptableObject Configuration` 오브젝트처럼 `VFXEmitter`의 `Definition`과 `Pool Scope`를
함께 연결합니다.

## 반환 시점 선택

| 설정 | 동작 | 알맞은 경우 |
| --- | --- | --- |
| `ManualVFXLifetimeConfiguration` | 자동 반환하지 않습니다. | 게임 로직이 끝나는 시점을 직접 아는 지속 효과 |
| `TimedVFXLifetimeConfiguration` | 지정한 시간 뒤 반환합니다. | 입자 상태와 무관한 고정 시간 효과 |
| `ParticleCompletionVFXLifetimeConfiguration` | 포함된 입자가 모두 사라진 뒤 반환합니다. | 반복하지 않는 일반 ParticleSystem 효과 |

`Timed`는 일반 게임 시간과 `Time.timeScale`의 영향을 받지 않는 시간 중 하나를 선택하며 시간은 0
이상이어야 합니다. `Particle Completion`은 첫 프레임 이후부터 완료 여부를 확인하고 Sub Emitter도
기다립니다. 프리팹에 `ParticleSystem`이 없거나 하나라도 계속 반복 재생되면 자동 반환되지 않습니다.
이때는 `Manual` 또는 `Timed`를 사용하거나 `VFXHandle.TryStopEmission()` 뒤 완료를 기다립니다.

사용자 정의 방식은 `IVFXLifetimeConfiguration.CreateSession(in VFXLifetimeContext)`에서 생성할 때마다
새 `IVFXLifetimeSession`을 반환해야 합니다. Session은 매 프레임 `Tick`으로 완료 여부를 알리고,
반환·Emitter 제거·Scene 종료 때 정확히 한 번 `Dispose`됩니다. `null`을 반환하면 생성이 거부됩니다.

## 생성한 효과 다루기

```csharp
VFXHandle handle = emitter.Spawn(VFXSpawnOptions.At(position, rotation));

handle.TrySetPose(nextPosition, nextRotation);
handle.TrySetScale(Vector3.one * 2f);
handle.TryPause();
handle.TryResume();
handle.TryStopEmission();
handle.TryRelease();
```

모든 `Try...` 메서드는 성공 여부를 반환합니다. 이미 자동 반환됐거나 같은 인스턴스가 새 효과로
재사용됐다면 오래된 손잡이는 `false`를 반환하고 새 효과를 건드리지 않습니다. `TryStopEmission`은
방출만 멈추며 즉시 반환하지 않습니다. 즉시 반환하려면 `TryRelease`를 호출합니다.

효과의 내부 GameObject를 직접 끄거나 파괴하지 마십시오. 부모를 비활성화하면 완료 확인도 부모가 다시
활성화될 때까지 멈춥니다. 수명은 `VFXHandle`과 `VFXEmitter`를 통해 관리합니다.

## 재생과 재사용 규칙

`VFXPlaybackConfiguration`은 자식 ParticleSystem 재생, 재생 전 입자 삭제, 반환 시 입자 삭제,
재생 속도를 정합니다. 재생 속도는 0 이상이어야 합니다.

풀에 반환할 때는 다음 상태를 정리합니다.

- `ClearOnRelease`가 켜진 경우 모든 입자를 삭제합니다.
- 모든 자식 `TrailRenderer`의 흔적을 항상 지웁니다.
- 모든 자식 `Animator`를 항상 처음 상태로 되돌립니다.
- 등록된 Sub Emitter는 부모 ParticleSystem이 재생하며 완료 확인에는 포함됩니다.

`VFXEmitter`가 만든 Provider는 Emitter가 제거될 때 활성 효과와 풀 연결을 정리합니다.
`Initialize(IVFXProvider, takeOwnership: false)`로 외부 Provider를 넣었다면 호출자가 정리할 책임이
있습니다. 한 Emitter를 두 번 초기화할 수 없습니다.

## 샘플과 검증

Package Manager에서 **Samples > Basic Usage**를 Import한 뒤 `VFXBasicUsageSample` Scene을 Play합니다.
왼쪽은 코드 설정, 오른쪽은 ScriptableObject 설정이며 둘 다 같은 프리팹을 반복해서 풀에서 꺼내고
입자가 끝나면 자동 반환합니다. 자세한 구성은 [샘플 안내](Samples~/BasicUsage/README.md)를 참고하십시오.

Unity 6000.5.7f1에서 다음을 확인했습니다.

- PlayMode 테스트 16/16 통과
- Editor Animator 초기화 테스트 1/1 통과
- 코드 설정과 ScriptableObject 설정 Sample의 생성·완료·재사용 확인
- Domain Reload를 끈 상태에서 반복 Play/Stop 확인

## 0.x에서 바뀐 점

- 자체 `VFXPool`을 제거했습니다. `VFXConfiguration.Pool`과 `GameObjectPoolScope`를 사용합니다.
- `PooledVFX` 이름을 `VFXInstance`로 바꿨습니다. 프리팹 최상단 부품을 교체해야 합니다.
- 빈 표식이던 `IVFXLifetimeConfiguration`이 Session 생성 계약을 갖습니다.
- `IVFXLifetimeHandler`와 `VFXEmitter.RegisterLifetimeHandler`를 제거했습니다. 사용자 수명 설정이 직접
  Session을 생성하도록 옮깁니다.

전체 변경 내용은 [CHANGELOG](CHANGELOG.md), 현재 안정화 상태는 [ROADMAP](ROADMAP.md)에서 확인할 수
있습니다.

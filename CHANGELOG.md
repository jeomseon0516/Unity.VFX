# 변경 기록

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

## [Unreleased]

- TODO(architecture): 자체 VFX 풀을 Jeomseon Unity GameObject Pooling 위에 구성하고 ScriptableObject 프리셋으로 프리웜·최대치를 관리하도록 개편합니다.
- 정적 이벤트와 전역 인스턴스의 Domain Reload 비활성화 호환성을 검토합니다.

## [0.1.0] - 2026-07-29

- JeomseonScriptPack의 관련 모듈을 독립 UPM 패키지로 분리했습니다.


## [0.1.3] - 2026-08-05

- Unity 6000.5.7f1을 최소 지원 버전으로 상향했습니다.

# VFX 로드맵

우선순위: `P0` 결함·안전성 → `P1` 핵심 구조 → `P2` API·성능 → `P3` 장기 확장

## 작업 순서

1. **P0-01 — 풀 수명과 씬 전환 안정화**
   - 파괴된 인스턴스, Domain Reload 비활성화, Additive Scene 및 종료 상태를 테스트합니다.
2. **P0-02 — 최대 용량 재사용 정책 검증**
   - 사용 중 인스턴스를 강제 재사용하지 않도록 활성·대기 상태를 구분합니다.
3. **P1-01 — Pooling 패키지 기반으로 통합**
   - ScriptableObject 프리셋에서 prefab, prewarm, max, overflow 및 scene lifetime을 설정합니다.
4. **P1-02 — 재설정 계약**
   - ParticleSystem, TrailRenderer, Animator와 사용자 컴포넌트의 reset 전략을 명시합니다.
5. **P2-01 — 자동 반환 신뢰성**
   - loop, sub-emitter, timeScale, 비활성화 및 수동 Despawn 시나리오를 검증합니다.
6. **P3-01 — Visual Effect Graph 지원**
   - VFX Graph가 설치된 경우에만 동작하는 선택 어댑터를 검토합니다.

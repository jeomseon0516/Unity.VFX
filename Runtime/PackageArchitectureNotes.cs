namespace Jeomseon.VFX
{
    internal static class PackageArchitectureNotes
    {
        // TODO(architecture): 자체 VFX 풀을 Jeomseon Unity Pooling 위에 구성하고 ScriptableObject 프리셋으로 프리웜·최대치를 관리하도록 개편합니다.
        // TODO(lifecycle): 정적 이벤트와 전역 인스턴스는 Domain Reload 비활성화 환경에서 초기화 상태가 남는지 검증합니다.
    }
}

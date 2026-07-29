using UnityEngine;

namespace Jeomseon.VFX
{
    [DisallowMultipleComponent]
    public sealed class PooledVFX : MonoBehaviour
    {
        [Tooltip("ParticleSystem이 없거나 수동형 오브젝트일 때 강제 수명(초). 0이면 비활성.")]
        public float fallbackLifetime = 1.5f;

        private bool _recycling;

        public void BeginAutoRecycle()
        {
            if (!_recycling) StartCoroutine(AutoRecycleRoutine());
        }

        private System.Collections.IEnumerator AutoRecycleRoutine()
        {
            _recycling = true;

            var pss = GetComponentsInChildren<ParticleSystem>(includeInactive: false);
            if (pss != null && pss.Length > 0)
            {
                // 모든 파티클이 사라질 때까지 대기
                bool anyAlive = true;
                while (anyAlive)
                {
                    anyAlive = false;
                    foreach (var ps in pss)
                    {
                        if (ps && ps.IsAlive(true)) { anyAlive = true; break; }
                    }
                    if (!anyAlive) break;
                    yield return null;
                }
            }
            else if (fallbackLifetime > 0f)
            {
                yield return new WaitForSeconds(fallbackLifetime);
            }

            VFXPool.Instance.Despawn(gameObject);
            _recycling = false;
        }
    }
}

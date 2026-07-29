using Jeomseon.Singleton;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.VFX
{
    public sealed class VFXPool : Singleton<VFXPool>
    {
        [Header("Optional")]
        [SerializeField] private int defaultPrewarm = 8;
        [SerializeField] private int defaultMax = 64;
        [SerializeField] private bool dontDestroyOnLoad = true;

        // prefab -> available queue
        private readonly Dictionary<GameObject, Queue<GameObject>> _pool = new();
        // instance -> prefab (역추적)
        private readonly Dictionary<GameObject, GameObject> _origin = new();
        // prefab -> caps
        private readonly Dictionary<GameObject, (int prewarm, int max)> _caps = new();

        protected override void Init() {}

        /// <summary> 프리팹별 프리웜/최대치 등록(선택) </summary>
        public void Configure(GameObject prefab, int prewarm, int max)
        {
            if (!prefab) return;
            _caps[prefab] = (Mathf.Max(0, prewarm), Mathf.Max(1, max));
            PrewarmIfNeeded(prefab);
        }

        /// <summary> 위치/회전/부모 지정 스폰. ParticleSystem이면 Play, 일반 오브젝트면 SetActive(true). </summary>
        public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
        {
            if (!prefab)
            {
                Debug.LogWarning("[VfxPool] Spawn called with null prefab.");
                return null;
            }

            PrewarmIfNeeded(prefab);

            GameObject go;
            if (_pool.TryGetValue(prefab, out var q) && q.Count > 0)
            {
                go = q.Dequeue();
                if (!go) // 파괴된 경우 방어
                    go = InternalCreate(prefab);
            }
            else
            {
                if (CountTotal(prefab) < GetMax(prefab))
                    go = InternalCreate(prefab);
                else
                    go = ForceReuse(prefab); // 최대치 초과 시 가장 오래된 것을 재사용(선택 로직)
            }

            SetupTransform(go, pos, rot, parent);
            Activate(go);

            // 자동 회수 지원 컴포넌트가 있으면 바로 시작
            if (!go.TryGetComponent<PooledVFX>(out var pooled))
                pooled = go.AddComponent<PooledVFX>();
            pooled.BeginAutoRecycle();

            return go;
        }

        /// <summary> Transform 기준 스폰(오버로드) </summary>
        public GameObject Spawn(GameObject prefab, Transform t)
            => Spawn(prefab, t.position, t.rotation, t.parent);

        /// <summary> 즉시 회수(사용자가 핸들 보관 후 조기 반환할 때) </summary>
        public void Despawn(GameObject instance)
        {
            if (!instance) return;
            if (!_origin.TryGetValue(instance, out var prefab) || !prefab)
            {
                // 풀에서 만들지 않은 외부 오브젝트일 수도 있음
                instance.SetActive(false);
                return;
            }

            // 트랜스폼 초기화(선택)
            instance.transform.SetParent(transform, false);

            // 상태 리셋
            ResetInstance(instance);

            if (!_pool.TryGetValue(prefab, out var q))
            {
                q = new Queue<GameObject>();
                _pool[prefab] = q;
            }
            q.Enqueue(instance);
        }

        // ---------- 내부 유틸 ----------

        private void PrewarmIfNeeded(GameObject prefab)
        {
            var (prewarm, _) = GetCaps(prefab);
            int current = CountTotal(prefab);
            for (int i = current; i < prewarm; i++)
            {
                var go = InternalCreate(prefab);
                ResetInstance(go);
                if (!_pool.TryGetValue(prefab, out var q))
                {
                    q = new Queue<GameObject>();
                    _pool[prefab] = q;
                }
                q.Enqueue(go);
            }
        }

        private (int prewarm, int max) GetCaps(GameObject prefab)
        {
            if (_caps.TryGetValue(prefab, out var c)) return c;
            return (defaultPrewarm, defaultMax);
        }
        private int GetMax(GameObject prefab) => GetCaps(prefab).max;

        private int CountTotal(GameObject prefab)
        {
            int count = 0;
            foreach (var kv in _origin)
                if (kv.Value == prefab) count++;
            return count;
        }

        private GameObject ForceReuse(GameObject prefab)
        {
            // 가장 먼저 풀에 들어온 것부터 재사용
            if (_pool.TryGetValue(prefab, out var q) && q.Count > 0)
            {
                var go = q.Dequeue();
                if (!go) return InternalCreate(prefab);
                return go;
            }
            // 큐가 없다면 새로 만들수밖에 없음(로그)
            Debug.LogWarning($"[VfxPool] Max reached but no idle instance for '{prefab.name}'. Creating new.");
            return InternalCreate(prefab);
        }

        private GameObject InternalCreate(GameObject prefab)
        {
            var go = Instantiate(prefab, transform);
            _origin[go] = prefab;
            return go;
        }

        private static void SetupTransform(GameObject go, Vector3 pos, Quaternion rot, Transform parent)
        {
            var tr = go.transform;
            tr.SetParent(parent, worldPositionStays: true);
            tr.SetPositionAndRotation(pos, rot);
            tr.localScale = Vector3.one;
        }

        private static void Activate(GameObject go)
        {
            // ParticleSystem 있으면 모두 Play
            var pss = go.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            if (pss != null && pss.Length > 0)
            {
                go.SetActive(true);
                foreach (var ps in pss) ps.Clear(true);
                foreach (var ps in pss) ps.Play(true);
            }
            else
            {
                go.SetActive(true); // 일반 오브젝트
            }
        }

        private static void ResetInstance(GameObject go)
        {
            // 파티클 멈춤/리셋
            var pss = go.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            if (pss != null && pss.Length > 0)
            {
                foreach (var ps in pss) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            go.SetActive(false);
        }
    }
}

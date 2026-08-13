using Jeomseon.Unity.VFX;
using UnityEngine;

namespace Jeomseon.Samples.VFX
{
    [RequireComponent(typeof(PooledVFX))]
    public sealed class PooledVFXSample : MonoBehaviour
    {
        [ContextMenu("자동 회수 시작")]
        private void BeginRecycle()
        {
            GetComponent<PooledVFX>().BeginAutoRecycle();
        }
    }
}

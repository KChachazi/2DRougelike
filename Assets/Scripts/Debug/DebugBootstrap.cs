using UnityEngine;

namespace Game.Debug
{
    /// <summary>
    /// 场景中的调试系统启动点。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class DebugBootstrap : MonoBehaviour
    {
        [SerializeField] private DebugSettings settings;
        private void Awake()
        {
            GameDebug.Initialize(settings);
        }
    }
}
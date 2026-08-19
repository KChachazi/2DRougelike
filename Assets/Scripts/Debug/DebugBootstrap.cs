using UnityEngine;

namespace Game.Debug
{
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
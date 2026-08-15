using System.Collections;
using UnityEngine;

namespace Game.Core
{
    /// <summary>使用非缩放时间实现短暂战斗停顿，并安全处理重复触发。</summary>
    public class HitStop : MonoBehaviour
    {
        private static HitStop instance;
        private Coroutine running;

        private void Awake() => instance = this;
        private void OnDestroy()
        {
            if (instance == this) instance = null;
            Time.timeScale = 1f;
        }
        /// <summary>
        /// 执行短暂战斗停顿。供攻击命中等场景调用。
        /// </summary>
        public static void Do(float duration)
        {
            if (instance == null || duration <= 0f) return ;
            if (instance.running != null)
                instance.StopCoroutine(instance.running);
            instance.running = instance.StartCoroutine(instance.Routine(duration));
        }
        private IEnumerator Routine(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            running = null;
        }
    }
}
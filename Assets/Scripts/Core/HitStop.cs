using System.Collections;
using UnityEngine;

namespace Game.Core
{
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
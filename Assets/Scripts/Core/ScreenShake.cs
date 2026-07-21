using UnityEngine;

namespace Game.Core
{
    public class ScreenShake : MonoBehaviour
    {
        private Vector3 originalLocalPos;
        private float timer, duration, intensity;

        private void Awake() => originalLocalPos = transform.localPosition;
        private void OnEnable() => EventBus.Subscribe<ScreenShakeEvent>(OnScreenShake);
        private void OnDisable()
        {
            EventBus.Unsubscribe<ScreenShakeEvent>(OnScreenShake);
            transform.localPosition = originalLocalPos;
            timer = 0f;
            intensity = 0f;
        }
        private void OnScreenShake(ScreenShakeEvent e)
        {
            intensity = Mathf.Max(intensity, e.Intensity);
            duration = Mathf.Max(duration, e.Duration);
            timer = duration;
        }

        private void Update()
        {
            if (timer <= 0f) return ;
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f)
            {
                transform.localPosition = originalLocalPos;
                intensity = 0f;
                return ;
            }
            float damper = timer / duration;
            transform.localPosition = originalLocalPos + (Vector3)(Random.insideUnitCircle * intensity * damper);
        }
    }
}
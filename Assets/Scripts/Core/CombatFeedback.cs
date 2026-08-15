using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 通过 EventBus 将敌人受伤/死亡等战斗语义事件翻译为效果表现。
    /// 伤害来源无需知道具体反馈参数。
    /// </summary>
    public class CombatFeedback : MonoBehaviour
    {
        [Header("命中")]
        [Tooltip("命中停顿时长")]
        [SerializeField] private float hitStopDuration = 0.04f;

        [Header("击杀")]
        [SerializeField] private float deathShakeIntensity = 0.3f;
        [SerializeField] private float deathShakeDuration = 0.25f;

        [Header("特效")]
        [SerializeField] private ObjectPool hitSparkPool;
        [SerializeField] private ObjectPool deathBurstPool;

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        }
        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void OnEnemyDamaged(EnemyDamagedEvent e)
        {
            HitStop.Do(hitStopDuration);
            if (hitSparkPool != null) hitSparkPool.Get(e.Position, Quaternion.identity);
        }
        private void OnEnemyDied(EnemyDiedEvent e)
        {
            EventBus.Publish(new ScreenShakeEvent(deathShakeIntensity, deathShakeDuration));
            if (deathBurstPool != null) deathBurstPool.Get(e.Position, Quaternion.identity);
        }
    }
}
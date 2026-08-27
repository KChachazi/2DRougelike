using Game.Entities;
using UnityEngine;

namespace Game.Pickups
{
    /// <summary>为玩家恢复生命；满生命时不会被消耗。</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class HealthPickup : MonoBehaviour
    {
        [SerializeField, Min(1)] private int amount = 30;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (!other.TryGetComponent(out Health health)) return;
            if (health.isDead || health.Current >= health.Max) return;
            health.Heal(amount);
            Destroy(gameObject);
        }
    }
}
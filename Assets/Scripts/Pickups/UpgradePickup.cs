using Game.Rewards;
using UnityEngine;

namespace Game.Pickups
{
    /// <summary>
    /// 被玩家拾取后打开一次局内强化选择。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class UpgradePickup : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return ;
            if (!other.TryGetComponent(out RunUpgradeManager upgradeManager)) return ;
            if (!upgradeManager.TryPresentChoices()) return ;
            Destroy(gameObject);
        }
    }
}
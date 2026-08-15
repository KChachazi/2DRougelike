using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 玩家接触后尝试为当前武器补弹并立即销毁；
    /// 无限弹药武器不会获得弹药。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class AmmoPickup : MonoBehaviour
    {
        [SerializeField] private int amount = 15;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return ;
            if (other.TryGetComponent(out WeaponController weapon))
                weapon.AddAmmo(amount);
            Destroy(gameObject);
        }
    }
}
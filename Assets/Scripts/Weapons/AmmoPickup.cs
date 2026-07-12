using UnityEngine;

namespace Game.Weapons
{
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
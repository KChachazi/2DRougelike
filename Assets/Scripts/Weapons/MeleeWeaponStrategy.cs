using Game.Entities;
using UnityEngine;

namespace Game.Weapons
{
    public class MeleeWeaponStrategy : IWeaponStrategy
    {
        public void Fire(WeaponController controller, WeaponData data)
        {
            Vector2 origin = (Vector2)controller.transform.position
                           + (Vector2)controller.transform.right * data.range;
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, data.range);
            foreach (Collider2D hit in hits)
            {
                if (!hit.CompareTag("Enemy")) continue;
                if (hit.TryGetComponent(out Health health))
                    health.TakeDamage(data.damage);
            }
        }
    }
}
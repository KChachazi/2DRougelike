using Game.Entities;
using UnityEngine;

namespace Game.Weapons
{
    public class MeleeWeaponStrategy : IWeaponStrategy
    {
        private const int MaxHits = 16;
        private readonly Collider2D[] hitBuffer = new Collider2D[MaxHits];
        private readonly ContactFilter2D filter = ContactFilter2D.noFilter;
        public void Fire(WeaponController controller, WeaponData data)
        {
            Vector2 origin = (Vector2)controller.transform.position
                           + (Vector2)controller.transform.right * data.range;
            int count = Physics2D.OverlapCircle(origin, data.range, filter, hitBuffer);
            for (int i = 0; i < count; i ++)
            {
                Collider2D hit = hitBuffer[i];
                if (!hit.CompareTag("Enemy")) continue;
                if (hit.TryGetComponent(out Health health))
                    health.TakeDamage(data.damage);
            }
        }
    }
}
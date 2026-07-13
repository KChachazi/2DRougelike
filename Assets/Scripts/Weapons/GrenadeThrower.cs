using Game.Core;
using UnityEngine;

namespace Game.Weapons
{
    public class GrenadeThrower : MonoBehaviour
    {
        [SerializeField] private ObjectPool grenadePool;
        [Tooltip("投掷起点，若留空则从玩家位置出发")]
        [SerializeField] private Transform throwPoint;
        [SerializeField] private float cooldown = 3f;

        private float cooldownTimer;
        
        public bool CanThrow => cooldownTimer <= 0f && grenadePool != null;
        public float Cooldown => cooldown;

        private void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        }

        public void Throw()
        {
            Transform origin = throwPoint != null ? throwPoint : transform;
            grenadePool.Get(origin.position, origin.rotation);
            cooldownTimer = cooldown;
            EventBus.Publish(new SkillCooldownStartedEvent(SkillId.Grenade, cooldown));
        }
    }
}
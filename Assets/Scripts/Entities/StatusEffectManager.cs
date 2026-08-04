using Game.Core;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Entities
{
    public class StatusEffectManager : MonoBehaviour
    {
        [Header("击退")]
        [Tooltip("击退初速度")]
        [SerializeField] private float knockbackSpeed = 8f;

        private readonly List<ActiveStatus> activeList = new List<ActiveStatus>();
        private Health health;
        private Rigidbody2D rb;

        public float SpeedMultiplier { get; private set; } = 1f;
        public float DamageMultiplier { get; private set; } = 1f;
        public int ActiveCount => activeList.Count;

        private void Awake()
        {
            health = GetComponent<Health>();
            rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (activeList.Count == 0) return ;
            float deltaTime = Time.deltaTime;
            bool needRecalc = false;

            for (int i = activeList.Count - 1; i >= 0; i --)
            {
                ActiveStatus active = activeList[i];
                active.RemainingDuration -= deltaTime;
                if (active.RemainingDuration <= 0f)
                {
                    activeList.RemoveAt(i);
                    needRecalc = true;
                    continue;
                }

                if (active.Config.Type == StatusType.Burn)
                {
                    active.TickTimer -= deltaTime;
                    if (active.TickTimer <= 0f)
                    {
                        active.TickTimer = active.Config.TickInterval;
                        if (health != null)
                        {
                            health.TakeDamage(active.Config.DamagePerTick);
                        }
                    }
                }
            }
            if (needRecalc) RecalcModifiers();
        }

        // 公开接口
        public void ApplyEffects(DamageInfo info)
        {
            if (info.StatusEffects == null || info.StatusEffects.Length == 0) return ;
            foreach (var config in info.StatusEffects)
            {
                ApplyEffect(config);
            }
        }
        public void ApplyEffect(StatusEffectConfig config)
        {
            if (config.Duration <= 0f) return ;
            for (int i = 0; i < activeList.Count; i ++)
            {
                if (activeList[i].Config.Type == config.Type)
                {
                    ActiveStatus existing = activeList[i];
                    if (config.Duration > existing.RemainingDuration)
                        existing.RemainingDuration = config.Duration;
                    if (config.Type == StatusType.Burn && config.TickInterval < existing.Config.TickInterval)
                    {
                        existing.Config = config;
                        existing.TickTimer = Mathf.Min(existing.TickTimer, existing.Config.TickInterval);
                    }
                    return ;
                }
            }
            activeList.Add(new ActiveStatus
            {
                Config = config,
                RemainingDuration = config.Duration,
                TickTimer = config.TickInterval,
            });
            RecalcModifiers();
        }
        public void ClearAll()
        {
            activeList.Clear();
            RecalcModifiers();
        }

        // 内部接口
        private void RecalcModifiers()
        {
            float minSpeed = 1f;
            float maxDamage = 1f;
            for (int i = 0; i < activeList.Count; i ++)
            {
                var config = activeList[i].Config;
                switch (config.Type)
                {
                    case StatusType.Freeze:
                        minSpeed = minSpeed > (1f - config.SlowPercent) ? (1f - config.SlowPercent) : minSpeed;
                        break;
                    case StatusType.Burn:
                        break;
                    case StatusType.Vulnerable:
                        maxDamage = maxDamage < config.DamageMultiplier ? config.DamageMultiplier : maxDamage;
                        break;
                    default:
                        Debug.Log($"[StatusEffectManager]Unexpected StatusType{config.Type}");
                        break;
                }
            }
            SpeedMultiplier = minSpeed;
            DamageMultiplier = maxDamage;
        }

        private class ActiveStatus
        {
            public StatusEffectConfig Config;
            public float RemainingDuration;
            public float TickTimer; // for Burn
        }
    }
}
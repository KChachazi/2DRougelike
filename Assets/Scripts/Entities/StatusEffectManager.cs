using Game.Core;
using Game.Debug;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Entities
{
    /// <summary>
    /// 状态异常管理器——挂载在需要接收状态异常的实体上（敌人、玩家）。
    /// </summary>
    //
    // 职责：
    // 1. 接收 DamageInfo 中携带的 StatusEffectConfig 列表，激活对应效果；
    // 2. 每帧更新所有激活效果，包括 DoT 扣血和效果过期；
    // 3. 向 PlayerController、EnemyController 和 Health 暴露组合后的倍率。
    // 击退（Knockback）不在此处理。
    public class StatusEffectManager : MonoBehaviour
    {
        private readonly List<ActiveStatus> activeList = new List<ActiveStatus>();
        private Health health;

        /// <summary>
        /// 当前综合移速倍率（所有 Freeze 效果取最慢者）
        /// </summary>
        public float SpeedMultiplier { get; private set; } = 1f;

        /// <summary>
        /// 当前综合受伤倍率（所有 Vulnerable 效果取最高者）
        /// </summary>
        public float DamageMultiplier { get; private set; } = 1f;

        /// <summary>只读：当前激活的效果数量（调试用）</summary>
        public int ActiveCount => activeList.Count;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void Update()
        {
            if (activeList.Count == 0) return ;
            float deltaTime = Time.deltaTime;
            bool needRecalc = false;

            // 倒序遍历，删除过期效果
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

        // ======================== 公开接口 ========================
        /// <summary>
        /// 对实体施加 DamageInfo 中携带的所有状态异常。
        /// 供命中逻辑调用。
        /// </summary>
        public void ApplyEffects(DamageInfo info)
        {
            if (info.StatusEffects == null || info.StatusEffects.Length == 0) return ;
            foreach (var config in info.StatusEffects)
            {
                ApplyEffect(config);
            }
        }
        /// <summary>
        /// 施加单条状态异常。供命中逻辑调用。
        /// </summary>
        public void ApplyEffect(StatusEffectConfig config)
        {
            if (!IsValidConfig(config)) return ;
            // 查找是否已有同类型效果
            for (int i = 0; i < activeList.Count; i ++)
            {
                ActiveStatus existing = activeList[i];
                if (existing.Config.Type != config.Type)
                    continue;
                if (TryMergeEffect(existing, config))
                    return ;
            }
            // 添加新效果
            activeList.Add(new ActiveStatus
            {
                Config = config,
                RemainingDuration = config.Duration,
                TickTimer = config.TickInterval,
            });
            RecalcModifiers();
        }
        /// <summary>
        /// 立即清除所有状态异常。
        /// </summary>
        public void ClearAll()
        {
            activeList.Clear();
            RecalcModifiers();
        }
        // ======================== 内部接口 ========================
        /// <summary>
        /// 重新计算状态效果。
        /// </summary>
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
                        GameDebug.Warning(DebugCategory.Combat, $"无法计算未知状态类型：{config.Type}", this);
                        break;
                }
            }
            SpeedMultiplier = minSpeed;
            DamageMultiplier = maxDamage;
        }
        /// <summary>
        /// 尝试合并类型相同的状态。
        /// </summary>
        private bool TryMergeEffect(ActiveStatus existing, StatusEffectConfig config)
        {
            switch (config.Type)
            {
                case StatusType.Freeze:
                    if (!Mathf.Approximately(config.SlowPercent, existing.Config.SlowPercent))
                        return false;
                    ExtendDuration(existing, config.Duration);
                    return true;

                case StatusType.Vulnerable:
                    if (!Mathf.Approximately(config.DamageMultiplier, existing.Config.DamageMultiplier))
                        return false;
                    ExtendDuration(existing, config.Duration);
                    return true;

                case StatusType.Burn:
                    MergeBurn(existing, config);
                    return true;
                default:
                    return false;
            }
        }
        /// <summary>
        /// 合并燃烧效果。
        /// </summary>
        //
        // 对于燃烧效果而言，只能同时存在一个。
        // 更新规则为：
        //  1. 若单次 dot 伤害和间隔都相同，则仅更新持续时间为最大值，不重置 Tick 进度；
        //  2. 若参数不同，则比较剩余总伤；新效果更优时替换旧效果，并从新效果的完整 Tick 间隔重新计时；
        //  3. 若总伤相同，则更新为剩余持续时间更短的一方；
        //  4. 若旧效果更优或完全相同，则保持原效果。
        private void MergeBurn(ActiveStatus existing, StatusEffectConfig config)
        {
            bool sameDamage = config.DamagePerTick == existing.Config.DamagePerTick;
            bool sameInterval = Mathf.Approximately(config.TickInterval, existing.Config.TickInterval);
            // 参数完全相同：只延长持续时间，不重置 Tick 进度。
            if (sameDamage && sameInterval)
            {
                existing.RemainingDuration = Mathf.Max(existing.RemainingDuration, config.Duration);
                return;
            }
            long existingDamage = CalculateRemainingBurnDamage(existing.Config.DamagePerTick, existing.Config.TickInterval, existing.RemainingDuration, existing.TickTimer);
            long newDamage = CalculateRemainingBurnDamage(config.DamagePerTick, config.TickInterval, config.Duration, config.TickInterval);
            bool shouldReplace = newDamage > existingDamage || (newDamage == existingDamage && config.Duration < existing.RemainingDuration);
            if (shouldReplace)
            {
                existing.Config = config;
                existing.RemainingDuration = config.Duration;
                existing.TickTimer = config.TickInterval;
            }
            return;
        }
        /// <summary>
        /// 更新异常状态的持续时间为最大值
        /// </summary>
        private static void ExtendDuration(ActiveStatus existing, float duration)
        {
            existing.RemainingDuration = Mathf.Max(existing.RemainingDuration, duration);
        }
        /// <summary>
        /// 用于计算灼烧效果的剩余总伤
        /// </summary>
        private static long CalculateRemainingBurnDamage(int damagePerTick, float tickInterval, float remainingDuration, float timeUntilNextTick)
        {
            if (damagePerTick <= 0 || tickInterval <= 0f || remainingDuration <= timeUntilNextTick)
            {
                return 0L;
            }
            int remainingTicks = Mathf.CeilToInt((remainingDuration - timeUntilNextTick) / tickInterval);
            return (long)damagePerTick * remainingTicks;
        }
        /// <summary>
        /// 判断 Config 是否合法。
        /// </summary>
        private static bool IsValidConfig(StatusEffectConfig config)
        {
            if (config.Duration <= 0f)
                return false;

            switch (config.Type)
            {
                case StatusType.Burn:
                    return config.DamagePerTick > 0 && config.TickInterval > 0f;
                case StatusType.Freeze:
                    return config.SlowPercent > 0f && config.SlowPercent <= 1f;
                case StatusType.Vulnerable:
                    return true;
                default:
                    return false;
            }
        }
        // ======================== 内部类型 ========================
        /// <summary>
        /// 一条正在生效的状态异常（运行时数据，非配置）。
        /// </summary>
        private class ActiveStatus
        {
            public StatusEffectConfig Config;
            public float RemainingDuration;
            public float TickTimer; // for Burn
        }
    }
}
using System;
using Game.Entities;

namespace Game.AI
{
    /// <summary>
    /// 条件：Boss 当前是否处于指定阶段。
    /// </summary>
    public class BossPhaseCondition : ConditionNode
    {
        private EnemyController enemy;
        private readonly float lowerBound;
        private readonly float upperBound;
        private readonly bool includeUpperBound;
        /// <summary>
        /// 条件：Boss 当前是否处于指定阶段，阶段计数从 1 开始。
        /// </summary>
        public BossPhaseCondition(EnemyController enemy, EnemyBehaviour behaviour, int phaseNumber)
        {
            this.enemy = enemy;
            float[] thresholds = behaviour.phaseThresholds;
            if (phaseNumber < 1 || phaseNumber > thresholds.Length + 1)
                throw new ArgumentOutOfRangeException(nameof(phaseNumber));
            upperBound = phaseNumber == 1 ? 1f : thresholds[phaseNumber - 2];
            lowerBound = phaseNumber == thresholds.Length + 1 ? 0f : thresholds[phaseNumber - 1];
            includeUpperBound = phaseNumber == 1;
        }
        protected override bool Check()
        {
            if (enemy.health == null || enemy.health.Max <= 0) return false;
            float ratio = (float)enemy.health.Current / enemy.health.Max;
            return lowerBound <= ratio && (includeUpperBound ? ratio <= upperBound : ratio < upperBound);
        }
    }
}
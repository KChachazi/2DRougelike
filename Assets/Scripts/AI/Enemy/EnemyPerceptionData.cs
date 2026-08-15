using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 敌人的感知快照。
    /// </summary>
    public sealed class EnemyPerceptionData
    {
        public Transform Target { get; set; }
        public float DistanceToTarget { get; set; } = float.MaxValue;
        public bool IsAlerted { get; set; }
    }
}
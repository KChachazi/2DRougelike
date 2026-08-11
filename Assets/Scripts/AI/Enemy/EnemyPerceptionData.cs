using UnityEngine;

namespace Game.AI
{
    public sealed class EnemyPerceptionData
    {
        public Transform Target { get; set; }
        public float DistanceToTarget { get; set; } = float.MaxValue;
        public bool IsAlerted { get; set; }
    }
}
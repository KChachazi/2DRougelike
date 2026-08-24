using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 程序化关卡生成的设置项，包含规模、特殊房数量与结构约束等。
    /// </summary>
    [CreateAssetMenu(fileName = "LevelGenerationSettings", menuName = "Game/Level Generation Settings")]
    public sealed class LevelGenerationSettings : ScriptableObject
    {
        [Header("规模")]
        [SerializeField, Min(6)] private int minRoomCount = 9;
        [SerializeField, Min(6)] private int maxRoomCount = 12;
        [SerializeField, Min(1)] private int maxGenerationAttempts = 50;

        [Header("结构约束")]
        [SerializeField, Min(2)] private int minBossDepth = 4;
        [Tooltip("Degree >= 3 的房间至少有几个")]
        [SerializeField, Min(1)] private int minBranchRoomCount = 1;
        
        [Header("特殊房间")]
        [SerializeField, Min(0)] private int treasureRoomCount = 1;
        [SerializeField, Min(0)] private int recoveryRoomCount = 1;
        [SerializeField, Min(0)] private int eliteRoomCount = 1;
        [SerializeField, Min(1)] private int minEliteDepth = 2;

        public int MinRoomCount => minRoomCount;
        public int MaxRoomCount => Mathf.Max(minRoomCount, maxRoomCount);
        public int MaxGenerationAttempts => Mathf.Max(1, maxGenerationAttempts);
        public int MinBossDepth => Mathf.Max(1, minBossDepth);
        public int MinBranchRoomCount => Mathf.Max(0, minBranchRoomCount);
        public int TreasureRoomCount => Mathf.Max(0, treasureRoomCount);
        public int RecoveryRoomCount => Mathf.Max(0, recoveryRoomCount);
        public int EliteRoomCount => Mathf.Max(0, eliteRoomCount);
        public int MinEliteDepth => Mathf.Max(1, minEliteDepth);

        private void OnValidate()
        {
            minRoomCount = Mathf.Max(6, minRoomCount);
            maxRoomCount = Mathf.Max(minRoomCount, maxRoomCount);
            maxGenerationAttempts = Mathf.Max(1, maxGenerationAttempts);
        }
    }
}
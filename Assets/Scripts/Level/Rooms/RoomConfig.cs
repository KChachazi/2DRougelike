using Game.Core;
using UnityEngine;

namespace Game.Level
{
    /// <summary>描述房间类型，以及敌人和拾取物关于房间中心的相对位置。</summary>
    [CreateAssetMenu(fileName = "NewRoom", menuName = "Game/Room Config")]
    public class RoomConfig : ScriptableObject
    {
        public string roomName = "Room";
        public RoomType type = RoomType.Normal;

        [Header("选择规则")]
        [Tooltip("节点深度低于该值时不会选择此配置")]
        [Min(0)] public int minDepth = 0;
        [Tooltip("节点深度低于该值时不会选择此配置")]
        [Min(0)] public int maxDepth = 99;
        [Tooltip("同类型、同深度候选中的相对权重")]
        [Min(1)] public int selectionWeight = 1;

        [Header("房间内容")]
        [Tooltip("敌人出生点(相对房间中心)")]
        public EnemySpawn[] enemySpawns;
        [Tooltip("拾取物生成点(相对房间中心)")]
        public PickupSpawn[] pickupSpawns;
        [Tooltip("战斗后生成的一次性奖励")]
        public PickupSpawn[] clearRewardSpawns;

        [Tooltip("恢复房间恢复值")]
        [Min(0)] public int healAmount = 50;

        public bool SupportsDepth(int depth) => minDepth <= depth && depth <= maxDepth;
    }

    /// <summary>一项敌人预制体及其关于房间中心的相对位置。</summary>
    [System.Serializable]
    public struct EnemySpawn
    {
        public GameObject prefab;
        [Tooltip("关于房间中心的相对位置")]
        public Vector2 localPosition;
    }

    /// <summary>一项拾取物预制体及其关于房间中心的相对位置。</summary>
    [System.Serializable]
    public struct PickupSpawn
    {
        public GameObject prefab;
        [Tooltip("关于房间中心的相对位置")]
        public Vector2 localPosition;
    }
}
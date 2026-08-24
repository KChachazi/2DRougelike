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
        [Tooltip("敌人出生点(相对房间中心)")]
        public EnemySpawn[] enemySpawns;
        [Tooltip("道具生成点(相对房间中心)")]
        public PickupSpawn[] pickupSpawns;
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
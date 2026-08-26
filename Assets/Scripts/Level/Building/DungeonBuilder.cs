using System.Collections.Generic;
using Game.Core;
using Game.Debug;
using UnityEngine;

namespace Game.Level
{
    public sealed class DungeonBuildResult
    {
        public readonly Dictionary<int, Room> Rooms;
        public readonly Room StartRoom;
        public DungeonBuildResult(Dictionary<int, Room> rooms, Room startRoom)
        {
            Rooms = rooms;
            StartRoom = startRoom;
        }
    }
    /// <summary>
    /// 把 LevelGraph 建造成世界空间中的房间、门洞和实体通道。
    /// </summary>
    public sealed class DungeonBuilder : MonoBehaviour
    {
        [SerializeField] private Room roomPrefab;
        [SerializeField] private RoomCatalog roomCatalog;
        [SerializeField] private Transform dungeonRoot;
        
        [Header("网格间距")]
        [SerializeField] private float horizontalSpacing = 28f;
        [SerializeField] private float vertivalSpacing = 20f;

        [Header("通道预制体")]
        [SerializeField] private GameObject horizontalCorridorPrefab;
        [SerializeField] private GameObject verticalCorridorPrefab;
        public DungeonBuildResult Build(LevelGraph graph, int seed)
        {
            if (graph == null || roomPrefab == null || roomCatalog == null)
            {
                GameDebug.Error(DebugCategory.Level, "DungeonBuilder 缺少关卡图、Room Prefab 或 RoomCatalog。", this);
                return null;
            }

            Transform parent = dungeonRoot != null ? dungeonRoot : transform;
            System.Random contentRandom = new System.Random(unchecked(seed ^ 0x2C9277B5));
            Dictionary<int, Room> rooms = new Dictionary<int, Room>();

            for (int i = 0; i < graph.Count; i ++)
            {
                // 生成房间
                RoomNode node = graph.Nodes[i];
                Vector3 worldPosition = new Vector3(
                    node.GridPosition.x * horizontalSpacing,
                    node.GridPosition.y * vertivalSpacing,
                    0f);
                Room room = Instantiate(roomPrefab, worldPosition, Quaternion.identity, parent);
                room.name = $"Room_{node.Id}_{node.Type}_{node.GridPosition.x}_{node.GridPosition.y}";
                // 加载房间配置
                RoomConfig config = null;
                if (!roomCatalog.TryChoose(node.Type, node.Depth, contentRandom, out config, out bool fallback))
                {
                    GameDebug.Error(DebugCategory.Level, $"{node.Type} 没有可用的 RoomConfig。", room);
                }
                else if (fallback)
                {
                    GameDebug.Warning(DebugCategory.Level, $"{node.Type} 在深度 {node.Depth} 没有匹配配置，已回退到该类型的任意配置。", room);
                }
                room.Initialize(node, config);
                rooms.Add(node.Id, room);
            }
            // 
            ConfigureConnectionsAndCorridors(graph, rooms, parent);
            rooms.TryGetValue(graph.StartId, out Room startRoom);
            return new DungeonBuildResult(rooms, startRoom);
        }
        private void ConfigureConnectionsAndCorridors(LevelGraph graph, IReadOnlyDictionary<int, Room> rooms, Transform parent)
        {
            for (int i = 0; i < graph.Count; i ++)
            {
                RoomNode node = graph.Nodes[i];
                Room room = rooms[node.Id];
                foreach (RoomDirection direction in RoomDirectionUtility.All)
                    room.ConfigureConnection(direction, node.GetNeighborId(direction) >= 0);
                CreateCorridorIfNeeded(node, RoomDirection.East, horizontalCorridorPrefab, rooms, parent);
                CreateCorridorIfNeeded(node, RoomDirection.North, verticalCorridorPrefab, rooms, parent);
            }
        }
        private void CreateCorridorIfNeeded(RoomNode node, RoomDirection direction, GameObject corridorPrefab, IReadOnlyDictionary<int, Room> rooms, Transform parent)
        {
            int neighborId = node.GetNeighborId(direction);
            if (neighborId < 0) return ;
            if (corridorPrefab == null)
            {
                GameDebug.Error(DebugCategory.Level, $"{direction} 通道预制体未连接。", this);
                return ;
            }

            Room fromRoom = rooms[node.Id];
            Room toRoom = rooms[neighborId];
            RoomConnection from = fromRoom.GetConnection(direction);
            RoomConnection to = toRoom.GetConnection(RoomDirectionUtility.Opposite(direction));
            if (from == null || to == null)
            {
                GameDebug.Error(DebugCategory.Level, $"Room {node.Id} 或 Room {neighborId} 缺少 {direction} 连接组件。", this);
                return ;
            }
            Vector3 middle = (from.CorridorAnchor.position + to.CorridorAnchor.position) * 0.5f;
            GameObject corridor = Instantiate(corridorPrefab, middle, Quaternion.identity, parent);
            corridor.name = $"Corridor_{node.Id}_{neighborId}";
        }
    }
}
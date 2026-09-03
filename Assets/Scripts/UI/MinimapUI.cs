using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 根据 RoomMapData 的网格坐标绘制房间、连线、探索与清空状态。
    /// </summary>
    public class MinimapUI : MonoBehaviour
    {
        private sealed class ConnectionVisual
        {
            public Image Image;
            public int A;
            public int B;
        }
        [SerializeField] private bool initVisible = true;
        [SerializeField] private RectTransform connectionContainer;
        [SerializeField] private RectTransform iconContainer;
        [SerializeField] private Image iconPrefab;
        [SerializeField] private Image connectionPrefab;
        [SerializeField] private Vector2 cellSize = new Vector2(48f, 48f);
        [SerializeField] private float connectionThickness = 6f;

        [Header("颜色")]
        [SerializeField] private Color currentColor = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private Color clearedColor = new Color(0.3f, 0.7f, 0.4f);
        [SerializeField] private Color normalColor = new Color(0.55f, 0.55f, 0.6f);
        [SerializeField] private Color eliteColor = new Color(0.75f, 0.3f, 0.85f);
        [SerializeField] private Color treasureColor = new Color(1f, 0.65f, 0.1f);
        [SerializeField] private Color recoveryColor = new Color(0.2f, 0.8f, 0.75f);
        [SerializeField] private Color bossColor = new Color(0.85f, 0.25f, 0.25f);
        [SerializeField] private Color connectionColor = new Color(0.45f, 0.45f, 0.5f);

        private readonly Dictionary<int, RoomMapData> roomById = new Dictionary<int, RoomMapData>();
        private readonly Dictionary<int, Image> iconById = new Dictionary<int, Image>();
        private readonly HashSet<int> discovered = new HashSet<int>();
        private readonly HashSet<int> cleared = new HashSet<int>();
        private readonly List<ConnectionVisual> connections = new List<ConnectionVisual>();
        private int currentRoomId = -1;
        private Vector2 mapOffset;

        private void OnEnable()
        {
            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Subscribe<RoomEnteredEvent>(OnRoomEntered);
            EventBus.Subscribe<RoomClearedEvent>(OnRoomCleared);
            EventBus.Subscribe<RunEndedEvent>(OnRunEnded);
        }
        private void OnDisable()
        {
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Unsubscribe<RoomEnteredEvent>(OnRoomEntered);
            EventBus.Unsubscribe<RoomClearedEvent>(OnRoomCleared);
            EventBus.Unsubscribe<RunEndedEvent>(OnRunEnded);
        }
        private void OnLevelStarted(LevelStartedEvent e)
        {
            ClearVisuals();
            if (e.Rooms == null || e.Rooms.Length == 0) return ;

            CalculateMapOffset(e.Rooms);
            for (int i = 0; i < e.Rooms.Length; i ++)
                roomById[e.Rooms[i].Id] = e.Rooms[i];
            CreateConnections(e.Rooms);
            CreateIcons(e.Rooms);

            for (int i = 0; i < e.Rooms.Length; i ++)
                if (initVisible || e.Rooms[i].Type == RoomType.Start) Discover(e.Rooms[i].Id);
            Refresh();
        }
        private void OnRoomEntered(RoomEnteredEvent e)
        {
            currentRoomId = e.RoomId;
            Discover(e.RoomId);
            if (roomById.TryGetValue(e.RoomId, out RoomMapData room))
            {
                Discover(room.NorthId);
                Discover(room.EastId);
                Discover(room.SouthId);
                Discover(room.WestId);
            }
            Refresh();
        }
        private void OnRoomCleared(RoomClearedEvent e)
        {
            cleared.Add(e.RoomId);
            Refresh();
        }
        private void OnRunEnded(RunEndedEvent e)
        {
            currentRoomId = -1;
            Refresh();
        }
        // 地图元素创建
        private void CreateIcons(IReadOnlyList<RoomMapData> rooms)
        {
            for (int i = 0; i < rooms.Count; i ++)
            {
                RoomMapData room = rooms[i];
                Image icon = Instantiate(iconPrefab, iconContainer);
                icon.name = $"RoomIcon_{room.Id}_{room.Type}";
                ((RectTransform)icon.transform).anchoredPosition = ToUIPosition(room.GridPosition);
                iconById.Add(room.Id, icon);
            }
        }
        private void CreateConnections(IReadOnlyList<RoomMapData> rooms)
        {
            for (int i = 0; i < rooms.Count; i ++)
            {
                RoomMapData room = rooms[i];
                if (room.EastId >= 0) CreateConnection(room.Id, room.EastId);
                if (room.NorthId >= 0) CreateConnection(room.Id, room.NorthId);
            }
        }
        private void CreateConnection(int a, int b)
        {
            if (!roomById.TryGetValue(a, out RoomMapData room_a) || !roomById.TryGetValue(b, out RoomMapData room_b))
                return ;
            Vector2 start = ToUIPosition(room_a.GridPosition);
            Vector2 end = ToUIPosition(room_b.GridPosition);
            Vector2 delta = end - start;
            Image image = Instantiate(connectionPrefab, connectionContainer);
            RectTransform rect = (RectTransform)image.transform;
            rect.anchoredPosition = (start + end) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, connectionThickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            image.color = connectionColor;
            connections.Add(new ConnectionVisual { Image = image, A = a, B = b });
        }
        // 刷新
        private void Refresh()
        {
            foreach (KeyValuePair<int, Image> pair in iconById)
            {
                bool visible = discovered.Contains(pair.Key);
                pair.Value.gameObject.SetActive(visible);
                if (visible) pair.Value.color = GetRoomColor(pair.Key);
            }
            for (int i = 0; i < connections.Count; i ++)
            {
                ConnectionVisual connection = connections[i];
                connection.Image.gameObject.SetActive(
                    discovered.Contains(connection.A) && discovered.Contains(connection.B));
            }
        }
        // 内部工具函数
        private Color GetRoomColor(int roomId)
        {
            if (roomId == currentRoomId) return currentColor;
            if (cleared.Contains(roomId)) return clearedColor;
            if (!roomById.TryGetValue(roomId, out RoomMapData room))
                return normalColor;
            switch (room.Type)
            {
                case RoomType.Elite: return eliteColor;
                case RoomType.Treasure: return treasureColor;
                case RoomType.Recovery: return recoveryColor;
                case RoomType.Boss: return bossColor;
                default: return normalColor;
            }
        }
        private void CalculateMapOffset(IReadOnlyList<RoomMapData> rooms)
        {
            Vector2Int min = rooms[0].GridPosition;
            Vector2Int max = rooms[0].GridPosition;
            for (int i = 1; i < rooms.Count; i++)
            {
                min = Vector2Int.Min(min, rooms[i].GridPosition);
                max = Vector2Int.Max(max, rooms[i].GridPosition);
            }
            Vector2 center = ((Vector2)min + (Vector2)max) * 0.5f;
            mapOffset = new Vector2(-center.x * cellSize.x, -center.y * cellSize.y);
        }
        private Vector2 ToUIPosition(Vector2Int gridPosition)
        {
            return new Vector2(gridPosition.x * cellSize.x, gridPosition.y * cellSize.y) + mapOffset;
        }
        private void Discover(int roomId)
        {
            if (roomId >= 0) discovered.Add(roomId);
        }
        private void ClearVisuals()
        {
            DestoryChildren(iconContainer);
            DestoryChildren(connectionContainer);
            roomById.Clear();
            iconById.Clear();
            discovered.Clear();
            cleared.Clear();
            connections.Clear();
            currentRoomId = -1;
        }
        private static void DestoryChildren(Transform parent)
        {
            if (parent == null) return ;
            for (int i = parent.childCount - 1; i >= 0; i --)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}

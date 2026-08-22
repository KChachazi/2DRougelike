using UnityEngine;
using UnityEngine.UI;
using Game.Core;

namespace Game.UI
{
    /// <summary>小地图 UI，订阅关卡和房间相关事件。</summary>
    public class MinimapUI : MonoBehaviour
    {
        [SerializeField] private Transform iconContainer;
        [SerializeField] private Image iconPrefab;

        [Header("颜色")]
        [SerializeField] private Color currentColor     = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private Color clearedColor     = new Color(0.3f, 0.7f, 0.4f);
        [SerializeField] private Color bossColor        = new Color(0.85f, 0.25f, 0.25f);
        [SerializeField] private Color unvisitedColor   = new Color(0.35f, 0.35f, 0.4f);

        private Image[] icons;
        private RoomMapData[] roomDatas;
        private bool[] clearedFlags;
        private int currentIndex = -1;

        private void OnEnable()
        {
            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Subscribe<RoomEnteredEvent>(OnRoomEntered);
            EventBus.Subscribe<RoomClearedEvent>(OnRoomCleared);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
        }
        private void OnDisable()
        {
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Unsubscribe<RoomEnteredEvent>(OnRoomEntered);
            EventBus.Unsubscribe<RoomClearedEvent>(OnRoomCleared);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
        }

        private void OnLevelStarted(LevelStartedEvent e)
        {
            for (int i = iconContainer.childCount - 1; i >= 0; i --)
                Destroy(iconContainer.GetChild(i).gameObject);
            roomDatas = e.Rooms;
            clearedFlags = new bool[roomDatas.Length];
            icons = new Image[roomDatas.Length];
            for (int i = 0; i < roomDatas.Length; i ++)
                icons[i] = Instantiate(iconPrefab, iconContainer);
            Refresh();
        }
        private void OnRoomEntered(RoomEnteredEvent e) { currentIndex = e.RoomId; Refresh(); }
        private void OnRoomCleared(RoomClearedEvent e) { clearedFlags[e.RoomId] = true; Refresh(); }
        private void OnLevelCompleted(LevelCompletedEvent e) { currentIndex = -1; Refresh(); }
        private void Refresh()
        {
            if (icons == null) return ;
            for (int i = 0; i < icons.Length; i ++)
                icons[i].color = GetColor(i);
        }
        private Color GetColor(int index)
        {
            if (index == currentIndex) return currentColor;
            if (clearedFlags[index]) return clearedColor;
            if (roomDatas[index].Type == RoomType.Boss) return bossColor;
            return unvisitedColor;
        }
    }
}

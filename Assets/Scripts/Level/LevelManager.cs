using Game.Core;
using Game.Debug;
using UnityEngine;

namespace Game.Level
{
    /// <summary>关卡管理器，负责维护房间切换、玩家传送等。</summary>
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private Room[] rooms;
        private int currentIndex = -1;

        private void OnEnable()
        {
            EventBus.Subscribe<DoorEnteredEvent>(OnDoorEntered);
            foreach (Room room in rooms)
                if (room != null) room.RoomCleared += OnRoomCleared;
        }
        private void OnDisable()
        {
            EventBus.Unsubscribe<DoorEnteredEvent>(OnDoorEntered);
            foreach (Room room in rooms)
                if (room != null) room.RoomCleared -= OnRoomCleared;
        }

        private void Start()
        {
            RoomType[] types = new RoomType[rooms.Length];
            for (int i = 0; i < rooms.Length; i ++)
                types[i] = rooms[i].Config != null ? rooms[i].Config.type : RoomType.Normal;
            EventBus.Publish(new LevelStartedEvent(types));
            EnterRoom(0);
        }

        private void OnDoorEntered(DoorEnteredEvent e) => EnterRoom(currentIndex + 1);
        private void EnterRoom(int index)
        {
            if (index < 0 || index >= rooms.Length)
            {
                EventBus.Publish(new LevelCompletedEvent());
                GameDebug.Log(DebugCategory.Level, "恭喜通关！", this);
                return ;
            }
            if (currentIndex >= 0) rooms[currentIndex].Exit();

            currentIndex = index;
            Room room = rooms[index];
            TeleportPlayer(room.EntryPoint);
            room.Enter();

            EventBus.Publish(new RoomEnteredEvent(index));
        }
        private void OnRoomCleared(Room room)
        {
            int index = System.Array.IndexOf(rooms, room);
            if (index < 0) return ;
            EventBus.Publish(new RoomClearedEvent(index));
        }

        private void TeleportPlayer(Transform entry)
        {
            GameObject player = GameManager.Instance != null
                              ? GameManager.Instance.Player
                              : GameObject.FindGameObjectWithTag("Player");
            if (player == null || entry == null) return ;
            
            if (player.TryGetComponent(out Rigidbody2D rb))
            {
                rb.position = entry.position;
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
}
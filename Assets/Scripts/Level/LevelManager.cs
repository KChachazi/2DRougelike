using System.Collections.Generic;
using Game.Core;
using Game.Debug;
using Game.Entities;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 生成并建造本局地图，协调房间事件、相机、恢复与最终通关。
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private LevelGenerationSettings generationSettings;
        [SerializeField] private DungeonBuilder dungeonBuilder;
        [SerializeField] private GameObject explorationCamera;

        private readonly List<Room> runtimeRooms = new List<Room>();
        private LevelGraph graph;
        private Room currentRoom;
        private void Start()
        {
            int seed = RunManager.Instance != null ? RunManager.Instance.CurrentSeed : 0x0d000721;
            if (!LevelGraphGenerator.TryGenerate(generationSettings, seed, out graph, out string error))
            {
                GameDebug.Error(DebugCategory.Level, error, this);
                enabled = false;
                return ;
            }
            DungeonBuildResult buildResult = dungeonBuilder != null ? dungeonBuilder.Build(graph, seed) : null;
            if (buildResult == null || buildResult.StartRoom == null)
            {
                GameDebug.Error(DebugCategory.Level, "实体地图建造失败", this);
                enabled = false;
                return ;
            }

            foreach (Room room in buildResult.Rooms.Values)
            {
                runtimeRooms.Add(room);
                room.Entered += OnRoomEntered;
                room.Exited += OnRoomExited;
                room.RoomCleared += OnRoomCleared;
            }
            if (explorationCamera != null) explorationCamera.SetActive(true);
            EventBus.Publish(new LevelStartedEvent(graph.CreateMapSnapshot()));
            PositionPlayer(buildResult.StartRoom.PlayerSpawnPoint);
            buildResult.StartRoom.HandlePlayerEntered();
            GameDebug.Log(DebugCategory.Level, $"地图生成完成：Seed={seed}，Rooms={graph.Count}，Signature={graph.BuildSignature()}。", this);
        }
        private void OnDisable()
        {
            for (int i = 0; i < runtimeRooms.Count; i ++)
            {
                Room room = runtimeRooms[i];
                if (room == null) continue;
                room.Entered -= OnRoomEntered;
                room.Exited -= OnRoomExited;
                room.RoomCleared -= OnRoomCleared;
            }
            runtimeRooms.Clear();
        }
        private void OnRoomEntered(Room room)
        {
            if (currentRoom != null && currentRoom != room) currentRoom.SetCameraActive(false);
            currentRoom = room;
            if (explorationCamera != null) explorationCamera.SetActive(false);
            room.SetCameraActive(true);
            EventBus.Publish(new RoomEnteredEvent(room.Node.Id));
        }
        private void OnRoomExited(Room room)
        {
            if (currentRoom != room) return ;
            room.SetCameraActive(false);
            currentRoom = null;
            if (explorationCamera != null) explorationCamera.SetActive(true);
        }
        private void OnRoomCleared(Room room)
        {
            EventBus.Publish(new RoomClearedEvent(room.Node.Id));
            if (room.Type != RoomType.Boss) return ;
            if (RunManager.Instance != null) RunManager.Instance.CompleteRun();
            else EventBus.Publish(new LevelCompletedEvent());
        }
        private void OnRewardRequested(Room room)
        {
            GameDebug.Error(DebugCategory.Level, "当前还不存在 upgradeManager", this);
            // if (upgradeManager)
        }
        private static void PositionPlayer(Transform spawnPoint)
        {
            GameObject player = GameManager.Instance != null ? GameManager.Instance.Player : GameObject.FindGameObjectWithTag("Player");
            if (player == null || spawnPoint == null) return ;
            if (player.TryGetComponent(out Rigidbody2D rb))
            {
                rb.position = spawnPoint.position;
                rb.linearVelocity = Vector2.zero;
            }
            else
                player.transform.position = spawnPoint.position;
        }
    }
 }

using System;
using System.Collections.Generic;
using Game.Core;
using Game.Entities;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 一个实体房间：延迟生成内容、管理战斗门、奖励等待和固定相机。
    /// </summary>
    public class Room : MonoBehaviour
    {
        [SerializeField] private RoomConfig config;
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private RoomConnection[] connections;
        [SerializeField] private GameObject roomCamera;
        [SerializeField] private Transform contentParent;

        public event Action<Room> Entered;
        public event Action<Room> Exited;
        public event Action<Room> RoomCleared;
        public event Action<Room> RewardRequested;

        private readonly List<Health> trackedEnemies = new List<Health>();
        private int aliveCount;
        private bool initialized;
        private bool spawned;
        private bool playerInside;
        private bool rewardPending;
        private bool recoveryConsumed;

        public RoomNode Node { get; private set; }
        public RoomConfig Config => config;
        public RoomType Type => Node != null ? Node.Type : config != null ? config.type : RoomType.Normal;
        public Transform PlayerSpawnPoint => playerSpawnPoint != null ? playerSpawnPoint : transform;
        public bool IsCleared { get; private set; }

        public void Initialize(RoomNode node, RoomConfig roomConfig)
        {
            Node = node;
            config = roomConfig;
            initialized = node != null;
            if (roomCamera != null) roomCamera.SetActive(false);
        }
        public void ConfigureConnection(RoomDirection direction, bool connected)
        {
            RoomConnection connection = GetConnection(direction);
            if (connection != null) connection.Configure(connected);
        }
        public RoomConnection GetConnection(RoomDirection direction)
        {
            if (connections == null) return null;
            for (int i = 0; i < connections.Length; i ++)
                if (connections[i] != null && connections[i].Direction == direction) return connections[i];
            return null;
        }
        public void HandlePlayerEntered()
        {
            if (!initialized || playerInside) return ;
            playerInside = true;
            Entered?.Invoke(this);
            if (spawned) return ;
            spawned = true;
            SpawnContents();
            if (aliveCount > 0) LockConnectedGates();
            else CompleteEncounter();
        }
        public void HandlePlayerExited()
        {
            if (!playerInside) return ;
            playerInside = false;
            Exited?.Invoke(this);
        }
        public void CompleteReward()
        {
            if (!rewardPending) return ;
            rewardPending = false;
            UnlockConnectedGates();
        }
        public bool TryConsumeRecovery(out int amount)
        {
            amount = 0;
            if (recoveryConsumed || Type != RoomType.Recovery || config == null || config.healAmount <= 0)
                return false;
            recoveryConsumed = true;
            amount = config.healAmount;
            return true;
        }
        public void SetCameraActive(bool active)
        {
            if (roomCamera != null) roomCamera.SetActive(active);
        }
        private void SpawnContents()
        {
            if (config == null) return ;
            Transform parent = contentParent != null ? contentParent : transform;
            if (config.enemySpawns != null)
            {
                foreach (EnemySpawn spawn in config.enemySpawns)
                {
                    Vector3 worldPosition = transform.position + (Vector3)spawn.localPosition;
                    GameObject enemy = EnemyFactory.Create(spawn.prefab, worldPosition, parent);
                    if (enemy == null || !enemy.TryGetComponent(out Health health)) continue;
                    health.Died += OnEnemyDied;
                    trackedEnemies.Add(health);
                    aliveCount ++;
                }
            }
            if (config.pickupSpawns != null)
            {
                foreach (PickupSpawn spawn in config.pickupSpawns)
                {
                    Vector3 worldPosition = transform.position + (Vector3)spawn.localPosition;
                    EnemyFactory.Create(spawn.prefab, worldPosition, parent);
                }
            }
        }
        private void OnEnemyDied()
        {
            aliveCount --;
            if (aliveCount <= 0) CompleteEncounter();
        }
        private void CompleteEncounter()
        {
            if (IsCleared) return ;
            IsCleared = true;
            RoomCleared?.Invoke(this);
            if (config != null && config.grantsUpgradeReward)
            {
                rewardPending = true;
                LockConnectedGates();
                RewardRequested?.Invoke(this);
            }
            else
                UnlockConnectedGates();
        }
        private void LockConnectedGates()
        {
            if (connections == null) return ;
            for (int i = 0; i < connections.Length; i ++)
                if (connections[i] != null && connections[i].IsConnected && connections[i].Gate != null)
                    connections[i].Gate.Lock();
        }
        private void UnlockConnectedGates()
        {
            if (connections == null) return;
            for (int i = 0; i < connections.Length; i++)
                if (connections[i] != null && connections[i].IsConnected && connections[i].Gate != null)
                    connections[i].Gate.Unlock();
        }
        private void OnDestroy()
        {
            for (int i = 0; i < trackedEnemies.Count; i++)
                if (trackedEnemies[i] != null) trackedEnemies[i].Died -= OnEnemyDied;
            trackedEnemies.Clear();
        }
    }
}
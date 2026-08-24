using System;
using System.Collections.Generic;
using Game.Core;
using Game.Entities;
using UnityEngine;

namespace Game.Level
{
    /// <summary>依据 RoomConfig 生成房间、统计存活敌人并管理门与房间相机。</summary>
    public class Room : MonoBehaviour
    {
        [SerializeField] private RoomConfig config;
        [Tooltip("玩家位置")]
        [SerializeField] private Transform entryPoint;
        [Tooltip("通往下个房间的门，可留空")]
        [SerializeField] private Door door;
        [Tooltip("本房间的 CinemachineCamera")]
        [SerializeField] private GameObject roomCamera;
        [Tooltip("生成对象挂载位置，留空默认房间自己")]
        [SerializeField] private Transform contentParent;

        public event Action<Room> RoomCleared;

        public RoomConfig Config => config;
        public Transform EntryPoint => entryPoint;
        public bool IsCleared { get; private set; }

        private readonly List<Health> trackedEnemies = new List<Health>();
        private int aliveCount;
        private bool spawned;

        /// <summary>
        /// 进入当前房间。
        /// </summary>
        public void Enter()
        {
            if (roomCamera != null) roomCamera.SetActive(true);
            if (!spawned)
            {
                SpawnContents();
                spawned = true;
            }
            if (aliveCount <= 0) MarkCleared();
        }

        /// <summary>
        /// 退出当前房间。
        /// </summary>
        public void Exit()
        {
            if (roomCamera != null) roomCamera.SetActive(false);
        }

        private void SpawnContents()
        {
            Transform parent = contentParent != null ? contentParent : transform;
            foreach (EnemySpawn spawn in config.enemySpawns)
            {
                Vector3 worldPos = transform.position + (Vector3)spawn.localPosition;
                GameObject enemy = EnemyFactory.Create(spawn.prefab, worldPos, parent);
                if (enemy == null) continue;
                if (enemy.TryGetComponent(out Health health))
                {
                    health.Died += OnEnemyDied;
                    trackedEnemies.Add(health);
                    aliveCount ++;
                }
            }
            foreach (PickupSpawn spawn in config.pickupSpawns)
            {
                Vector3 worldPos = transform.position + (Vector3)spawn.localPosition;
                EnemyFactory.Create(spawn.prefab, worldPos, parent);
            }
        }

        private void OnEnemyDied()
        {
            aliveCount --;
            if (aliveCount <= 0 && !IsCleared) MarkCleared();
        }
        private void MarkCleared()
        {
            IsCleared = true;
            if (door != null) door.Unlock();
            RoomCleared?.Invoke(this);
        }
        private void OnDestroy()
        {
            foreach (Health health in trackedEnemies)
            {
                if (health != null) health.Died -= OnEnemyDied;
            }
            trackedEnemies.Clear();
        }
    }
}
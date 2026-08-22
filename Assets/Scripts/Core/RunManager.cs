using System;
using System.Collections.Generic;
using Game.Entities;
using Game.Debug;
using Game.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class RunManager : MonoBehaviour
    {
        private static bool hasPendingSeed;
        private static int pendingSeed;
        public static RunManager Instance { get; private set; }
        [Header("Seed")]
        [SerializeField] private bool randomizeSeed = true;
        [SerializeField] private int fixedSeed = 0x0d000721;
        [SerializeField] private bool freezeTimeOnRunEnd = true;

        private readonly HashSet<int> visitedRooms = new HashSet<int>();
        private Health playerHealth;
        private bool ended;
        private int enemiesDefeated;
        private int upgradesCollected;

        public int CurrentSeed { get; private set; }
        public bool HasEnded => ended;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            hasPendingSeed = false;
            pendingSeed = 0x0d000721;
        }
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return ;
            }
            Instance = this;
            Time.timeScale = 1f;
            if (hasPendingSeed)
            {
                CurrentSeed = pendingSeed;
                hasPendingSeed = false;
            }
            else
            {
                CurrentSeed = randomizeSeed ? CreateRandomSeed() : fixedSeed;
            }
        }
        private void Start()
        {
            GameObject player = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (player != null && player.TryGetComponent(out playerHealth))
                playerHealth.Died += OnPlayerDied;
            else
                GameDebug.Error(DebugCategory.System, "RunManager 找不到玩家 Health，无法触发失败结算。", this);
            EventBus.Publish(new RunStartedEvent(CurrentSeed));
            GameDebug.Log(DebugCategory.Level, $"新的一局游戏开始了，Seed = {CurrentSeed}", this);
        }
        private void OnEnable()
        {
            EventBus.Subscribe<RoomEnteredEvent>(OnRoomEntered);
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Subscribe<RunUpgradeSelectedEvent>(OnUpgradeSelected);
        }
        private void OnDisable()
        {
            EventBus.Unsubscribe<RoomEnteredEvent>(OnRoomEntered);
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Unsubscribe<RunUpgradeSelectedEvent>(OnUpgradeSelected);
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
            if (Instance == this) Instance = null;
        }
        // ======================== 公开接口 ========================
        public void CompleteRun()
        {
            EndRun(RunResult.Victory);
        }
        public void RestartSameSeed()
        {
            hasPendingSeed = true;
            pendingSeed = CurrentSeed;
            ReloadCurrentScene();
        }
        public void RestartNewSeed()
        {
            hasPendingSeed = false;
            ReloadCurrentScene();
        }
        // ======================== 私有工具 ========================
        private void OnPlayerDied()
        {
            EndRun(RunResult.Defeat);
        }
        private void EndRun(RunResult result)
        {
            if (ended) return ;
            ended = true;
            SetPlayerInputEnable(false);

            if (result == RunResult.Victory)
                EventBus.Publish(new LevelCompletedEvent());
            
            EventBus.Publish(new RunEndedEvent(
                result, CurrentSeed, visitedRooms.Count,
                enemiesDefeated, upgradesCollected
            ));
            GameDebug.Log(DebugCategory.System, $"本局游戏结束，{result}, Seed = {CurrentSeed}。", this);
            if (freezeTimeOnRunEnd) Time.timeScale = 0f;
        }
        private void OnRoomEntered(RoomEnteredEvent e) => visitedRooms.Add(e.RoomId);
        private void OnEnemyDied(EnemyDiedEvent e) => enemiesDefeated ++;
        private void OnUpgradeSelected(RunUpgradeSelectedEvent e) => upgradesCollected ++;
        private static int CreateRandomSeed()
        {
            return unchecked(Environment.TickCount * 397 ^ DateTime.UtcNow.Millisecond);
        }
        private static void SetPlayerInputEnable(bool enabled)
        {
            GameObject player = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (player != null && player.TryGetComponent(out PlayerInputHandler input))
                input.enabled = enabled;
        }
        private static void ReloadCurrentScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

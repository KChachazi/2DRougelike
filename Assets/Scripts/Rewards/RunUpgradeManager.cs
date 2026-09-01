using System.Collections.Generic;
using Game.Commands;
using Game.Core;
using Game.Debug;
using Game.UI;
using UnityEngine;

namespace Game.Rewards
{
    [RequireComponent(typeof(RunModifierSet))]
    public sealed class RunUpgradeManager : MonoBehaviour
    {
        [SerializeField] private RunUpgradeData[] upgradePool;
        [SerializeField] private RewardChoiceUI rewardChoiceUI;

        private readonly Dictionary<RunUpgradeData, int> stackCounts = new Dictionary<RunUpgradeData, int>();
        private RunModifierSet modifiers;
        private PlayerInputHandler inputHandler;
        private System.Random random;
        private bool presenting;

        private void Awake()
        {
            modifiers = GetComponent<RunModifierSet>();
            inputHandler = GetComponent<PlayerInputHandler>();
        }
        private void Start()
        {
            int seed = RunManager.Instance != null ? RunManager.Instance.CurrentSeed : 0;
            random = new System.Random(unchecked(seed ^ 0x5F3759DF));
        }
        public bool TryPresentChoices()
        {
            if (presenting)
            {
                GameDebug.Warning(DebugCategory.Level, "奖励 UI 正处于选择中，暂不消耗新的强化拾取物。", this);
                return false;
            }
            List<RunUpgradeData> eligible = CollectEligibleUpgrades();
            if (eligible.Count == 0)
            {
                GameDebug.Warning(DebugCategory.Level, "没有仍可获取的局内强化，本次拾取不再提供选择", this);
                return true;
            }
            if (rewardChoiceUI == null)
            {
                GameDebug.Error(DebugCategory.Level, "RunUpgradeManager 没有连接 RewardChoiceUI。", this);
                return false;
            }

            Shuffle(eligible);
            int choiceCount = Mathf.Min(3, eligible.Count);
            List<RunUpgradeData> choices = eligible.GetRange(0, choiceCount);
            presenting = true;
            if (inputHandler != null) inputHandler.enabled = false;
            rewardChoiceUI.Show(choices, OnUpgradeChosen);
            return true;
        }
        private List<RunUpgradeData> CollectEligibleUpgrades()
        {
            List<RunUpgradeData> result = new List<RunUpgradeData>();
            if (upgradePool == null) return result;
            for (int i = 0; i < upgradePool.Length; i ++)
            {
                RunUpgradeData upgrade = upgradePool[i];
                if (upgrade == null) continue ;
                int stacks = stackCounts.TryGetValue(upgrade, out int count) ? count : 0;
                if (stacks < upgrade.MaxStacks) result.Add(upgrade);
            }
            return result;
        }
        private void OnUpgradeChosen(RunUpgradeData upgrade)
        {
            if (!presenting || upgrade == null) return ;
            modifiers.Apply(upgrade);
            stackCounts[upgrade] = stackCounts.TryGetValue(upgrade, out int count) ? count + 1 : 1;
            EventBus.Publish(new RunUpgradeSelectedEvent(upgrade.DisplayName));
            GameDebug.Log(DebugCategory.Combat, $"获得局内强化：{upgrade.DisplayName}。", this);

            rewardChoiceUI.Hide();
            if (inputHandler != null) inputHandler.enabled = true;
            presenting = false;
        }
        private void Shuffle<T>(List<T> list)
        {
            if (random == null) random = new System.Random(0);
            for (int i = list.Count - 1; i > 0; i --)
            {
                int j = random.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
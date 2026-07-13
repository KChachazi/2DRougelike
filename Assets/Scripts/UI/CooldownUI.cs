using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class CooldownUI : MonoBehaviour
    {
        [Tooltip("UI SkillId")]
        [SerializeField] private SkillId skill;
        [Tooltip("技能图标上的遮罩")]
        [SerializeField] private Image cooldownMask;
        

        private float remaining;
        private float total;

        private void OnEnable()  => EventBus.Subscribe<SkillCooldownStartedEvent>(OnCooldownStarted);
        private void OnDisable() => EventBus.Unsubscribe<SkillCooldownStartedEvent>(OnCooldownStarted);

        private void OnCooldownStarted(SkillCooldownStartedEvent e)
        {
            if (e.Skill != skill) return;
            remaining = e.Cooldown;
            total = e.Cooldown;
        }

        private void Update()
        {
            if (cooldownMask == null || total <= 0f) return ;
            if (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                cooldownMask.fillAmount = Mathf.Clamp01(remaining / total);
            }
            else if (cooldownMask.fillAmount != 0f)
            {
                cooldownMask.fillAmount = 0f;
            }
        }
    }
}
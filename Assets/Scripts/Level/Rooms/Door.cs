using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 锁定时阻止通行；解锁后把玩家进入转换为全局门事件。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Door : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color lockedColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color unlockedColor = new Color(0.2f, 0.8f, 0.3f);

        private void Awake() => Lock();

        public void Lock()
        {
            if (spriteRenderer != null) spriteRenderer.color = lockedColor;
            if (this.TryGetComponent(out Collider2D collider))
                collider.enabled = true;
        }
        public void Unlock()
        {
            if (spriteRenderer != null) spriteRenderer.color = unlockedColor;
            if (this.TryGetComponent(out Collider2D collider))
                collider.enabled = false;
        }
    }
}
using Game.Core;
using UnityEngine;

namespace Game.Level
{
    [RequireComponent(typeof(Collider2D))]
    public class Door : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color lockedColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color unlockedColor = new Color(0.2f, 0.8f, 0.3f);

        private bool locked = true;
        private void Awake() => Lock();

        public void Lock()
        {
            locked = true;
            if (spriteRenderer != null) spriteRenderer.color = lockedColor;
        }
        public void Unlock()
        {
            locked = false;
            if (spriteRenderer != null) spriteRenderer.color = unlockedColor;
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (locked) return;
            if (!other.CompareTag("Player")) return ;
            EventBus.Publish(new DoorEnteredEvent());
        }
    }
}
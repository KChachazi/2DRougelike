using Game.Commands;
using UnityEngine;
using TMPro;

namespace Game.UI
{
    public class DebugText : MonoBehaviour
    {
        [Header("Debug文本")]
        [SerializeField] private TMP_Text debugText;
        [SerializeField] bool display;

        [Header("CommandBuffer")]
        [SerializeField] private PlayerInputHandler playerInputHandler;

        private void Awake()
        {
            if (!display) gameObject.SetActive(false);
        }

        private void Update()
        {
            if (playerInputHandler == null || debugText == null) return ;
            InputBuffer buffer = playerInputHandler.Buffer;
            if (buffer.Empty())
                debugText.text = "Buffer(0): —";
            else 
                debugText.text = $"Buffer({buffer.Count}): {buffer.Peek().GetType().Name}";
        }
    }
}
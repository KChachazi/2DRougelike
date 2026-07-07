using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Entities
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        private Rigidbody2D rb;
        private Camera mainCamera;
        private Vector2 moveInput;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            ReadMoveInput();
            RotateTowardsMouse();
        }

        private void FixedUpdate()
        {
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }

        private void ReadMoveInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                moveInput = Vector2.zero;
                return ;
            }
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                    - (kb.aKey.isPressed || kb.leftArrowKey.isPressed  ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed    ? 1f : 0f)
                    - (kb.sKey.isPressed || kb.downArrowKey.isPressed  ? 1f : 0f);
            moveInput = new Vector2(x, y).normalized;
        }

        private void RotateTowardsMouse()
        {
            if (Mouse.current == null || mainCamera == null) return;
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));

            Vector2 direction = (Vector2)worldPos - rb.position;
            if (direction.sqrMagnitude < 0.0001f) return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rb.rotation = angle;
        }
    }
}
using Game.Entities;
using Game.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Commands
{
    /// <summary>
    /// 玩家输入的唯一读取入口。
    /// </summary>
    //
    // 持续输入直接执行，离散输入进入缓冲队列，
    // 其余游戏对象只接收已经解释好的命令或方向数据。
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("输入缓冲")]
        [Tooltip("超时等待")]
        [SerializeField] private float bufferDuration = 0.25f;
        [Tooltip("队列长度")]
        [SerializeField] private int bufferCapacity = 3;

        private PlayerController player;
        private WeaponController weapon;
        private GrenadeThrower grenadeThrower;
        private InputBuffer buffer;
        private Camera mainCamera;

        private MoveCommand moveCommand;
        private AttackCommand attackCommand;
        private DashCommand dashCommand;
        private GrenadeCommand grenadeCommand;
        private SwitchWeaponCommand switchWeaponCommand;

        /// <summary>
        /// 当前输入缓冲的只读入口，供 DebugOverlay 观察队列状态；
        /// 外部系统不应通过它入队或执行命令。
        /// </summary>
        public InputBuffer Buffer => buffer;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            weapon = GetComponent<WeaponController>();
            grenadeThrower = GetComponent<GrenadeThrower>();
            buffer = new InputBuffer(bufferCapacity, bufferDuration);
            mainCamera = Camera.main;

            moveCommand = new MoveCommand(player);
            attackCommand = new AttackCommand(weapon);
            dashCommand = new DashCommand(player);
            grenadeCommand = new GrenadeCommand(player, grenadeThrower);
            switchWeaponCommand = new SwitchWeaponCommand(weapon);
        }
        private void Update()
        {
            ReadMove();
            ReadAim();
            ReadContinuousFire();
            ReadDiscreteActions();
            ReadWeaponSwitch();
            buffer.Tick();
        }
        private void OnDisable()
        {
            if (player != null)
                player.SetMoveInput(Vector2.zero);
            buffer?.Clear();
        }

        /* --------------- 持续动作 --------------- */
        private void ReadAim()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || mainCamera == null) return ;
            Vector2 screenPosition = mouse.position.ReadValue();
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z));
            player.SetAimDirection((Vector2)worldPosition - player.Rb.position);
        }
        private void ReadMove()
        {
            Vector2 direction = Vector2.zero;
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                        - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
                float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                        - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
                direction = new Vector2(x, y).normalized;
            }
            moveCommand.SetDirection(direction);
            moveCommand.Execute();
        }
        private void ReadContinuousFire()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.isPressed) return ;
            if (attackCommand.CanExecute())
                attackCommand.Execute();
        }

        /* --------------- 离散动作 --------------- */
        private void ReadDiscreteActions()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return ;
            if (kb.leftShiftKey.wasPressedThisFrame) buffer.Enqueue(dashCommand);
            if (kb.qKey.wasPressedThisFrame) buffer.Enqueue(grenadeCommand);
        }
        /* --------------- 立即动作 --------------- */
        private void ReadWeaponSwitch()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return ;
            if (kb.digit1Key.wasPressedThisFrame) TrySwitchWeapon(0);
            else if (kb.digit2Key.wasPressedThisFrame) TrySwitchWeapon(1);
            else if (kb.digit3Key.wasPressedThisFrame) TrySwitchWeapon(2);
        }
        private void TrySwitchWeapon(int index)
        {
            switchWeaponCommand.SwitchIndex(index);
            if (switchWeaponCommand.CanExecute())
                switchWeaponCommand.Execute();
        }
    }
}

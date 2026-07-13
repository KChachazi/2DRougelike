using Game.Entities;
using Game.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Commands
{
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

        private MoveCommand moveCommand;
        private AttackCommand attackCommand;
        private DashCommand dashCommand;
        private GrenadeCommand grenadeCommand;
        private SwitchWeaponCommand switchWeaponCommand;

        public InputBuffer Buffer => buffer;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            weapon = GetComponent<WeaponController>();
            grenadeThrower = GetComponent<GrenadeThrower>();
            buffer = new InputBuffer(bufferCapacity, bufferDuration);

            moveCommand = new MoveCommand(player);
            attackCommand = new AttackCommand(weapon);
            dashCommand = new DashCommand(player);
            grenadeCommand = new GrenadeCommand(player, grenadeThrower);
            switchWeaponCommand = new SwitchWeaponCommand(weapon);
        }

        private void Update()
        {
            ReadMove();
            ReadContinuousFire();
            ReadDiscreteActions();
            ReadWeaponSwitch();
            buffer.Tick();
        }

        /* --------------- 持续动作 --------------- */
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
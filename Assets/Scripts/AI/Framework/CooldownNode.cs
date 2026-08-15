using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 冷却装饰器，包装一个子节点，子节点"成功"后进入冷却。
    /// </summary>
    // 
    // 子节点返回 Success 则进入冷却，期间子节点被跳过；
    //      返回 Running/Failure 不触发冷却。
    // 冷却只在本节点被 Evaluate 时递减；
    //      分支未被行为树访问期间，冷却计时会暂停。
    // 
    // 典型用途：给技能/攻击动作加冷却——
    //   Cooldown( 攻击Action, 1f )   —— 每次攻击后 1 秒内无法再攻击
    public class CooldownNode : Node
    {
        private readonly Node child;
        private readonly float cooldownDuration;
        private float timer;
        public CooldownNode(Node child, float cooldownDuration)
        {
            this.child = child;
            this.cooldownDuration = cooldownDuration;
        }
        public override NodeState Evaluate()
        {
            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                return State = NodeState.Failure; // 冷却中，技能不可用
            }
            NodeState childState = child.Evaluate();
            if (childState == NodeState.Success)
                timer = cooldownDuration; // 成功释放技能，进入冷却
            return State = childState;
        }
    }
}
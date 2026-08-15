using System.Collections.Generic;

namespace Game.AI
{
    /// <summary>
    /// 组合节点抽象基类，Sequence / Selector 的共同父类。
    /// 含义为持有若干子节点，决定它们的执行方式。
    /// </summary>
    public abstract class CompositeNode : Node
    {
        /// <summary>子节点列表（按顺序执行）</summary>
        protected readonly List<Node> children = new List<Node>();
        protected CompositeNode(params Node[] childNodes)
        {
            if (childNodes != null)
                children.AddRange(childNodes);
        }
        /// <summary>运行时动态添加子节点</summary>
        public void AddChild(Node child) => children.Add(child);
    }
}
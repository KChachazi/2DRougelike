using System.Collections.Generic;

namespace Game.AI
{
    public abstract class CompositeNode : Node
    {
        protected readonly List<Node> children = new List<Node>();
        protected CompositeNode(params Node[] childNodes)
        {
            if (childNodes != null)
                children.AddRange(childNodes);
        }
        public void AddChild(Node child) => children.Add(child);
    }
}
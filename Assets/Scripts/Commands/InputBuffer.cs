using System.Collections.Generic;
using UnityEngine;

namespace Game.Commands
{
    public class InputBuffer
    {
        private readonly struct BufferedCommand
        {
            public readonly ICommand Command;
            public readonly float ExpireTime;
            public BufferedCommand(ICommand command, float expireTime)
            {
                Command = command;
                ExpireTime = expireTime;
            }
        }

        private readonly Queue<BufferedCommand> queue = new Queue<BufferedCommand>();
        private readonly int capacity;
        private readonly float bufferDuration;

        public int Count => queue.Count;

        public InputBuffer(int capacity, float bufferDuration)
        {
            this.capacity = capacity;
            this.bufferDuration = bufferDuration;
        }

        public void Enqueue(ICommand command)
        {
            if (queue.Count >= capacity)
                queue.Dequeue();
            queue.Enqueue(new BufferedCommand(command, Time.time + bufferDuration));
        }

        public bool Empty() => queue.Count == 0;
        public ICommand Peek() => queue.Peek().Command;
        
        public void Tick()
        {
            while (queue.Count > 0)
            {
                BufferedCommand head = queue.Peek();
                if (Time.time > head.ExpireTime) // 指令过期
                {
                    queue.Dequeue();
                    continue;
                }
                if (!head.Command.CanExecute())
                    return ;
                queue.Dequeue();
                head.Command.Execute();
                return ;
            }
        }

        public void Clear() => queue.Clear();
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace Game.Commands
{
    /// <summary>
    /// 有容量和有效期的命令队列；不可执行的队首会保留等待，每帧最多执行一个命令。
    /// </summary>
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
        /// <summary>
        /// 容量已满时会丢弃更早的命令。
        /// </summary>
        public void Enqueue(ICommand command)
        {
            if (queue.Count >= capacity)
                queue.Dequeue();
            queue.Enqueue(new BufferedCommand(command, Time.time + bufferDuration));
        }

        public bool Empty() => queue.Count == 0;
        /// <summary>
        /// 只能在队列非空时调用。
        /// </summary>
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
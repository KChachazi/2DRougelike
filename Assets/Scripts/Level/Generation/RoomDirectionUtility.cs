using Game.Core;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 实现房间方向与网格偏移之间稳定转换的静态工具。
    /// </summary>
    public static class RoomDirectionUtility
    {
        public static readonly RoomDirection[] All =
        {
            RoomDirection.North,
            RoomDirection.East,
            RoomDirection.South,
            RoomDirection.West,
        };
        public static Vector2Int ToOffset(RoomDirection direction)
        {
            switch (direction)
            {
                case RoomDirection.North: return Vector2Int.up;
                case RoomDirection.East: return Vector2Int.right;
                case RoomDirection.South: return Vector2Int.down;
                case RoomDirection.West: return Vector2Int.left;
                default: return Vector2Int.zero;
            }
        }
        public static RoomDirection Opposite(RoomDirection direction)
        {
            switch (direction)
            {
                case RoomDirection.North: return RoomDirection.South;
                case RoomDirection.East: return RoomDirection.West;
                case RoomDirection.South: return RoomDirection.North;
                case RoomDirection.West: return RoomDirection.East;
                default: return RoomDirection.North;
            }
        }
    }
}
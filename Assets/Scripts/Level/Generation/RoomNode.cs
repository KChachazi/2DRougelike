using Game.Core;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 程序化关卡中的一个纯数据房间节点。
    /// </summary>
    public sealed class RoomNode
    {
        private readonly int[] neighborIds = {-1, -1, -1, -1};

        public int Id { get; }
        public Vector2Int GridPosition { get; }
        public int Depth { get; }
        public RoomType Type { get; internal set; }
        public int Degree { get 
        {
            int count = 0;
            for (int i = 0; i < neighborIds.Length; i ++)
                if (neighborIds[i] >= 0) count ++;
            return count;
        }}
        public bool IsLeaf => Degree == 1;

        internal RoomNode(int id, Vector2Int gridPosition, int depth)
        {
            Id = id;
            GridPosition = gridPosition;
            Depth = depth;
            Type = RoomType.Normal;
        }
        public int GetNeighborId(RoomDirection direction) => neighborIds[(int)direction];
        internal void SetNeighbor(RoomDirection direction, int neighborId)
        {
            neighborIds[(int)direction] = neighborId;
        }
        public RoomMapData ToMapData()
        {
            return new RoomMapData(
                Id, GridPosition, Type,
                neighborIds[(int)RoomDirection.North],
                neighborIds[(int)RoomDirection.East],
                neighborIds[(int)RoomDirection.South],
                neighborIds[(int)RoomDirection.West]
            );
        }
    }
}
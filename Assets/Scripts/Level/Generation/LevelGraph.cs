using Game.Core;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Level
{
    public sealed class LevelGraph
    {
        private readonly List<RoomNode> nodes = new List<RoomNode>();
        private readonly Dictionary<Vector2Int, RoomNode> nodesByPosition = new Dictionary<Vector2Int, RoomNode>();

        public IReadOnlyList<RoomNode> Nodes => nodes;
        public int Count => nodes.Count;
        public int StartId { get; internal set; } = -1;
        public int BossId { get; internal set; } = -1;

        internal RoomNode AddNode(Vector2Int position, int depth)
        {
            RoomNode node = new RoomNode(nodes.Count, position, depth);
            nodes.Add(node);
            nodesByPosition.Add(position, node);
            return node;
        }
        internal void Connect(RoomNode from, RoomDirection direction, RoomNode to)
        {
            from.SetNeighbor(direction, to.Id);
            to.SetNeighbor(RoomDirectionUtility.Opposite(direction), from.Id);
        }
        public RoomNode GetRoomNode(int id) =>
            id >= 0 && id < nodes.Count ? nodes[id] : null;
        public bool TryGetNodeAt(Vector2Int position, out RoomNode node)
            => nodesByPosition.TryGetValue(position, out node);
        public RoomMapData[] CreateMapSnapshot()
        {
            RoomMapData[] result = new RoomMapData[nodes.Count];
            for (int i = 0; i < nodes.Count; i ++)
                result[i] = nodes[i].ToMapData();
            return result;
        }
        public string BuildSignature()
        {
            StringBuilder builder = new StringBuilder(nodes.Count * 32);
            for (int i = 0; i < nodes.Count; i ++)
            {
                RoomNode node = nodes[i];
                builder.Append(node.Id).Append('@')
                    .Append(node.GridPosition.x).Append(',').Append(node.GridPosition.y)
                    .Append(':').Append((int)node.Type).Append(':');
                for (int d = 0; d < RoomDirectionUtility.All.Length; d ++)
                    builder.Append(node.GetNeighborId(RoomDirectionUtility.All[d])).Append(',');
                builder.Append('|');
            }
            return builder.ToString();
        }
    }
}
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 用于集中验证生成图的连通性、唯一性和玩法约束。
    /// </summary>
    public static class LevelGraphValidator
    {
        public static bool Validate(LevelGraph graph, LevelGenerationSettings settings, out string error)
        {
            if (graph == null)
            {
                error = "关卡图为空。";
                return false;
            }
            if (graph.Count < settings.MinRoomCount || graph.Count > settings.MaxRoomCount)
            {
                error = $"房间数 {graph.Count} 不在配置范围内。当前范围为 {settings.MinRoomCount} ~ {settings.MaxRoomCount}。";
                return false;
            }
            HashSet<Vector2Int> positions = new HashSet<Vector2Int>();
            int directedEdges = 0;
            int branchRooms = 0;
            int treasures = 0;
            int recoveries = 0;
            int elites = 0;
            for (int i = 0; i < graph.Count; i ++)
            {
                // 检验房间节点不能使用重复坐标
                RoomNode node = graph.Nodes[i];
                if (!positions.Add(node.GridPosition))
                {
                    error = $"坐标 {node.GridPosition} 被重复占用。";
                    return false;
                }
                if (node.Degree >= 3) branchRooms ++;
                if (node.Type == RoomType.Treasure) treasures ++;
                else if (node.Type == RoomType.Recovery) recoveries ++;
                else if (node.Type == RoomType.Elite) elites ++;
                // 检验房间间为双向连接
                foreach (RoomDirection direction in RoomDirectionUtility.All)
                {
                    int neighborId = node.GetNeighborId(direction);
                    if (neighborId < 0) continue;
                    directedEdges ++;
                    RoomNode neighbor = graph.GetRoomNode(neighborId);
                    if (neighbor == null)
                    {
                        error = $"Room {node.Id} 指向不存在的 Room {neighborId}。";
                        return false;
                    }
                    if (neighbor.GridPosition != node.GridPosition + RoomDirectionUtility.ToOffset(direction))
                    {
                        error = $"Room {node.Id} 的 {direction} 邻居不在相邻网格。";
                        return false;
                    }
                    if (neighbor.GetNeighborId(RoomDirectionUtility.Opposite(direction)) != node.Id)
                    {
                        error = $"Room {node.Id} 与 Room {neighborId} 的连接不是双向的。";
                        return false;
                    }
                }
            }
            // 首版地图必须是一棵树
            if (directedEdges / 2 != graph.Count - 1)
            {
                error = "首版地图必须是一棵树，连接数应为房间数减一。";
                return false;
            }
            // 有效分岔房间需要达到 settings 的数目
            if (branchRooms < settings.MinBranchRoomCount)
            {
                error = $"有效分岔房只有 {branchRooms} 个。";
                return false;
            }
            // 起始房、特殊类型房和 Boss 房间必须符合要求
            RoomNode start = graph.GetRoomNode(graph.StartId);
            RoomNode boss = graph.GetRoomNode(graph.BossId);
            if (start == null || start.Type != RoomType.Start)
            {
                error = "起始节点类型错误。";
                return false;
            }
            if (boss == null || boss.Type != RoomType.Boss || boss.Depth < settings.MinBossDepth)
            {
                error = "Boss 房类型或深度不满足配置。";
                return false;
            }
            if (treasures != settings.TreasureRoomCount || recoveries != settings.RecoveryRoomCount || elites != settings.EliteRoomCount)
            {
                error = "特殊房数量与配置不一致。";
                return false;
            }
            // 地图必须全连通
            HashSet<int> visited = new HashSet<int>();
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(graph.StartId);
            visited.Add(graph.StartId);
            while (queue.Count > 0)
            {
                RoomNode node = graph.GetRoomNode(queue.Dequeue());
                foreach (RoomDirection direction in RoomDirectionUtility.All)
                {
                    int neighborId = node.GetNeighborId(direction);
                    if (neighborId >= 0 && visited.Add(neighborId)) queue.Enqueue(neighborId);
                }
            }
            if (visited.Count != graph.Count)
            {
                error = $"地图不连通，只能到达 {visited.Count}/{graph.Count} 个房间。";
                return false;
            }
            // 检验通过
            error = null;
            return true;
        }
    }
}
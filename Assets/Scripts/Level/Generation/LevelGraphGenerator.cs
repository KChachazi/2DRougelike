using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Level
{
    public static class LevelGraphGenerator
    {
        private readonly struct FrontierCandidate
        {
            public readonly int ParentId;
            public readonly RoomDirection Direction;
            public readonly Vector2Int Position;
            public FrontierCandidate(int parentId, RoomDirection direction, Vector2Int position)
            {
                ParentId = parentId;
                Direction = direction;
                Position = position;
            }
        }
        public static bool TryGenerate(LevelGenerationSettings settings, int seed, out LevelGraph graph, out string error)
        {
            if (settings == null)
            {
                graph = null;
                error = "没有指定 LevelGenerationSettings。";
                return false;
            }

            string lastError = "未知生成错误。";
            for (int attempt = 0; attempt < settings.MaxGenerationAttempts; attempt ++)
            {
                int attemptSeed = unchecked(seed + attempt * 7919);
                System.Random random = new System.Random(attemptSeed);
                LevelGraph candidate = BuildTree(settings, random);
                if (candidate == null)
                {
                    lastError = "前沿耗尽，未能生成目标房间数。";
                    continue;
                }
                if (!AssignRoomTypes(candidate, settings, random, out lastError)) continue;
                if (!LevelGraphValidator.Validate(candidate, settings, out lastError)) continue;
                graph = candidate;
                error = null;
                return true;
            }
            graph = null;
            error = $"尝试 {settings.MaxGenerationAttempts} 次后仍未生成有效地图：{lastError}";
            return false;
        }

        private static LevelGraph BuildTree(LevelGenerationSettings settings, System.Random random)
        {
            int targetCount = random.Next(settings.MinRoomCount, settings.MaxRoomCount + 1);
            LevelGraph graph = new LevelGraph();
            RoomNode start = graph.AddNode(Vector2Int.zero, 0);
            graph.StartId = start.Id;
            while (graph.Count < targetCount)
            {
                List<FrontierCandidate> candidates = CollectFrontier(graph);
                if (candidates.Count == 0) return null;
                FrontierCandidate selected = candidates[random.Next(candidates.Count)];
                RoomNode parent = graph.GetRoomNode(selected.ParentId);
                RoomNode child = graph.AddNode(selected.Position, parent.Depth + 1);
                graph.Connect(parent, selected.Direction, child);
            }
            return graph;
        }
        private static List<FrontierCandidate> CollectFrontier(LevelGraph graph)
        {
            List<FrontierCandidate> result = new List<FrontierCandidate>();
            for (int i = 0; i < graph.Count; i++)
            {
                RoomNode node = graph.Nodes[i];
                foreach (RoomDirection direction in RoomDirectionUtility.All)
                {
                    Vector2Int position = node.GridPosition + RoomDirectionUtility.ToOffset(direction);
                    if (graph.TryGetNodeAt(position, out _)) continue;
                    result.Add(new FrontierCandidate(node.Id, direction, position));
                }
            }
            return result;
        }
        private static bool AssignRoomTypes(LevelGraph graph, LevelGenerationSettings settings, System.Random random, out string error)
        {
            for (int i = 0; i < graph.Count; i ++) graph.Nodes[i].Type = RoomType.Normal;
            graph.GetRoomNode(graph.StartId).Type = RoomType.Start;
            // 生成 Boss 房间
            List<RoomNode> farthestLeaves = new List<RoomNode>();
            int maxDepth = 1;
            for (int i = 0; i < graph.Count; i ++)
            {
                RoomNode node = graph.Nodes[i];
                if (!node.IsLeaf || node.Id == graph.StartId) continue;
                if (node.Depth > maxDepth)
                {
                    maxDepth = node.Depth;
                    farthestLeaves.Clear();
                    farthestLeaves.Add(node);
                }
                else if (node.Depth == maxDepth)
                {
                    farthestLeaves.Add(node);
                }
            }
            if (maxDepth < settings.MinBossDepth || farthestLeaves.Count == 0)
            {
                error = "没有足够远的叶节点可用作 Boss 房。";
                return false;
            }
            RoomNode boss = farthestLeaves[random.Next(farthestLeaves.Count)];
            boss.Type = RoomType.Boss;
            graph.BossId = boss.Id;
            // 生成特殊类型房间
            List<RoomNode> specialLeaves = new List<RoomNode>();
            for (int i = 0; i < graph.Count; i++)
            {
                RoomNode node = graph.Nodes[i];
                if (node.IsLeaf && node.Id != graph.StartId && node.Id != graph.BossId)
                    specialLeaves.Add(node);
            }
            Shuffle(specialLeaves, random);
            int requiredLeaves = settings.TreasureRoomCount + settings.RecoveryRoomCount;
            if (specialLeaves.Count < requiredLeaves)
            {
                error = $"特殊叶节点不足，需要 {requiredLeaves} 个，实际只有 {specialLeaves.Count} 个。";
                return false;
            }
            int cursor = 0;
            for (int i = 0; i < settings.TreasureRoomCount; i ++)
                specialLeaves[cursor ++].Type = RoomType.Treasure;
            for (int i = 0; i < settings.RecoveryRoomCount; i ++)
                specialLeaves[cursor ++].Type = RoomType.Recovery;
            // 生成精英怪房间
            List<RoomNode> eliteCandidates = new List<RoomNode>();
            for (int i = 0; i < graph.Count; i++)
            {
                RoomNode node = graph.Nodes[i];
                if (node.Type == RoomType.Normal && node.Depth >= settings.MinEliteDepth)
                    eliteCandidates.Add(node);
            }
            Shuffle(eliteCandidates, random);
            if (eliteCandidates.Count < settings.EliteRoomCount)
            {
                error = "满足深度要求的精英房候选不足。";
                return false;
            }
            for (int i = 0; i < settings.EliteRoomCount; i++)
                eliteCandidates[i].Type = RoomType.Elite;

            error = null;
            return true;
        }
        private static void Shuffle<T>(IList<T> list, System.Random random)
        {
            for (int i = list.Count - 1; i > 0; i --)
            {
                int j = random.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
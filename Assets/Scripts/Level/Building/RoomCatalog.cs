using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;

namespace Game.Level
{
    [CreateAssetMenu(fileName = "RoomCatalog", menuName = "Game/Room Catalog")]
    public sealed class RoomCatalog : ScriptableObject
    {
        [SerializeField] private RoomConfig[] startRooms;
        [SerializeField] private RoomConfig[] normalRooms;
        [SerializeField] private RoomConfig[] eliteRooms;
        [SerializeField] private RoomConfig[] treasureRooms;
        [SerializeField] private RoomConfig[] recoveryRooms;
        [SerializeField] private RoomConfig[] bossRooms;

        public bool TryChoose(RoomType type, int depth, System.Random random, out RoomConfig config, out bool usedDepthFallback)
        {
            RoomConfig[] pool = GetPool(type);
            List<RoomConfig> all = CollectNonNull(pool);
            if (all.Count == 0)
            {
                config = null;
                usedDepthFallback = false;
                return false;
            }
            List<RoomConfig> depthMatches = new List<RoomConfig>();
            for (int i = 0; i < all.Count; i ++)
                if (all[i].SupportsDepth(depth)) depthMatches.Add(all[i]);
            usedDepthFallback = depthMatches.Count == 0;
            config = ChooseWeighted(usedDepthFallback ? all : depthMatches, random);
            return config != null;
        }
        private RoomConfig[] GetPool(RoomType type)
        {
            switch (type)
            {
                case RoomType.Start: return startRooms;
                case RoomType.Normal: return normalRooms;
                case RoomType.Elite: return eliteRooms;
                case RoomType.Treasure: return treasureRooms;
                case RoomType.Recovery: return recoveryRooms;
                case RoomType.Boss: return bossRooms;
                default: return Array.Empty<RoomConfig>();
            }
        }
        private static List<RoomConfig> CollectNonNull(RoomConfig[] pool)
        {
            List<RoomConfig> result = new List<RoomConfig>();
            if (pool == null) return result;
            for (int i = 0; i < pool.Length; i ++)
                if (pool[i] != null) result.Add(pool[i]);
            return result;
        }
        private static RoomConfig ChooseWeighted(IReadOnlyList<RoomConfig> candidates, System.Random random)
        {
            int totalWeight = 0;
            for (int i = 0; i < candidates.Count; i ++)
                totalWeight += Mathf.Max(1, candidates[i].selectionWeight);
            int rool = random.Next(totalWeight);
            for (int i = 0; i < candidates.Count; i ++)
            {
                rool -= Mathf.Max(1, candidates[i].selectionWeight);
                if (rool < 0)
                    return candidates[i];
            }
            return candidates[candidates.Count - 1];
        }
    }
}
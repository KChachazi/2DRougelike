using Game.Core;
using Game.Debug;
using Game.Level;
using UnityEngine;

namespace Game.Debugging
{
    /// <summary>
    /// 开发期调试工具：在 Scene 视图预览纯数据地图，并批量验证 Seed。
    /// </summary>
    public sealed class LevelGraphDebugView : MonoBehaviour
    {
        [SerializeField] private LevelGenerationSettings settings;
        [SerializeField] private int previewSeed = 0x0d000721;
        [SerializeField, Min(1)] private int validationSeedCount = 100;
        [SerializeField] private float gizmoSpacing = 3f;
        [SerializeField] private bool generateOnStart = true;

        private LevelGraph graph;
        private void Start()
        {
            if (generateOnStart) GeneratePreview();
        }

        [ContextMenu("Generate Preview")]
        public void GeneratePreview()
        {
            if (LevelGraphGenerator.TryGenerate(settings, previewSeed, out graph, out string error))
                GameDebug.Log(DebugCategory.Level, $"地图预览成功：{graph.BuildSignature()}", this);
            else
                GameDebug.Error(DebugCategory.Level, error, this);
        }
        
        [ContextMenu("Validate Seed Range")]
        public void ValidateSeedRange()
        {
            for (int i = 0; i < validationSeedCount; i ++)
            {
                if (!LevelGraphGenerator.TryGenerate(settings, previewSeed + i, out LevelGraph first, out string error))
                {
                    GameDebug.Error(DebugCategory.Level, $"Seed={previewSeed + i}：{error}", this);
                    return;
                }
                if (!LevelGraphGenerator.TryGenerate(settings, previewSeed + i, out LevelGraph second, out error)
                    || first.BuildSignature() != second.BuildSignature())
                {
                    GameDebug.Error(DebugCategory.Level, $"Seed={previewSeed + i} 不能稳定复现。", this);
                    return;
                }
            }
            GameDebug.Log(DebugCategory.Level, $"连续 {validationSeedCount} 个 Seed 验证通过。", this);
        }

        private void OnDrawGizmos()
        {
            if (graph == null) return ;
            for (int i = 0; i < graph.Count; i ++)
            {
                RoomNode node = graph.Nodes[i];
                Vector3 position = new Vector3(
                    node.GridPosition.x * gizmoSpacing,
                    node.GridPosition.y * gizmoSpacing,
                    0f) + transform.position;
                Gizmos.color = GetColor(node.Type);
                Gizmos.DrawCube(position, Vector3.one);
                DrawConnection(node, RoomDirection.East, position);
                DrawConnection(node, RoomDirection.North, position);
            }
        }
        private Color GetColor(RoomType type)
        {
            switch (type)
            {
                case RoomType.Start: return Color.cyan;
                case RoomType.Elite: return Color.magenta;
                case RoomType.Treasure: return Color.yellow;
                case RoomType.Recovery: return Color.green;
                case RoomType.Boss: return Color.red;
                default: return Color.gray;
            }
        }
        private void DrawConnection(RoomNode node, RoomDirection direction, Vector3 from)
        {
            int neighborId = node.GetNeighborId(direction);
            if (neighborId < 0) return ;
            RoomNode neighbor = graph.GetRoomNode(neighborId);
            Vector3 to = new Vector3(
                neighbor.GridPosition.x * gizmoSpacing,
                neighbor.GridPosition.y * gizmoSpacing,
                0f) + transform.position;
            Gizmos.color = Color.white;
            Gizmos.DrawLine(from, to);
        }

    }
}
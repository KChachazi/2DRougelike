using UnityEngine;

namespace Game.Level
{
    /// <summary>集中敌人与房间内容的实例化入口，便于后续替换生成策略。</summary>
    public static class EnemyFactory
    {
        /// <summary>
        /// 在对应世界坐标生成预制体，并挂在指定对象下。
        /// </summary>
        /// <param name="prefab">即将生成的预制体。</param>
        /// <param name="worldPosition">预制体生成的世界坐标</param>
        /// <param name="parent">生成预制体的挂载对象</param>
        public static GameObject Create(GameObject prefab, Vector3 worldPosition, Transform parent)
        {
            if (prefab == null) return null;
            return Object.Instantiate(prefab, worldPosition, Quaternion.identity, parent);
        }
    }
}
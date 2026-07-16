using UnityEngine;

namespace Game.Level
{
    public static class EnemyFactory
    {
        public static GameObject Create(GameObject prefab, Vector3 worldPosition, Transform parent)
        {
            if (prefab == null) return null;
            return Object.Instantiate(prefab, worldPosition, Quaternion.identity, parent);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>允许池化对象接收所属对象池引用。</summary>
    public interface IPoolable
    {
        ObjectPool Pool { get; set; }
    }

    /// <summary>按需扩容、支持预热与重复归还诊断的 GameObject 对象池。</summary>
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int prewarmCount = 20;

        private readonly Queue<GameObject> pool = new Queue<GameObject>();

        [Tooltip("Debug: 检测")]
        [SerializeField] private bool debugMode = true;
        private readonly HashSet<GameObject> inPool = new HashSet<GameObject>();
        private int totalCreated;
        public int TotalCreated => totalCreated;
        public int InPoolCount => pool.Count;
        public int ActiveCount => totalCreated - pool.Count;
        private void Awake()
        {
            totalCreated = 0;
            for (int i = 0; i < prewarmCount; i ++)
            {
                GameObject instance = CreateInstance();
                instance.SetActive(false);
                pool.Enqueue(instance);
                if (debugMode) inPool.Add(instance);
            }
        }
        // ======================== 内部工具 ========================
        private GameObject CreateInstance()
        {
            GameObject instance = Instantiate(prefab, transform);
            if (instance.TryGetComponent(out IPoolable poolable))
            {
                poolable.Pool = this;
            }
            totalCreated ++;
            return instance;
        }
        // ======================== 对外接口 ========================
        /// <summary>
        /// 从对象池中获取并初始化一个对象。
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject instance = pool.Count > 0 ? pool.Dequeue() : CreateInstance();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            if (debugMode) inPool.Remove(instance);
            return instance;
        }
        /// <summary>
        /// 往对象池中释放并返还一个对象。
        /// </summary>
        public void Release(GameObject instance)
        {
            if (instance == null) return ;
            if (debugMode && !inPool.Add(instance))
            {
                Debug.LogError($"[ObjectPool] '{instance.name}' 被重复 Release!", instance);
                return ;
            }
            instance.SetActive(false);
            pool.Enqueue(instance);
        }
    } 
}
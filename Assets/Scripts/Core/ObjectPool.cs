using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

namespace Game.Core
{
    public interface IPoolable
    {
        ObjectPool Pool { get; set; }
    }

    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int prewarmCount = 20;

        private readonly Queue<GameObject> pool = new Queue<GameObject>();

        private void Awake()
        {
            for (int i = 0; i < prewarmCount; i ++)
            {
                GameObject instance = CreateInstance();
                instance.SetActive(false);
                pool.Enqueue(instance);
            }
        }

        private GameObject CreateInstance()
        {
            GameObject instance = Instantiate(prefab, transform);
            if (instance.TryGetComponent(out IPoolable poolable))
            {
                poolable.Pool = this;
            }
            return instance;
        }

        /* ---------------- 对外接口 ---------------- */
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject instance = pool.Count > 0 ? pool.Dequeue() : CreateInstance();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            return instance;
        }

        public void Release(GameObject instance)
        {
            instance.SetActive(false);
            pool.Enqueue(instance);
        }
    } 
}
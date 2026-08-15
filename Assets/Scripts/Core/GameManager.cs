using UnityEngine;

namespace Game.Core
{
    /// <summary>保存全局游戏对象引用的轻量场景单例。</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        [SerializeField] private GameObject player;
        public GameObject Player => player; // 只读访问器
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return ;
            }
            Instance = this;
        }
    }
}
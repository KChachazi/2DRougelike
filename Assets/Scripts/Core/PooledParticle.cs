using UnityEngine;

namespace Game.Core
{
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledParticle : MonoBehaviour, IPoolable
    {
        private ParticleSystem ps;
        private float lifeTime;
        private float timer;

        public ObjectPool Pool { get; set; }

        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            lifeTime = main.duration + main.startLifetime.constantMax;
        }
        private void OnEnable()
        {
            timer = 0f;
            ps.Clear(true);
            ps.Play(true);
        }
        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < lifeTime) return ;
            if (Pool != null) Pool.Release(gameObject);
            else Destroy(gameObject);
        }
    }
}
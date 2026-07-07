using UnityEngine;
namespace Game.Core
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset;

        private void LateUpdate()
        {
            if (target == null) return ;
            Vector3 desired = target.position - (Vector3)offset;
            transform.position = new Vector3(desired.x, desired.y, transform.position.z);
        }
    }
}
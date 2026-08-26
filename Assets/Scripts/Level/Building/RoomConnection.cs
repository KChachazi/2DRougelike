using Game.Core;
using UnityEngine;

namespace Game.Level
{
    public sealed class RoomConnection : MonoBehaviour
    {
        [SerializeField] private RoomDirection direction;
        [Tooltip("没有邻居时覆盖门洞的墙体")]
        [SerializeField] private GameObject closedWall;
        [SerializeField] private Door gate;
        [SerializeField] private Transform corridorAnchor;

        public RoomDirection Direction => direction;
        public Door Gate => gate;
        public Transform CorridorAnchor => corridorAnchor != null ? corridorAnchor : transform;
        public bool IsConnected { get; private set; }

        public void Configure(bool connected)
        {
            IsConnected = connected;
            if (closedWall != null) closedWall.SetActive(!connected);
            if (gate != null)
            {
                gate.gameObject.SetActive(connected);
                if (connected) gate.Unlock();
            }
        }
    }
}
using UnityEngine;

namespace Game.Level
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class RoomEncounterTrigger : MonoBehaviour
    {
        [SerializeField] private Room room;
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player")) room.HandlePlayerEntered();
        }
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player")) room.HandlePlayerExited();
        }
    }
}
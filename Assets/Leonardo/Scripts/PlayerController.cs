using UnityEngine;

namespace Leonardo.Scripts
{
    /// <summary>
    /// This is a placeholder to try the NetworkManager, this just saves the position of the "player" gameobject as if it were an actual Player Controller.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public PlayerData playerData;

        public void UpdatePlayerPosition(Vector3 newPosition, Quaternion newRotation)
        {
            transform.position = newPosition;
            transform.rotation = newRotation;
        }
    }
}

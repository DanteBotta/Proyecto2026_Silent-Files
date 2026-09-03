using UnityEngine;

public class CanvasFollowCamera : MonoBehaviour
{
    // The camera that the canvas will follow
    [SerializeField] private Transform camara;

    void LateUpdate() // The camera moves on Update(), so we need to move the canvas on LateUpdate() to avoid jittering
    {
        // Make the canvas follow the camera's rotation, so moves only when press Q / E
        transform.rotation = camara.rotation;
    }
}

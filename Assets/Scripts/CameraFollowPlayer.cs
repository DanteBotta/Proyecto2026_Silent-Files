using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    // The camera that the canvas will follow
    [SerializeField] private Transform player;

    public int AlturaExtra = 10; // Altura extra para que la cámara esté por encima del jugador

    private void Start()
    {
        player = FindAnyObjectByType<PlayerController>().gameObject.transform;
    }
    void LateUpdate()
    {
        // Make the camara follow the player's position
        transform.position = player.position + Vector3.up * AlturaExtra;
    }
}

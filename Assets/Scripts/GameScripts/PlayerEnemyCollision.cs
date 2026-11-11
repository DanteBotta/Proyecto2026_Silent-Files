using UnityEngine;

// Se coloca en el jugador, junto al CharacterController.
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class PlayerEnemyCollision : MonoBehaviour
{
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Permite reconocer al enemigo aunque el collider esté en un objeto hijo.
        if (hit.collider.GetComponentInParent<AI>() != null)
            AlColisionarConEnemigo();
    }

    private void AlColisionarConEnemigo()
    {
        Debug.Log("67");
        Time.timeScale = 0f; // Detiene el tiempo del juego.
    }
}

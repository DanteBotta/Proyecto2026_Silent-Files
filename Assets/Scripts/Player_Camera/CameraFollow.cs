using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // el jugador

    [Header("Offset")]
    public Vector3 offset = new Vector3(0f, 12f, -8f);
    // Y = altura, Z negativo = detrás y arriba
    // Este valor da el ángulo tipo Minecraft Dungeons
    // Lo podés tunear en el inspector en tiempo real

    [Header("Suavizado")]
    public float smoothSpeed = 8f;
    // Más alto = más pegada al jugador, menos = más suave/flotante

    void LateUpdate()
    // LateUpdate porque el jugador se mueve en Update,
    // la cámara actualiza después para evitar jitter
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        // La cámara siempre mira al jugador
        transform.LookAt(target.position);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla una cámara top-down con ángulo diagonal que sigue al jugador.
/// Permite rotar la cámara alrededor del jugador con Q y E (en incrementos, no libre).
/// Todos los valores clave son públicos para poder ajustarlos desde el Inspector
/// sin tocar código.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Referencia")]
    [Tooltip("Transform del jugador al que la cámara va a seguir.")]
    public Transform target;
 
    [Header("Posición de la cámara respecto al jugador")]
    [Tooltip("Distancia horizontal de la cámara respecto al jugador.")]
    public float distance = 8f;
 
    [Tooltip("Altura de la cámara respecto al jugador.")]
    public float height = 10f;
 
    [Tooltip("Ángulo de inclinación de la cámara mirando hacia abajo (en grados). 90 = totalmente cenital, valores menores = más diagonal.")]
    [Range(20f, 90f)]
    public float tiltAngle = 55f;
 
    [Header("Suavizado de seguimiento")]
    [Tooltip("Qué tan rápido la cámara sigue al jugador al moverse. Valores más altos = sigue más rápido/rígido.")]
    public float followSmoothness = 8f;
 
    [Header("Rotación con Q y E")]
    [Tooltip("Ángulo que rota la cámara cada vez que se presiona Q o E (en grados).")]
    public float rotationStep = 45f;
 
    [Tooltip("Tiempo aproximado (en segundos) que tarda la cámara en completar la rotación. Más bajo = rotación más rápida/snap, más alto = más suave.")]
    public float rotationSmoothTime = 0.15f;
 
    [Tooltip("Tecla para rotar la cámara hacia la izquierda.")]
    public KeyCode rotateLeftKey = KeyCode.Q;
 
    [Tooltip("Tecla para rotar la cámara hacia la derecha.")]
    public KeyCode rotateRightKey = KeyCode.E;
 
    // --- Estado interno ---
    private float currentYaw;           // Ángulo actual alrededor del jugador (eje Y)
    private float targetYaw;            // Ángulo al que se está interpolando
    private float yawVelocity;          // Usado internamente por SmoothDampAngle
 
    private void Start()
    {
        currentYaw = transform.eulerAngles.y;
        targetYaw = currentYaw;
    }
 
    private void Update()
    {
        HandleRotationInput();
    }
 
    private void LateUpdate()
    {
        if (target == null) return;
 
        UpdateCameraPosition();
    }
 
    private void HandleRotationInput()
    {
        if (Input.GetKeyDown(rotateLeftKey))
        {
            targetYaw -= rotationStep;
        }
        else if (Input.GetKeyDown(rotateRightKey))
        {
            targetYaw += rotationStep;
        }
 
        // SmoothDampAngle da una rotación más estable y consistente entre
        // distintos framerates que un Lerp simple, y maneja bien el "wrap"
        // de 360° a 0° sin saltos raros.
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
    }
 
    private void UpdateCameraPosition()
    {
        // Calcular la posición deseada de la cámara alrededor del jugador,
        // según el ángulo actual (currentYaw) y la inclinación (tiltAngle)
        Quaternion rotation = Quaternion.Euler(tiltAngle, currentYaw, 0f);
        Vector3 desiredPosition = target.position - (rotation * Vector3.forward * distance);
        desiredPosition.y = target.position.y + height;
 
        // Mover la cámara suavemente hacia la posición deseada (sigue al jugador)
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmoothness * Time.deltaTime);
 
        // Que la cámara siempre mire hacia el jugador
        transform.rotation = rotation;
    }
}
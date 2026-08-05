using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla el movimiento del jugador: caminar normal (WASD), correr (Shift izquierdo)
/// y caminar sigiloso/agachado (Ctrl izquierdo). El movimiento es relativo a la cámara
/// (si la cámara rota con Q/E, el jugador se sigue moviendo "hacia adelante de cámara"
/// al apretar W, etc).
/// Requiere un CharacterController en el mismo GameObject.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform de la cámara. Si se deja vacío, busca la Main Camera automáticamente.")]
    public Transform cameraTransform;
 
    [Header("Velocidades de movimiento")]
    [Tooltip("Velocidad de caminata normal (unidades por segundo).")]
    public float walkSpeed = 4f;
 
    [Tooltip("Velocidad al correr (Shift izquierdo). Debe ser mayor a walkSpeed.")]
    public float runSpeed = 7f;
 
    [Tooltip("Velocidad al caminar sigiloso (Ctrl izquierdo). Debe ser menor a walkSpeed.")]
    public float crouchSpeed = 2f;
 
    [Header("Ruido generado (para el futuro sistema de detección de guardias)")]
    [Tooltip("Nivel de ruido al correr. Usar como radio en metros para que un guardia lo escuche.")]
    public float runNoiseRadius = 8f;
 
    [Tooltip("Nivel de ruido al caminar normal.")]
    public float walkNoiseRadius = 3f;
 
    [Tooltip("Nivel de ruido al caminar sigiloso/agachado. Debería ser 0 o casi 0.")]
    public float crouchNoiseRadius = 0f;
 
    [Header("Peso e inventario (afecta velocidad)")]
    [Tooltip("Multiplicador de velocidad actual según el peso cargado. 1 = sin penalización, 0.5 = mitad de velocidad, etc. Lo debería actualizar el sistema de inventario.")]
    [Range(0.1f, 1f)]
    public float weightSpeedMultiplier = 1f;
 
    [Header("Gravedad")]
    [Tooltip("Fuerza de gravedad aplicada al personaje.")]
    public float gravity = -9.81f;
 
    [Header("Rotación del personaje")]
    [Tooltip("Qué tan rápido gira el personaje para mirar hacia la dirección de movimiento (grados por segundo aprox, usado con Slerp).")]
    public float rotationSpeed = 12f;
 
    // --- Estado interno ---
    private CharacterController controller;
    private Vector3 velocity;
    private MovementState currentState = MovementState.Walking;
 
    // Se expone de solo lectura para que otros scripts (ej. sistema de detección) sepan
    // qué tan rápido/ruidoso está siendo el jugador en este momento.
    public MovementState CurrentState => currentState;
    public float CurrentNoiseRadius { get; private set; }
    public float CurrentSpeed { get; private set; }
 
    public enum MovementState
    {
        Crouching,
        Walking,
        Running
    }
 
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
 
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }
 
    private void Update()
    {
        HandleMovement();
        ApplyGravity();
    }
 
    private void HandleMovement()
    {
        // Input crudo de WASD (o flechas)
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D
        float vertical = Input.GetAxisRaw("Vertical");     // W/S
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;
 
        // Determinar el estado de movimiento según las teclas modificadoras
        currentState = DetermineMovementState(inputDir);
 
        // Elegir velocidad y ruido según el estado actual
        float targetSpeed = GetSpeedForState(currentState);
        CurrentNoiseRadius = GetNoiseForState(currentState);
 
        // Aplicar penalización de peso (siempre, sin importar el estado)
        targetSpeed *= weightSpeedMultiplier;
        CurrentSpeed = targetSpeed;
 
        if (inputDir.magnitude >= 0.1f && cameraTransform != null)
        {
            // Convertir el input a dirección relativa a la cámara (ignorando su inclinación vertical)
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();
 
            Vector3 moveDirection = (camForward * inputDir.z + camRight * inputDir.x).normalized;
 
            // Mover al personaje
            controller.Move(moveDirection * targetSpeed * Time.deltaTime);
 
            // Rotar al personaje suavemente hacia la dirección de movimiento
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
 
    private MovementState DetermineMovementState(Vector3 inputDir)
    {
        bool isMoving = inputDir.magnitude >= 0.1f;
 
        if (Input.GetKey(KeyCode.LeftShift) && isMoving)
        {
            return MovementState.Running;
        }
 
        if (Input.GetKey(KeyCode.LeftControl))
        {
            return MovementState.Crouching;
        }
 
        return MovementState.Walking;
    }
 
    private float GetSpeedForState(MovementState state)
    {
        switch (state)
        {
            case MovementState.Running:
                return runSpeed;
            case MovementState.Crouching:
                return crouchSpeed;
            case MovementState.Walking:
            default:
                return walkSpeed;
        }
    }
 
    private float GetNoiseForState(MovementState state)
    {
        switch (state)
        {
            case MovementState.Running:
                return runNoiseRadius;
            case MovementState.Crouching:
                return crouchNoiseRadius;
            case MovementState.Walking:
            default:
                return walkNoiseRadius;
        }
    }
 
    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Pequeño valor negativo para mantenerlo pegado al piso
        }
 
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
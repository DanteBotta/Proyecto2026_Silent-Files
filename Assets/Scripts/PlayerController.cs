using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Controla el movimiento del jugador
// caminar normal (WASD), correr (Shift izq.) y caminar sigiloso/agachado (Ctrl izq.).
// Usa el character controller para funcionar, si no no anda
public class PlayerController : MonoBehaviour
{
    //Cámara que va a seguir al personaje, si no esta asignada, no anda
    [Header("Cámara Asignada")]
    public Transform cameraTransform;

    [Header("Velocidades de movimiento")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f; 
    public float crouchSpeed = 2f;
 
    [Header("Ruido generado")]
    public float walkNoiseRadius = 3f;
    public float runNoiseRadius = 8f;
    public float crouchNoiseRadius = 0f;

    //Multiplicador de Velocidad, al tener cosas disminuye y vas más lento
    [Header("Multiplicador de Peso e inventario")]
    [Range(0.1f, 1f)]
    public float weightSpeedMultiplier = 1f;
 
    //Gravedad sobre el personaje (la de la Tierra)
    [Header("Gravedad")]
    public float gravity = -9.81f;
 
    //Velocidad que rota el personaje hacie la dirección del moviento
    [Header("Rotación del personaje")]
    public float rotationSpeed = 12f;

    [Header("Minijuego de ganzúa")]
    public LockpickMinigameController lockpickManager;

    // --- Estado interno ---
    private CharacterController controller; //Crea una variable para gurdar el CharacterController como variable
    private Vector3 velocity; //Guarda velocidad vertical (gravedad, no hay salto)
    private MovementState currentState = MovementState.Walking; //Empieza en caminando
 
    // Varialbes para que otros scripts puedan leer, pero no modificar
    // qué tan rápido/ruidoso está siendo el jugador en este momento.
    public MovementState CurrentState => currentState; //Muestra en que estado del MovementState se encuentra
    public float CurrentNoiseRadius { get; private set; } //Guarda el sonido actual
    public float CurrentSpeed { get; private set; } //Guarda la velocidad actual
 
    //Crea el dato del estado que se encuentra el jugador
    public enum MovementState{
        Crouching,
        Walking,
        Running
    }
    
    //Se ejecuta apenas empieza el codigo, antes que todo el resto
    private void Awake()
    {
        //Asigan el CharacterController a una variable
        controller = GetComponent<CharacterController>();

        //Como no tira error si no hay cámara, aviso que hay que asignar una
        if (cameraTransform == null)
        {
            Debug.Log("No hay cámara asignada, sin cámara no funciona");
            Debug.Log("Debe asignar una cámara para su funcionamiento");
        }
    }
 
    //Se repite cada frame
    private void Update()
    {
        if (lockpickManager == null || !lockpickManager.MinijuegoEnCurso)
        {
            HandleMovement();
        }

        ApplyGravity();
    }
 
    private void HandleMovement()
    {
        // Input de WASD o flechas
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        //Crea un Vector para mover al personaje dependiendo de los input, en horizontal y vertical
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized; //.normalized hace que ir diagonal no sea mas rápido
 
        // Determinar el estado de movimiento según las teclas modificadoras
        currentState = DetermineMovementState(inputDir);
 
        float targetSpeed = GetSpeedForState(currentState); //Determina la velocidad dependiendo del estado
        CurrentNoiseRadius = GetNoiseForState(currentState); //Determina el sonido generado segun el estado
 
        // Aplicar penalización de peso (siempre, sin importar el estado)
        targetSpeed *= weightSpeedMultiplier;
        CurrentSpeed = targetSpeed; //Determina la velocidad final
 
        // Se encarga de mover y rotar al personaje
        // Convierte las teclas que presiona el jugador en una dirección basada en la orientación de la cámara
        // Hace que el personaje gire hacia donde se está desplazando.
        if (inputDir.magnitude >= 0.1f && cameraTransform != null) //Verifica que se este moviendo y si hay una cámara asginada
        {
            //Determina la dirección dependiendo de la dirección de la cámara, define que es adelante y que son los lados
            Vector3 camForward = cameraTransform.forward; 
            Vector3 camRight = cameraTransform.right;
            //En caso de que la cámara esta inclinada, lo ignora (no se mueve hacia abajo/arriba)
            camForward.y = 0f; 
            camRight.y = 0f;
            //Evita que al moverse en diagonal puedas ser más rápido
            camForward.Normalize();
            camRight.Normalize();
 
            //Mueve el personaje en la dirección de la cámara, en la dirección y velocidad
            Vector3 moveDirection = (camForward * inputDir.z + camRight * inputDir.x).normalized;
            controller.Move(moveDirection * targetSpeed * Time.deltaTime);
 
            // Crea una rotación para que el personaje mire hacia la dirección del movimiento
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            //Mide la rotación actual (transform.rotation), hasta donde debe rotar (targetRotation), y a que velocidad rota (rotationSpeed)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
 
    //Función que se encarga de determinar el estado de velocidad que esta
    //Usa el dato MovementState para devolver directamente el estado
    private MovementState DetermineMovementState(Vector3 inputDir)
    {
        //Verifica si se mueve, velocidad mayor a 0.1
        bool isMoving = inputDir.magnitude >= 0.1f;

        //Si esta corriendo 
        if (Input.GetKey(KeyCode.LeftShift) && isMoving){
            return MovementState.Running;
        }
 
        //Si esta agachado/silencioso
        if (Input.GetKey(KeyCode.LeftControl)){
            return MovementState.Crouching;
        }
        //Si no, esta caminando normal
        return MovementState.Walking;
    }
 
    //Se encarga de determinar la velocidad dependiendo del estado
    private float GetSpeedForState(MovementState state)
    {
        //Revisa el estado para determinar a que velocidad se mueve, devuelve la variable de velocidad de cada estado
        switch (state)
        {
            case MovementState.Running: //Si corre
                return runSpeed;
            case MovementState.Crouching: //Si esta agachado
                return crouchSpeed;
            case MovementState.Walking: //Si camina, es el default
            default:
                return walkSpeed;
        }
    }
 
    //Se encarga de determinar el sonido generado dependiendo del estado
    private float GetNoiseForState(MovementState state)
    {
        //Revisa el estado para determinar el sonido generado, devuelve la variable de sonido de cada estado
        switch (state)
        {
            case MovementState.Running: //Si corre
                return runNoiseRadius;
            case MovementState.Crouching: //Si esta agachado
                return crouchNoiseRadius;
            case MovementState.Walking: //Si camina, es el default
            default:
                return walkNoiseRadius;
        }
    }
    
    //Se encarga de determinar la velocidad vertical (de caida) en caso de estar cayendo
    //Al no usar RigidBody se debe hacer manualmente
    private void ApplyGravity()
    {
        //Reinicia la velocidad de caida cada vez que toca el piso, para evitar que se sume cada vez que caes
        if (controller.isGrounded && velocity.y < 0) 
        {
            velocity.y = -2f; // Pequeño valor negativo para mantenerlo pegado al piso
        }
 
        velocity.y += gravity * Time.deltaTime; //Acelera gradualmente, según gravedad
        controller.Move(velocity * Time.deltaTime); //Mueve al personaje
    }
}
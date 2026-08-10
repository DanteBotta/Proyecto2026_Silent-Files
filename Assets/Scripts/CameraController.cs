using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Controla la cámara top-down con un ángulo diagonal que sigue al jugador
/// Permite rotar la cámara alrededor del jugador con Q y E
public class CameraController : MonoBehaviour
{
    [Header("Objeto que sigue")]
    public Transform target;
 
    [Header("Configuración de la Cámara (respecto al jugador)")]
    public float distance = 8f; //Distancia
    public float height = 10f; //Altura
    [Range(0f, 90f)]
    public float tiltAngle = 55f; //Angulo de Inclinación
 
    [Header("Suavizado de seguimiento")]
    public float followSmoothness = 8f; //Rapidez de la cámara al seguir al jugador
 
    [Header("Rotación con Q y E")]
    public float rotationStep = 45f; //Cuanto rota la cámara (en grados)
    public float rotationSmoothTime = 0.15f; //Velocidad de rotación, cuanto tarda en rotar (segundos)
    public KeyCode rotateLeftKey = KeyCode.Q; //Tecla para rotar hacia la izquierda
    public KeyCode rotateRightKey = KeyCode.E; //Tecla para rotar hacia la derecha
 
    // --- Estado interno ---
    private float currentYaw; // Ángulo actual que esta la cámara
    private float targetYaw; // Ángulo al que debe rotar, luego de presionar Q o E
    private float yawVelocity; // Ayuda matemática para suavizar el giro
 
    //Se ejecuta una vez al iniciar el juego
    private void Start()
    {
        //Define la rotación de la cámara actual
        currentYaw = transform.eulerAngles.y;
        targetYaw = currentYaw; //El objetivo inicial sea el mismo, para que la cámara no rote apenas comienza
    }
 
    //Se repite cada frame
    private void Update()
    {
        HandleRotationInput(); //Revisa si se presiono Q o E
    }
 
    //Se ejecuta todos los frames, pero luego del Update
    //Se usa para que primero se mueva el jugador y luego la cámara, si la cámara se moviera en Update(), podría seguir la posición anterior del jugador
    private void LateUpdate()
    {
        //Verifica que trenga algo que seguir, sino no se mueve
        if (target == null) return;
        UpdateCameraPosition();
    }
 
    //Detecta cuando se debe rotar la cámara (Q o E)
    private void HandleRotationInput()
    {
        //Asigna el objetivo de rotación dependiendo de la tecla
        if (Input.GetKeyDown(rotateLeftKey)) //Si debe rotar a la izquierda
        {
            targetYaw -= rotationStep;
        }
        else if (Input.GetKeyDown(rotateRightKey)) //Si debe rotar a la derecha
        {
            targetYaw += rotationStep;
        }
 
        // SmoothDampAngle da una rotación más estable y consistente entre
        // distintos framerates que un Lerp simple, y maneja bien el "wrap"
        // de 360° a 0° sin saltos raros.
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
    }
 
    //Calcula dónde debe colocarse la cámara
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
using UnityEngine;
using System.Collections.Generic;

public class ShowCanvasNearPlayer : MonoBehaviour
{
    [Header("Configuración")]
    public Transform player; // The object from which the distance is calculated
    public float distanciaActivacion = 5f; // The distance at which the canvas will activate
    public float distanciaFuncion = 2f; // The distance at which the canvas will function (not used in this script)
    public bool FuncionCanvas = false; // A boolean to control the canvas functionality (not used in this script)

    [Header("Debug Distancia")]
    public bool MostrarDebug = false;

    private Canvas canvas; // Reference to the Canvas component

    private float distancia;

    // Lista de todos los Canvas que tienen este script.
    private static readonly List<ShowCanvasNearPlayer> canvasActivos =
        new List<ShowCanvasNearPlayer>();

    // Canvas que actualmente tiene prioridad.
    private static ShowCanvasNearPlayer canvasSeleccionado;

    // Evita realizar la selección más de una vez por frame.
    private static int ultimoFrameEvaluado = -1;

    void Start()
    {
        if (player == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");

            if (jugador != null)
                player = jugador.transform;
        }

        // Get the Canvas component attached to this GameObject
        canvas = GetComponent<Canvas>();

        // Start with the canvas disabled
        if (canvas != null)
            canvas.enabled = false;
    }

    void OnEnable()
    {
        if (!canvasActivos.Contains(this))
            canvasActivos.Add(this);
    }

    void OnDisable()
    {
        canvasActivos.Remove(this);

        if (canvasSeleccionado == this)
            canvasSeleccionado = null;

        if (canvas != null)
            canvas.enabled = false;
    }

    void Update()
    {
        if (player == null || canvas == null)
            return;

        // Vector3.Distance calculates the distance between two points in 3D space
        distancia = Vector3.Distance(player.position, transform.position);

        if (MostrarDebug)
        {
            Debug.Log(distancia);
        }

        // Solo funciona el Canvas seleccionado y dentro de la distancia funcional.
        FuncionCanvas = canvasSeleccionado == this &&
                        distancia <= distanciaFuncion;
    }

    void LateUpdate()
    {
        // Todos los Canvas se evalúan una sola vez por frame.
        if (ultimoFrameEvaluado != Time.frameCount)
        {
            ultimoFrameEvaluado = Time.frameCount;
            SeleccionarCanvasMasCercano();
        }

        // Activa/desactiva el Canvas según la distancia
        canvas.enabled = canvasSeleccionado == this;

        // Solo el Canvas visible puede funcionar.
        FuncionCanvas = canvasSeleccionado == this &&
                        distancia <= distanciaFuncion;
    }

    private static void SeleccionarCanvasMasCercano()
    {
        canvasSeleccionado = null;
        float menorDistancia = float.MaxValue;

        foreach (ShowCanvasNearPlayer candidato in canvasActivos)
        {
            if (candidato == null ||
                !candidato.isActiveAndEnabled ||
                candidato.canvas == null ||
                candidato.player == null)
            {
                continue;
            }

            float distanciaActual = Vector3.Distance(
                candidato.player.position,
                candidato.transform.position
            );

            if (candidato.MostrarDebug)
                Debug.Log($"{candidato.name}: distancia = {distanciaActual}");

            // Solo se consideran los Canvas dentro del rango de activación.
            if (distanciaActual > candidato.distanciaActivacion)
                continue;

            // Gana el más cercano. En empate, gana el InstanceID menor.
            if (distanciaActual < menorDistancia ||
                (Mathf.Approximately(distanciaActual, menorDistancia) &&
                 (canvasSeleccionado == null ||
                  candidato.GetEntityId() < canvasSeleccionado.GetEntityId())))
            {
                menorDistancia = distanciaActual;
                canvasSeleccionado = candidato;
            }
        }
    }
}
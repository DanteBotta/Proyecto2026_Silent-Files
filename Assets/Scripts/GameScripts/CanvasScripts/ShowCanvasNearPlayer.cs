using UnityEngine;
using System.Collections.Generic;

public class ShowCanvasNearPlayer : MonoBehaviour
{
    [Header("Configuración")]
    public Transform player; // The object from which the distance is calculated
    public float distanciaActivacion = 5f; // The distance at which the canvas will activate
    public float distanciaFuncion = 2f; // The distance at which the canvas will function (not used in this script)
    public bool FuncionCanvas = false; // A boolean to control the canvas functionality (not used in this script)

    [Header("Paredes (colliders)")]
    [Tooltip("Si está activado, el Canvas no aparece (ni funciona) cuando hay un collider entre el jugador y el Canvas.")]
    public bool bloquearConParedes = true;

    [Tooltip("Capas que cuentan como pared. Poné solo las capas de paredes/obstáculos para evitar que otros objetos tapen el Canvas.")]
    public LayerMask capasObstaculo = ~0;

    [Tooltip("Altura sobre la posición del jugador desde donde sale la línea de visión (útil si el pivote del jugador está en los pies).")]
    public float alturaRayoJugador = 1f;

    [Tooltip("La línea se corta esta distancia antes del Canvas, así no choca con la pared o superficie donde está colocado.")]
    public float margenDestino = 0.1f;

    [Tooltip("Los colliders que contienen o tocan al Canvas (por ejemplo la pared o el marco de la puerta donde está metido) no cuentan como obstáculo. Radio de detección en metros.")]
    public float radioParedPropia = 0.1f;

    [Header("Lado del Canvas (opcional)")]
    [Tooltip("Como el Canvas está metido en la pared, también se vería desde el otro lado. Si lo activás, solo aparece cuando el jugador está del lado desde donde se lee el Canvas.")]
    public bool soloDesdeElFrente = false;

    [Tooltip("Si 'soloDesdeElFrente' está activado y aparece desde el lado equivocado, marcá esto para invertirlo.")]
    public bool invertirFrente = false;

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

    // Buffer reutilizable para el raycast (evita generar basura cada frame).
    private static readonly RaycastHit[] hitsBuffer = new RaycastHit[16];
    private static readonly Collider[] paredPropiaBuffer = new Collider[8];

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

            // Opcional: solo se muestra desde el lado desde donde se lee el Canvas.
            if (!EstaDelLadoCorrecto(candidato))
                continue;

            // Si hay una pared (collider) en medio, este Canvas no se muestra.
            if (HayObstaculo(candidato))
            {
                if (candidato.MostrarDebug)
                    Debug.Log($"{candidato.name}: bloqueado por una pared.");
                continue;
            }

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

    /// <summary>
    /// Línea de visión entre el jugador y el Canvas. Devuelve true si hay un collider en el medio.
    /// Ignora los triggers y los colliders del propio jugador y del propio Canvas (y sus hijos).
    /// </summary>
    private static bool HayObstaculo(ShowCanvasNearPlayer candidato)
    {
        if (!candidato.bloquearConParedes)
            return false;

        Vector3 origen = candidato.player.position + Vector3.up * candidato.alturaRayoJugador;
        Vector3 haciaCanvas = candidato.transform.position - origen;
        float largoTotal = haciaCanvas.magnitude;
        float largoRayo = largoTotal - candidato.margenDestino;

        // El jugador está pegado al Canvas: nada puede interponerse.
        if (largoRayo <= 0f)
            return false;

        // Colliders que contienen al Canvas (la pared o el marco donde está metido): no cuentan como obstáculo.
        int cantidadPropios = Physics.OverlapSphereNonAlloc(
            candidato.transform.position,
            candidato.radioParedPropia,
            paredPropiaBuffer,
            candidato.capasObstaculo,
            QueryTriggerInteraction.Ignore
        );

        int cantidad = Physics.RaycastNonAlloc(
            origen,
            haciaCanvas / largoTotal,
            hitsBuffer,
            largoRayo,
            candidato.capasObstaculo,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < cantidad; i++)
        {
            Collider collider = hitsBuffer[i].collider;
            Transform golpeado = collider.transform;

            if (golpeado.IsChildOf(candidato.player)) continue;      // el propio jugador
            if (golpeado.IsChildOf(candidato.transform)) continue;   // el propio Canvas
            if (EsParedPropia(collider, cantidadPropios)) continue;  // la pared donde está metido el Canvas

            return true; // cualquier otro collider es una pared
        }

        return false;
    }

    private static bool EsParedPropia(Collider collider, int cantidadPropios)
    {
        for (int j = 0; j < cantidadPropios; j++)
            if (paredPropiaBuffer[j] == collider)
                return true;

        return false;
    }

    /// <summary>
    /// Si 'soloDesdeElFrente' está activado, el Canvas solo se muestra cuando el jugador está del lado
    /// desde donde se lee (un Canvas de Unity se lee desde el lado opuesto a su eje azul/forward).
    /// </summary>
    private static bool EstaDelLadoCorrecto(ShowCanvasNearPlayer candidato)
    {
        if (!candidato.soloDesdeElFrente)
            return true;

        Vector3 haciaJugador = candidato.player.position - candidato.transform.position;
        float lado = Vector3.Dot(candidato.transform.forward, haciaJugador);

        return candidato.invertirFrente ? lado > 0f : lado < 0f;
    }
}
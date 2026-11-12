using System;
using System.Collections;
using UnityEngine;
using TMPro;
using GamePlay;

public partial class Door : MonoBehaviour
{
    public InventoryManager InventoryManager;
    public LockpickMinigameController LockpickMinigameController;
    public ShowCanvasNearPlayer ShowCanvasNearPlayer;

    [Header("Ruido")]
    [SerializeField, Min(0f)] private float radioRuidoApertura = 5f;
    [Tooltip("Punto de interacción cerca del suelo. Sin asignar, usa la posición del jugador al abrir la puerta.")]
    [SerializeField] private Transform origenRuido;

    [Header("Rotación")]
    public Transform Bisagra;
    public float AnguloApertura = 90f;
    public float VelocidadRotacion = 3f;
    [Tooltip("Cuántas veces más rápido se abre la puerta cuando se fuerza con la palanca.")]
    [SerializeField, Min(1f)] private float multiplicadorVelocidadPalanca = 2f;

    [Header("Texto de estado")]
    [Tooltip("Texto que muestra el estado de la puerta. Sin asignar, usa el único texto hijo directo del Canvas hermano de la bisagra.")]
    public TMP_Text TextoEstado;

    private Quaternion RotacionCerrada;
    private Quaternion RotacionAbierta;
    private Quaternion RotacionObjetivo;
    private bool aperturaRapida;
    private EstadoPuerta? estadoMostrado;

    [Header("Debug")]
    public bool MostrarDebugs = false;

    private Rigidbody rbBisagra;
    private Collider colliderPuerta;
    private bool colisionActiva = true;

    public enum EstadoPuerta
    {
        Abierta,
        Cerrada,
        Trabada,
        Bloqueada
    }
    public EstadoPuerta estadoActual;

    // Door passage state lives in Door.Passage.cs.

    void Start()
    {
        // Buscar el Canvas desde el objeto raíz de la puerta
        Transform raizPuerta = transform.parent.parent;

        // Obtener la bisagra (padre del objeto que tiene Door.cs)
        Bisagra = transform.parent;

        // Guardar rotaciones inicial y abierta
        RotacionCerrada = Bisagra.localRotation;

        RotacionAbierta = RotacionCerrada *
            Quaternion.Euler(0, AnguloApertura, 0);

        RotacionObjetivo = RotacionCerrada;

        if (InventoryManager == null)
            InventoryManager = FindAnyObjectByType<InventoryManager>();
        if (LockpickMinigameController == null)
            LockpickMinigameController = FindAnyObjectByType<LockpickMinigameController>();
        if (ShowCanvasNearPlayer == null)
            ShowCanvasNearPlayer = raizPuerta.GetComponentInChildren<ShowCanvasNearPlayer>(true);
        if (TextoEstado == null)
            TextoEstado = BuscarTextoEstado(raizPuerta);

        ActualizarTextoEstado();

        rbBisagra = Bisagra.GetComponent<Rigidbody>();
        rbBisagra.isKinematic = true;
        rbBisagra.interpolation = RigidbodyInterpolation.Interpolate;
        colliderPuerta = GetComponent<Collider>();
    }

    // El Canvas es hijo de la raíz de la puerta (hermano de la bisagra) y el texto es hijo directo del Canvas.
    private TMP_Text BuscarTextoEstado(Transform raizPuerta)
    {
        foreach (Transform hermano in raizPuerta)
        {
            if (hermano == Bisagra || hermano.GetComponent<Canvas>() == null)
                continue;

            foreach (Transform hijo in hermano)
            {
                TMP_Text texto = hijo.GetComponent<TMP_Text>();
                if (texto != null)
                    return texto;
            }
        }
        return null;
    }

    // Sólo reescribe el texto cuando el estado cambió (se llama desde LateUpdate en Door.Passage.cs).
    private void ActualizarTextoEstado()
    {
        if (TextoEstado == null || (estadoMostrado.HasValue && estadoMostrado.Value == estadoActual))
            return;

        estadoMostrado = estadoActual;
        TextoEstado.text = estadoActual switch
        {
            EstadoPuerta.Abierta => "ABIERTA",
            EstadoPuerta.Cerrada => "CERRADA",
            EstadoPuerta.Trabada => "TRABADA",
            EstadoPuerta.Bloqueada => "BLOQUEADA",
            _ => string.Empty
        };
    }

    void Update()
    {
        if (guardasEnPaso.Count > 0 || fase == FaseMovimiento.Abriendo || fase == FaseMovimiento.Cerrando)
            return;

        if (ShowCanvasNearPlayer != null && InventoryManager != null &&
            ShowCanvasNearPlayer.FuncionCanvas && Input.GetKeyDown(KeyCode.F))
        {
            InventoryManager.Herramienta herramientaActual = InventoryManager.ObjetoEnMano; // Obtener la herramienta actual del jugador

            if (estadoActual != EstadoPuerta.Bloqueada) // Si la puerta no esta bloqueada, se puede interactuar con ella
            {
                if ((herramientaActual == InventoryManager.Herramienta.Mano)) // Se interactua con la mano
                {
                    if (estadoActual == EstadoPuerta.Cerrada) // Puerta Cerrada
                    {
                        DebugPuerta("Abriste puerta con la mano");
                        aperturaRapida = false;
                        CambiarEstado(EstadoPuerta.Abierta);
                        RotacionObjetivo = RotacionAbierta;
                        ActivarColision(false);

                        Vector3 posicionRuido = origenRuido != null
                            ? origenRuido.position
                            : ShowCanvasNearPlayer.player != null
                                ? ShowCanvasNearPlayer.player.position
                                : transform.position;
                        Sounds.MakeSound(new Sound(posicionRuido, radioRuidoApertura, Sound.SoundType.Interesting));
                    }
                    else if (estadoActual == EstadoPuerta.Abierta) // Puerta Abierta
                    {
                        DebugPuerta("Cerraste puerta con la mano");
                        aperturaRapida = false;
                        CambiarEstado(EstadoPuerta.Cerrada);
                        RotacionObjetivo = RotacionCerrada;
                        ActivarColision(false);
                    }
                    else if (estadoActual == EstadoPuerta.Trabada) // Puerta Trabada
                    {
                        DebugPuerta("La puerta esta bloqueada, no se puede abrir con la mano");
                    }
                }


                else if (herramientaActual == InventoryManager.Herramienta.Ganzua) // Se interactua con la ganzua
                {
                    if (estadoActual == EstadoPuerta.Cerrada) // Puerta Cerrada
                    {
                        DebugPuerta("La puerta esta cerrada, no es necesario usar ganzua");
                    }
                    else if (estadoActual == EstadoPuerta.Abierta) // Puerta Abierta
                    {
                        DebugPuerta("Puerta Abierta");
                    }
                    else if (estadoActual == EstadoPuerta.Trabada) // Puerta Trabada, se puede intentar abrir con la ganzúa
                    {
                        if (LockpickMinigameController != null && !LockpickMinigameController.MinigameRunning)
                        {
                            LockpickMinigameController.StartLockpickMinigame();
                            StartCoroutine(EsperarResultadoLockpickMinigame());
                        }
                    }

                }
                else if (herramientaActual == InventoryManager.Herramienta.Palanca)
                {
                    if (estadoActual == EstadoPuerta.Cerrada) // Puerta Cerrada
                    {
                        DebugPuerta("La puerta esta cerrada, no es necesario usar palanca");
                    }
                    else if (estadoActual == EstadoPuerta.Abierta) // Puerta Abierta
                    {
                        DebugPuerta("Puerta Abierta");
                    }
                    else if (estadoActual == EstadoPuerta.Trabada) // Puerta Trabada, la palanca la fuerza y la abre de una
                    {
                        DebugPuerta("Forzaste la puerta con la palanca");
                        aperturaRapida = true;
                        CambiarEstado(EstadoPuerta.Abierta);
                        RotacionObjetivo = RotacionAbierta;
                        ActivarColision(false);
                    }
                }
            }

            else
            {
                // Lo que pasa si la puerta esta bloqueada (no se puede abrir)
                DebugPuerta("La puerta no se puede abrir, esta bloqueada");
            }
        }
    }

    void FixedUpdate()
    {
        if (guardasEnPaso.Count > 0 || fase == FaseMovimiento.Abriendo || fase == FaseMovimiento.Cerrando)
            return;

        bool enMovimiento = Quaternion.Angle(Bisagra.localRotation, RotacionObjetivo) > 0.01f;

        // Mientras se mueve: sin collider. Quieta: con collider.
        ActivarColision(!enMovimiento);

        if (!enMovimiento)
            return;

        float velocidad = aperturaRapida && estadoActual == EstadoPuerta.Abierta
            ? VelocidadRotacion * multiplicadorVelocidadPalanca
            : VelocidadRotacion;

        Quaternion siguiente = Quaternion.RotateTowards(Bisagra.localRotation, RotacionObjetivo, velocidad * Time.fixedDeltaTime);
        Quaternion mundo = Bisagra.parent != null ? Bisagra.parent.rotation * siguiente : siguiente;
        rbBisagra.MoveRotation(mundo);
    }

    private void ActivarColision(bool activa)
    {
        if (colisionActiva == activa || colliderPuerta == null) return;
        colisionActiva = activa;
        colliderPuerta.enabled = activa;
    }

    IEnumerator EsperarResultadoLockpickMinigame()
    {
        int revisionAlComenzar = revisionEstado;
        // Espera hasta que el minijuego termine
        yield return new WaitUntil(() => LockpickMinigameController.MinigameFinished);

        // Un resultado anterior al paso del guarda no debe sobrescribir la restauración.
        if (revisionEstado != revisionAlComenzar || guardasEnPaso.Count > 0)
            yield break;

        if (LockpickMinigameController.MinigameSuccess)
        {
            // Salio bien
            CambiarEstado(EstadoPuerta.Cerrada);
            DebugPuerta("Puerta desbloqueada con la ganzua");
        }
        else
        {
            // Salio mal
            CambiarEstado(EstadoPuerta.Trabada);
            DebugPuerta("No pudiste abrir la puerta con la ganzua");
        }
    }
    void DebugPuerta(string mensaje)
    {
        if (MostrarDebugs)
            Debug.Log("[Puerta] " + mensaje, this);
    }
}
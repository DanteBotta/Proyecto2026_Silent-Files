using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// Window minigame: By holding [F] near a closed window, a signal appears at a random time
/// Releasing the key at the right moment opens the window silently; releasing it too early, too late, or not releasing it at all generates different levels of noise and may not open it

public class WindowGameManager : MonoBehaviour
{
    // Enumerations for possible noise levels and window states
    public enum NivelRuido
    {
        Nulo,
        Bajo,
        Medio,
        Alto
    }
    public enum EstadoVentana
    {
        Bloqueada,
        Cerrada,
        Abierta
    }

    // Custom UnityEvent that takes a NivelRuido parameter, allowing for event-driven noise generation handling
    [Serializable] // This tells Unity "this class can be saved and displayed in the Inspector"
    public class RuidoUnityEvent : UnityEvent<NivelRuido> { }

    [Header("Referencias")]
    public ShowCanvasNearPlayer CanvasWindow; // Script that decides if the player is close enough for the window UI to appear.
    public TextMeshProUGUI WindowStateText; // Text that shows the current state of the window (closed, open, blocked, etc.)
    public TextMeshProUGUI WindowTitleText; // Text that shows the title or instruction of the window
    public Image Image; // Image that provides visual feedback for the minigame

    [Header("Estado")]
    public EstadoVentana estado = EstadoVentana.Cerrada; // The current state of the window
    // Which can be blocked, closed, or open

    [Header("Tecla")]
    public KeyCode teclaInteraccion = KeyCode.F; // The key the player must hold to start and release to open the window

    [Header("Tiempos de la señal")]
    [Tooltip("Tiempo mínimo antes de la señal")]
    public float tiempoEsperaMin = 1f; // Minimum time before the signal appears

    [Tooltip("Tiempo máximo antes de la señal")]
    public float tiempoEsperaMax = 3f; // Maximum time before the signal appears

    [Tooltip("Cuánto tiempo tiene el jugador para soltar la tecla después de que aparece la señal")]
    public float ventanaReaccion = 0.5f; // Maximum time the player has to release the key after the signal appears before it is considered too late

    [Header("Umbrales de reacción")]
    public float umbralPerfecto = 0.2f; // Threshold for a perfect reaction time
    public float umbralAceptable = 0.5f; // Threshold for an acceptable reaction time (beyond this, it is considered too late)

    public float tiempoMostrarResultado = 1f; // How much time to show the result (perfect, acceptable, too late) before returning to the normal state

    [Header("Resultado de cada caso: ¿abre la ventana? ¿qué ruido genera?")]
    // These variables define the outcome of each possible case: whether the window opens and what level of noise is generated

    // if releases the key before the signal appears
    public NivelRuido ruidoSiSueltaAntes = NivelRuido.Bajo;
    public bool abreSiSueltaAntes = false;
    // if releases the key perfectly
    public NivelRuido ruidoSiPerfecto = NivelRuido.Nulo;
    public bool abreSiPerfecto = true;
    // if releases the key after the signal appears but within the acceptable threshold
    public NivelRuido ruidoSiAceptable = NivelRuido.Medio;
    public bool abreSiAceptable = true;
    // if releases the key too late
    public NivelRuido ruidoSiTarde = NivelRuido.Alto;
    public bool abreSiTarde = false;

    [Header("Colores de feedback por resultado")]
    // These colors are used to provide visual feedback to the player based on their performance in the minigame
    public Color colorEsperando = Color.gray;
    public Color colorSenal = Color.green;
    public Color colorPerfecto = Color.green;
    public Color colorAceptable = Color.yellow;
    public Color colorFallo = Color.red;
    public Color colorNeutral = Color.white;

    [Header("Textos")]
    // These texts are displayed in the UI to inform the player about the current state of the window and the minigame
    public string textoEsperandoEstado = "Abriendo...";
    public string textoEsperandoTitulo = "ESPERA...";
    public string textoSenalEstado = "¡SUELTA!";
    public string textoSenalTitulo = "¡AHORA!";
    public string textoCerradaEstado = "Ventana (cerrada)";
    public string textoCerradaTitulo = "Mantén [F]";
    public string textoAbiertaEstado = "Ventana (abierta)";
    public string textoAbiertaTitulo = "Presiona [F] para entrar";
    public string textoBloqueadaEstado = "Ventana (bloqueada)";
    public string textoBloqueadaTitulo = "No puedes abrirla";
    public string textoSoltoAntes = "La soltaste demasiado pronto";
    public string textoDemasiadoTarde = "¡Demasiado tarde!";

    [Header("Eventos")] // These events allow other scripts to respond to the minigame's outcomes
    [Tooltip("Se dispara cada vez que el minijuego genera ruido, con el nivel correspondiente")]
    public RuidoUnityEvent onRuidoGenerado;

    [Tooltip("Se dispara cuando la ventana se abre exitosamente")]
    public UnityEvent onVentanaAbierta;

    // --- Estado interno ---
    private Coroutine coroutineSenal; // It stores a reference to the coroutine that is waiting for a random time to display the signal
    private float tiempoSenal; // The exact moment the signal appeared, to calculate how long it took the player to react.
    private bool minijuegoActivo = false; // Indicates whether the minigame is currently active (the player is holding the key and waiting for the signal)
    private bool senalMostrada = false; // Indicates whether the signal has been shown to the player yet

    private void Start()
    {
        Image.gameObject.SetActive(false); // Initially, the feedback image is hidden until the player is close enough to interact with the window.
        ActualizarUI(); // Function to update the UI texts based on the current state of the window at the start of the game.
    }

    private void Update()
    {
        if (!CanvasWindow.FuncionCanvas)
        {
            Image.gameObject.SetActive(false);
            return;
        }

        // La imagen de feedback (la de "mantener F") solo se muestra mientras
        // la ventana todavía no está abierta. Una vez abierta, desaparece.
        Image.gameObject.SetActive(estado != EstadoVentana.Abierta);

        if (estado == EstadoVentana.Cerrada)
        {
            if (Input.GetKeyDown(teclaInteraccion) && !minijuegoActivo)
            {
                IniciarMinijuego();
            }

            if (Input.GetKeyUp(teclaInteraccion) && minijuegoActivo)
            {
                SoltarTecla();
            }
        }
        else if (estado == EstadoVentana.Abierta)
        {
            WindowStateText.text = textoAbiertaEstado;
            WindowTitleText.text = textoAbiertaTitulo;

            // Acá después ponemos la animación de entrar
        }
        else if (estado == EstadoVentana.Bloqueada)
        {
            WindowStateText.text = textoBloqueadaEstado;
            WindowTitleText.text = textoBloqueadaTitulo;
        }
    }

    // =========================
    // INICIAR MINIJUEGO
    // =========================

    private void IniciarMinijuego()
    {
        minijuegoActivo = true;
        senalMostrada = false;

        WindowStateText.text = textoEsperandoEstado;
        WindowTitleText.text = textoEsperandoTitulo;

        Image.color = colorEsperando;

        coroutineSenal = StartCoroutine(EsperarSenal());
    }

    // =========================
    // ESPERAR SEÑAL
    // =========================

    private IEnumerator EsperarSenal()
    {
        float tiempoEspera = UnityEngine.Random.Range(tiempoEsperaMin, tiempoEsperaMax);

        yield return new WaitForSeconds(tiempoEspera);

        if (!minijuegoActivo)
            yield break;

        tiempoSenal = Time.time;
        senalMostrada = true;

        WindowStateText.text = textoSenalEstado;
        WindowTitleText.text = textoSenalTitulo;

        Image.color = colorSenal;

        yield return new WaitForSeconds(ventanaReaccion);

        // Si todavía está activo, el jugador no soltó a tiempo
        if (minijuegoActivo)
        {
            FalloPorTardanza();
        }
    }

    // =========================
    // SOLTAR TECLA
    // =========================

    private void SoltarTecla()
    {
        // Caso: soltó antes de que apareciera la señal
        if (!senalMostrada)
        {
            if (coroutineSenal != null)
            {
                StopCoroutine(coroutineSenal);
                coroutineSenal = null;
            }

            minijuegoActivo = false;

            WindowStateText.text = textoCerradaEstado;
            WindowTitleText.text = textoSoltoAntes;

            Image.color = colorNeutral;

            Debug.Log("Fallo: soltaste antes de la señal.");

            ResolverResultado(abreSiSueltaAntes, ruidoSiSueltaAntes);
            return;
        }

        float tiempoSoltar = Time.time;
        float reaccion = tiempoSoltar - tiempoSenal;

        Debug.Log("Tiempo de reacción: " + reaccion.ToString("F3") + " segundos");

        minijuegoActivo = false;

        if (coroutineSenal != null)
        {
            StopCoroutine(coroutineSenal);
            coroutineSenal = null;
        }

        // =========================
        // PERFECTO
        // =========================
        if (reaccion >= 0f && reaccion <= umbralPerfecto)
        {
            Image.color = colorPerfecto;
            Debug.Log("¡PERFECTO!");
            ResolverResultado(abreSiPerfecto, ruidoSiPerfecto);
        }
        // =========================
        // ACEPTABLE (abre, pero con más ruido)
        // =========================
        else if (reaccion > umbralPerfecto && reaccion <= umbralAceptable)
        {
            Image.color = colorAceptable;
            Debug.Log("Abierta, pero hiciste ruido.");
            ResolverResultado(abreSiAceptable, ruidoSiAceptable);
        }
        // =========================
        // DEMASIADO TARDE
        // =========================
        else
        {
            FalloPorTardanza();
        }
    }

    // =========================
    // FALLO POR TARDANZA
    // =========================

    private void FalloPorTardanza()
    {
        minijuegoActivo = false;

        Image.color = colorFallo;

        WindowTitleText.text = textoDemasiadoTarde;

        Debug.Log("Fallo: demasiado tarde.");

        ResolverResultado(abreSiTarde, ruidoSiTarde);
    }

    // =========================
    // RESOLVER RESULTADO (abre o no + ruido, según los parámetros del caso)
    // =========================

    private void ResolverResultado(bool abre, NivelRuido ruido)
    {
        HacerRuido(ruido);
        StartCoroutine(ResolverResultadoRoutine(abre));
    }

    private IEnumerator ResolverResultadoRoutine(bool abre)
    {
        // El color y el texto de "PERFECTO"/"FALLO"/etc ya se pusieron
        // antes de llamar a este método (en SoltarTecla/FalloPorTardanza).
        // Acá solo esperamos y después aplicamos el resultado final.

        yield return new WaitForSeconds(tiempoMostrarResultado);

        if (abre)
        {
            estado = EstadoVentana.Abierta;
            WindowStateText.text = textoAbiertaEstado;
            WindowTitleText.text = textoAbiertaTitulo;
            onVentanaAbierta?.Invoke();
            // La Image se oculta sola en el próximo Update, porque el estado ya es Abierta
        }
        else
        {
            estado = EstadoVentana.Cerrada;
            WindowStateText.text = textoCerradaEstado;
            WindowTitleText.text = textoCerradaTitulo;
            Image.color = colorNeutral; // vuelve al color normal
        }
    }

    // Function to update the UI texts based on the current state of the window
    // Is called whenever the state changes to ensure the player sees the correct information
    private void ActualizarUI()
    {
        if (estado == EstadoVentana.Bloqueada) // if is blocked
        {
            WindowStateText.text = textoBloqueadaEstado;
            WindowTitleText.text = textoBloqueadaTitulo;
        }
        else if (estado == EstadoVentana.Cerrada) // if is closed
        {
            WindowStateText.text = textoCerradaEstado;
            WindowTitleText.text = textoCerradaTitulo;
        }
        else if (estado == EstadoVentana.Abierta) // if is open
        {
            WindowStateText.text = textoAbiertaEstado;
            WindowTitleText.text = textoAbiertaTitulo;
        }
    }

    // =========================
    // RUIDO
    // =========================

    private void HacerRuido(NivelRuido nivel)
    {
        Debug.Log("Ruido: " + nivel);
        onRuidoGenerado?.Invoke(nivel);
    }
}
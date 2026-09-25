using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Herramienta que puede tener el jugador en el slot principal.</summary>
public enum ToolType { Ninguna, Mano, Palanca, Ganzua }

/// <summary>
/// Implementalo en el script de inventario del jugador para que el InteractionManager
/// sepa qué herramienta está equipada en el slot principal. Si todavía no tenés
/// inventario, usá el campo "herramientaDebug" del InteractionManager para testear.
/// </summary>
public interface IToolProvider
{
    ToolType GetHerramientaEquipada();
}

/// <summary>
/// Único manager del sistema de puertas + ganzúa. Va en UN SOLO GameObject vacío
/// en la escena (no uno por puerta). Se encarga de:
/// - Detectar la puerta más cercana y mostrar el cartel "Presioná F".
/// - Leer la herramienta equipada y ejecutar la acción correspondiente en la puerta.
/// - Correr el minijuego de ganzúa (reutiliza una sola UI de círculo, sin instanciar
///   un prefab por cada intento).
/// - Avisar (vía UnityEvent) cuando hay que bloquear/liberar el movimiento del jugador.
/// </summary>
public class InteractionManager : MonoBehaviour
{
    [Header("Jugador")]
    public Transform player;
    [Tooltip("Arrastrá acá el script de inventario del jugador (debe implementar IToolProvider).")]
    public MonoBehaviour proveedorHerramientaComponent;
    [Tooltip("Se usa solo si no hay proveedorHerramientaComponent asignado. Sirve para testear sin inventario.")]
    public ToolType herramientaDebug = ToolType.Mano;

    [Header("Detección de puertas")]
    [Tooltip("Se completa solo al iniciar (busca todas las Door de la escena). Podés arrastrar puertas a mano si preferís armar la lista vos mismo.")]
    public Door[] puertas;

    [Header("UI del minijuego de ganzúa (una sola instancia, se reutiliza)")]
    public GameObject panelMinijuego;
    public Image greenZoneImage;      // Image Type = Filled, Fill Method = Radial 360
    public RectTransform lineTransform;
    public CanvasGroup circleCanvasGroup;
    public Image[] indicadoresSuperiores = new Image[5];
    public TextMeshProUGUI textoObjetivo;
    public Color colorNeutral = Color.gray;
    public Color colorAcierto = Color.green;
    public Color colorFallo = Color.red;
    public float fadeDuration = 0.15f;
    public float tiempoEntreCirculos = 0.1f;
    public float tiempoMostrarResultado = 1f;

    [Header("Bloqueo de movimiento durante el minijuego")]
    [Tooltip("Enganchá acá lo necesario para desactivar el movimiento del jugador (ej: PlayerMovement.enabled = false).")]
    public UnityEvent onInteraccionBloqueada;
    [Tooltip("Enganchá acá lo necesario para reactivar el movimiento del jugador.")]
    public UnityEvent onInteraccionLiberada;

    [Header("Rotura de la ganzúa")]
    [Tooltip("Se dispara si, al FALLAR el intento completo, la tirada de probabilidad rompe la ganzúa. " +
             "No es un estado que este manager guarde de forma permanente: enganchá acá tu inventario " +
             "(sacar/deshabilitar el ítem). El jugador puede conseguir o reparar otra y volver a intentar.")]
    public UnityEvent onGanzuaRota;

    [Header("Debug")]
    [Tooltip("Si está en true, imprime por consola lo que va haciendo el manager (puerta detectada, herramienta usada, resultado del minijuego, etc).")]
    public bool debugMode = false;

    public bool MinijuegoEnCurso { get; private set; }

    private IToolProvider proveedorHerramienta;
    private Door puertaCercana;

    private void Awake()
    {
        proveedorHerramienta = proveedorHerramientaComponent as IToolProvider;

        // Si no armaste la lista a mano en el Inspector, la completa sola con todas las Door de la escena
        if (puertas == null || puertas.Length == 0)
            puertas = FindObjectsByType<Door>(FindObjectsSortMode.None);

        if (panelMinijuego != null) panelMinijuego.SetActive(false);
    }

    private void Update()
    {
        if (MinijuegoEnCurso) return;

        Door detectada = EncontrarPuertaInteractuable();
        if (detectada != puertaCercana)
        {
            puertaCercana = detectada;
            Log(puertaCercana != null ? $"Puerta interactuable: {puertaCercana.name}" : "Ninguna puerta interactuable");
        }

        if (puertaCercana != null && Input.GetKeyDown(KeyCode.F))
            Interactuar(puertaCercana);
    }

    private void Log(string mensaje)
    {
        if (debugMode) Debug.Log($"[InteractionManager] {mensaje}");
    }

    // =========================================================
    // DETECCIÓN (delegada al ShowCanvasNearPlayer de cada puerta)
    // =========================================================

    private Door EncontrarPuertaInteractuable()
    {
        Door mejor = null;
        float mejorDistancia = float.MaxValue;

        foreach (var door in puertas)
        {
            if (door == null || !door.JugadorPuedeInteractuar) continue;

            float dist = Vector3.Distance(player.position, door.transform.position);
            if (dist < mejorDistancia)
            {
                mejorDistancia = dist;
                mejor = door;
            }
        }

        return mejor;
    }

    // =========================================================
    // INTERACCIÓN SEGÚN HERRAMIENTA EQUIPADA
    // =========================================================

    private ToolType ObtenerHerramientaEquipada()
    {
        return proveedorHerramienta != null
            ? proveedorHerramienta.GetHerramientaEquipada()
            : herramientaDebug;
    }

    private void Interactuar(Door door)
    {
        ToolType herramienta = ObtenerHerramientaEquipada();
        Log($"Interactuando con {door.name} usando {herramienta}");

        switch (herramienta)
        {
            case ToolType.Mano:
                door.UsarMano();
                break;

            case ToolType.Palanca:
                door.UsarPalanca();
                break;

            case ToolType.Ganzua:
                if (door.PuedeUsarGanzua)
                    IniciarMinijuegoGanzua(door);
                else
                    door.HerramientaNoAplica();
                break;

            case ToolType.Ninguna:
            default:
                Log("No hay ninguna herramienta equipada, no se hace nada.");
                break;
        }
    }

    // =========================================================
    // MINIJUEGO DE GANZÚA
    // =========================================================

    private void IniciarMinijuegoGanzua(Door door)
    {
        StartCoroutine(RunMinigameRoutine(door));
    }

    private IEnumerator RunMinigameRoutine(Door door)
    {
        MinijuegoEnCurso = true;
        onInteraccionBloqueada?.Invoke();

        if (panelMinijuego != null) panelMinijuego.SetActive(true);

        int aciertos = 0;
        int fallos = 0;
        int fallosMaximos = door.TotalCirculosActual - door.AciertosNecesariosActual + 1;

        ResetearIndicadores();
        if (textoObjetivo != null)
            textoObjetivo.text = $"Conseguí {door.AciertosNecesariosActual} para acceder";

        for (int i = 0; i < door.TotalCirculosActual; i++)
        {
            bool acierto = false;
            yield return StartCoroutine(RunCirculoRoutine(door, resultado => acierto = resultado));

            if (acierto)
            {
                aciertos++;
                if (i < indicadoresSuperiores.Length && indicadoresSuperiores[i] != null)
                    indicadoresSuperiores[i].color = colorAcierto;
            }
            else
            {
                fallos++;
                if (i < indicadoresSuperiores.Length && indicadoresSuperiores[i] != null)
                    indicadoresSuperiores[i].color = colorFallo;
            }

            yield return new WaitForSeconds(tiempoEntreCirculos);

            // Corta antes si ya ganó o si ya es matemáticamente imposible ganar
            if (aciertos >= door.AciertosNecesariosActual || fallos >= fallosMaximos)
                break;
        }

        bool exito = aciertos >= door.AciertosNecesariosActual;
        Log($"Minijuego terminado: {(exito ? "ÉXITO" : "FALLO")} ({aciertos} aciertos / {fallos} fallos)");
        door.ResolverGanzua(exito);

        if (!exito)
        {
            float roll = UnityEngine.Random.Range(0f, 1f);
            if (roll <= door.ProbabilidadRoturaActual)
            {
                Log("La ganzúa se rompió.");
                onGanzuaRota?.Invoke();
            }
        }

        yield return new WaitForSeconds(tiempoMostrarResultado);

        if (panelMinijuego != null) panelMinijuego.SetActive(false);

        MinijuegoEnCurso = false;
        onInteraccionLiberada?.Invoke();
    }

    private void ResetearIndicadores()
    {
        foreach (var img in indicadoresSuperiores)
            if (img != null) img.color = colorNeutral;
    }

    // ---- lógica de UN círculo (antes vivía en LockpickCircleUI, ahora es una corutina) ----

    private IEnumerator RunCirculoRoutine(Door door, Action<bool> onResultado)
    {
        float zoneStart = UnityEngine.Random.Range(0f, 360f);
        float zoneWidth = Mathf.Clamp(door.AnchoZonaVerdeActual, 5f, 360f);
        float speed = door.VelocidadGanzuaActual;

        greenZoneImage.fillAmount = zoneWidth / 360f;
        greenZoneImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -zoneStart);

        circleCanvasGroup.blocksRaycasts = false;
        yield return StartCoroutine(Fade(circleCanvasGroup, 0f, 1f));
        circleCanvasGroup.blocksRaycasts = true;

        float currentAngle = 0f;
        bool resuelto = false;
        bool exito = false;

        while (!resuelto)
        {
            currentAngle = (currentAngle + speed * Time.deltaTime) % 360f;
            lineTransform.localRotation = Quaternion.Euler(0f, 0f, -currentAngle);

            if (Input.GetMouseButtonDown(0))
            {
                resuelto = true;
                exito = AnguloEnZonaVerde(currentAngle, zoneStart, zoneWidth);
            }

            yield return null;
        }

        circleCanvasGroup.blocksRaycasts = false;
        yield return StartCoroutine(Fade(circleCanvasGroup, 1f, 0f));

        onResultado(exito);
    }

    private bool AnguloEnZonaVerde(float angle, float zoneStart, float zoneWidth)
    {
        float zoneEnd = zoneStart + zoneWidth;

        if (zoneEnd <= 360f)
            return angle >= zoneStart && angle <= zoneEnd;

        float wrappedEnd = zoneEnd - 360f;
        return angle >= zoneStart || angle <= wrappedEnd;
    }

    private IEnumerator Fade(CanvasGroup cg, float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        cg.alpha = to;
    }
}
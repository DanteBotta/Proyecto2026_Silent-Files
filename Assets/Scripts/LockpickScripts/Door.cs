using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Preset reutilizable de dificultad para el minijuego de ganzúa. Creá varios
/// (Assets > Create > Puertas > Dificultad de ganzúa) — Fácil/Medio/Difícil o los
/// que quieras — y asignaselos a las puertas que correspondan. Una puerta sin
/// preset asignado usa sus propios valores sueltos.
/// </summary>
[CreateAssetMenu(menuName = "Puertas/Dificultad de ganzúa", fileName = "NuevaDificultad")]
public class LockpickDifficulty : ScriptableObject
{
    public string nombre = "Nivel";
    public float velocidad = 100f;
    [Range(5f, 360f)] public float anchoZonaVerde = 60f;
    [Min(1)] public int totalCirculos = 5;
    [Min(1)] public int aciertosNecesarios = 3;
    [Tooltip("Probabilidad de que la ganzúa se rompa si se falla el intento completo (0 a 1).")]
    [Range(0f, 1f)] public float probabilidadRotura = 0.1f;
}

/// <summary>
/// Puerta interactuable. Todo se configura desde el Inspector:
/// - Estado inicial: Open / Closed / Locked (simple, sin combinar dos bools).
/// - Un "Outcome" (sonido + animación + evento + mensaje) por cada resultado posible.
/// - Nivel de ruido por herramienta.
///
/// Reglas:
/// - Mano: abre la puerta si está Closed. Si ya está Open, la CIERRA (evento propio).
/// - Palanca: fuerza la puerta al instante si está Locked (bloqueada).
/// - Ganzáa: abre la puerta vía minijuego si está Locked.
/// </summary>
public class Door : MonoBehaviour
{
    public enum State { Open, Closed, Locked }

    [Serializable]
    public class StringEvent : UnityEvent<string> { }

    [Serializable]
    public class Outcome
    {
        [Tooltip("Nombre del Trigger en el Animator de la puerta. Dejar vacío si no hay animación.")]
        public string animationTrigger;
        public AudioClip sound;
        [TextArea] public string feedbackMessage;
        public UnityEvent onTriggered;
    }

    [Header("Estado")]
    public State state = State.Closed;

    [Header("Componentes")]
    public Animator doorAnimator;
    public AudioSource audioSource;
    [Tooltip("Fallback: si no hay animación asignada para el resultado, desactiva este mesh al abrir.")]
    public MeshRenderer fallbackMeshRenderer;

    [Header("Cartel 'F' de esta puerta")]
    [Tooltip("El ShowCanvasNearPlayer del canvas 'F' asociado a ESTA puerta (normalmente un hijo suyo).")]
    public ShowCanvasNearPlayer promptCanvas;

    /// <summary>True cuando el jugador está dentro de la distanciaFuncion del cartel de esta puerta.</summary>
    public bool JugadorPuedeInteractuar => promptCanvas != null && promptCanvas.FuncionCanvas;

    [Header("Minijuego de ganzáa de preset")]
    [Tooltip("Preset obligatorio. Sin un preset asignado, la ganzúa no podrá utilizarse en esta puerta.")]
    public LockpickDifficulty dificultadGanzua;

    // Los valores del minijuego se obtienen exclusivamente del preset.
    // No hay valores propios de la puerta.
    public float VelocidadGanzuaActual => dificultadGanzua != null ? dificultadGanzua.velocidad : 0f;
    public float AnchoZonaVerdeActual => dificultadGanzua != null ? dificultadGanzua.anchoZonaVerde : 0f;
    public int TotalCirculosActual => dificultadGanzua != null ? dificultadGanzua.totalCirculos : 0;
    public int AciertosNecesariosActual => dificultadGanzua != null ? dificultadGanzua.aciertosNecesarios : 0;
    public float ProbabilidadRoturaActual => dificultadGanzua != null ? dificultadGanzua.probabilidadRotura : 0f;


    [Header("Ruido por herramienta (0 = silencioso, 1 = muy ruidoso)")]
    [Range(0f, 1f)] public float ruidoMano = 0.1f;
    [Range(0f, 1f)] public float ruidoPalanca = 0.8f;
    [Range(0f, 1f)] public float ruidoGanzua = 0.3f;

    [Header("Resultados (sonido + animación + evento por caso)")]
    public Outcome onOpenedWithHand;
    [Tooltip("Se dispara cuando se usa la mano sobre una puerta que ya estaba Open: la cierra.")]
    public Outcome onClosedWithHand;
    public Outcome onOpenedWithLever;
    public Outcome onLockpickSuccess;
    public Outcome onLockpickFailed;
    [Tooltip("Se dispara si se intenta usar una herramienta que no corresponde al estado actual (ej: ganzúa en una puerta ya desbloqueada).")]
    public Outcome onBlocked;

    [Tooltip("Feedback de texto genérico, para enganchar a un Text/TMP en pantalla.")]
    public StringEvent onFeedback;

    /// <summary>Se dispara cada vez que se hace ruido al interactuar (puerta, nivel 0-1). Útil para IA/sigilo.</summary>
    public event Action<Door, float> OnNoise;

    public bool EstaAbierta => state == State.Open;
    // La ganzúa solo puede utilizarse si la puerta está bloqueada
    // y tiene un preset asignado.
    public bool PuedeUsarGanzua => state == State.Locked && dificultadGanzua != null;

    // =========================================================
    // MANO
    // =========================================================
    public bool UsarMano()
    {
        if (state == State.Open)
        {
            Cerrar();
            return true;
        }

        if (state != State.Closed)
        {
            Trigger(onBlocked);
            return false;
        }

        Abrir(onOpenedWithHand);
        HacerRuido(ruidoMano);
        return true;
    }

    // =========================================================
    // PALANCA
    // =========================================================
    public bool UsarPalanca()
    {
        if (state == State.Open)
        {
            Trigger(onBlocked);
            return false;
        }

        // La palanca fuerza la puerta al instante, está Closed o Locked.
        Abrir(onOpenedWithLever);
        HacerRuido(ruidoPalanca);
        return true;
    }

    // =========================================================
    // GANZÚA (el minijuego en sí lo corre el InteractionManager;
    // esta puerta solo expone si corresponde y recibe el resultado)
    // =========================================================
    public void ResolverGanzua(bool exito)
    {
        // Seguridad extra: sin preset no se puede resolver la ganzúa.
        if (dificultadGanzua == null)
        {
            Debug.LogWarning($"La puerta '{name}' no tiene un preset de dificultad de ganzúa asignado.");
            return;
        }

        if (exito)
            Abrir(onLockpickSuccess);
        else
            Trigger(onLockpickFailed);

        HacerRuido(ruidoGanzua);
    }

    /// <summary>Llamar cuando se intenta usar una herramienta que no corresponde al estado actual.</summary>
    public void HerramientaNoAplica()
    {
        Trigger(onBlocked);
    }

    // =========================================================
    // INTERNO
    // =========================================================
    private void Abrir(Outcome outcome)
    {
        state = State.Open;

        bool tieneAnimacion = doorAnimator != null && outcome != null && !string.IsNullOrEmpty(outcome.animationTrigger);
        if (fallbackMeshRenderer != null && !tieneAnimacion)
            fallbackMeshRenderer.enabled = false;

        Trigger(outcome);
    }

    private void Cerrar()
    {
        state = State.Closed;

        bool tieneAnimacion = doorAnimator != null && onClosedWithHand != null && !string.IsNullOrEmpty(onClosedWithHand.animationTrigger);
        if (fallbackMeshRenderer != null && !tieneAnimacion)
            fallbackMeshRenderer.enabled = true;

        Trigger(onClosedWithHand);
        HacerRuido(ruidoMano);
    }

    private void Trigger(Outcome outcome)
    {
        if (outcome == null) return;

        if (audioSource != null && outcome.sound != null)
            audioSource.PlayOneShot(outcome.sound);

        if (doorAnimator != null && !string.IsNullOrEmpty(outcome.animationTrigger))
            doorAnimator.SetTrigger(outcome.animationTrigger);

        if (!string.IsNullOrEmpty(outcome.feedbackMessage))
        {
            Debug.Log(outcome.feedbackMessage);
            onFeedback?.Invoke(outcome.feedbackMessage);
        }

        outcome.onTriggered?.Invoke();
    }

    private void HacerRuido(float nivel)
    {
        OnNoise?.Invoke(this, nivel);
    }
}
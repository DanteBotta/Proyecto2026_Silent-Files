using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controla una puerta que puede utilizarse con mano, ganzúa o palanca.
/// Cada resultado posible dispara su propio UnityEvent, para poder engancharle
/// animaciones, sonidos o textos en pantalla directamente desde el Inspector,
/// sin tener que tocar este script cada vez.
/// </summary>
public class SimpleDoor : MonoBehaviour
{
    /// <summary>
    /// UnityEvent que además pasa un string. Sirve para conectar un mensaje de
    /// feedback (ej. "La puerta está bloqueada") a un Text/TMP en pantalla,
    /// además de la consola.
    /// </summary>
    [Serializable]
    public class StringUnityEvent : UnityEvent<string> { }

    [Header("Estado de la puerta")]
    public bool locked = true;
    public bool isOpen = false;

    [Header("Mesh de la puerta")]
    public MeshRenderer doorMeshRenderer;

    [Header("Minijuego de ganzúa")]
    public LockpickMinigameController lockpickManager;

    [Header("Eventos - Mano")]
    [Tooltip("Se dispara cuando la puerta se abre exitosamente con la mano (solo funciona si NO estaba bloqueada).")]
    public UnityEvent onOpenedWithHand;

    [Tooltip("Se dispara cuando se intenta abrir con la mano pero la puerta está bloqueada.")]
    public UnityEvent onHandBlocked;

    [Header("Eventos - Palanca")]
    [Tooltip("Se dispara al abrir con la palanca. Acá enganchás la animación especial de destrucción/forzado.")]
    public UnityEvent onOpenedWithLever;

    [Header("Eventos - Ganzúa")]
    public UnityEvent onLockpickStarted;
    public UnityEvent onLockpickSucceeded;
    public UnityEvent onLockpickFailed;
    public UnityEvent onLockpickBroken;

    [Header("Eventos generales")]
    [Tooltip("Se dispara SIEMPRE que la puerta termina abierta, sin importar con qué herramienta.")]
    public UnityEvent onDoorOpened;

    [Tooltip("Mensaje de feedback con el texto incluido. Conectalo a un método que reciba un " +
             "string (ej. un Text/TMP en pantalla) además de mirar la consola.")]
    public StringUnityEvent onFeedbackMessage;

    // =========================
    // MANO
    // =========================

    public void UseHand()
    {
        if (isOpen)
        {
            Feedback("La puerta ya está abierta.");
            return;
        }

        if (locked)
        {
            Feedback("La puerta está bloqueada.");
            onHandBlocked?.Invoke();
            return;
        }

        OpenInternal(onOpenedWithHand);
    }

    // =========================
    // GANZÚA
    // =========================

    public void UseLockpick()
    {
        if (isOpen)
        {
            Feedback("La puerta ya está abierta.");
            return;
        }

        if (!locked)
        {
            Feedback("La puerta no está bloqueada. No hace falta usar la ganzúa.");
            return;
        }

        if (lockpickManager == null)
        {
            Debug.LogError("No hay un LockpickMinigameController asignado a esta puerta.");
            return;
        }

        onLockpickStarted?.Invoke();
        lockpickManager.StartMinigameForDoor(this);
    }

    // =========================
    // PALANCA
    // =========================

    public void UseLever()
    {
        if (isOpen)
        {
            Feedback("La puerta ya está abierta.");
            return;
        }

        Feedback("Usaste la palanca para abrir la puerta.");

        // La palanca abre la puerta esté o no bloqueada, con su propia animación
        OpenInternal(onOpenedWithLever);
    }

    // =========================
    // ABRIR PUERTA (interno, compartido por todos los métodos)
    // =========================

    private void OpenInternal(UnityEvent metodoEspecifico)
    {
        if (isOpen) return;

        isOpen = true;
        locked = false;

        // Por defecto desactiva el mesh (podés reemplazar esto por una animación
        // real más adelante, o dejarlo como fallback si no conectás ninguna animación).
        if (doorMeshRenderer != null)
        {
            doorMeshRenderer.enabled = false;
        }

        metodoEspecifico?.Invoke();
        onDoorOpened?.Invoke();

        Feedback("¡Puerta abierta!");
    }

    // =========================
    // RESULTADOS DE LA GANZÚA (llamados por LockpickMinigameController)
    // =========================

    public void LockpickSucceeded()
    {
        onLockpickSucceeded?.Invoke();
        OpenInternal(null); // la ganzúa no tiene animación "propia" distinta a la genérica, pero podés agregarle una acá si querés
    }

    public void LockpickFailed()
    {
        Feedback("La ganzúa no consiguió abrir la puerta.");
        onLockpickFailed?.Invoke();
    }

    public void LockpickBroken()
    {
        Feedback("¡La ganzúa se rompió!");
        onLockpickBroken?.Invoke();
    }

    // =========================
    // FEEDBACK (consola + evento con texto para UI)
    // =========================

    private void Feedback(string message)
    {
        Debug.Log(message);
        onFeedbackMessage?.Invoke(message);
    }
}

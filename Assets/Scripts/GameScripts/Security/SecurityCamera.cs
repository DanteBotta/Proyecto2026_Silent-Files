using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Select instant detection before FieldOfView initializes its guard behaviour.
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
[RequireComponent(typeof(FieldOfView))]
public sealed class SecurityCamera : MonoBehaviour
{
    [SerializeField, Tooltip("Guardias que reciben la posición descubierta. Se pueden asignar varios.")]
    private AI[] guards = new AI[0];

    [SerializeField, Tooltip("Aviso por cada nueva detección; disponible para una futura alarma general.")]
    private UnityEvent<Vector3> onPlayerDetected = new UnityEvent<Vector3>();

    public UnityEvent<Vector3> PlayerDetected => onPlayerDetected;

    private FieldOfView fieldOfView;
    private bool detectionReported;
    private readonly HashSet<AI> notifiedGuards = new HashSet<AI>();

    private void Reset()
    {
        GetComponent<FieldOfView>().UseInstantDetection();
    }

    private void Awake()
    {
        fieldOfView = GetComponent<FieldOfView>();
        fieldOfView.UseInstantDetection();
    }

    private void OnEnable()
    {
        if (fieldOfView == null)
            fieldOfView = GetComponent<FieldOfView>();

        fieldOfView.UseInstantDetection();
        fieldOfView.VisibilityChanged += OnVisibilityChanged;
        // A coordinator can be re-enabled before the next FOV frame; never replay stale visibility.
        fieldOfView.RefreshVisibility();
        OnVisibilityChanged(fieldOfView.isActiveAndEnabled && fieldOfView.canSeePlayer);
    }

    private void OnDisable()
    {
        if (fieldOfView != null)
            fieldOfView.VisibilityChanged -= OnVisibilityChanged;

        detectionReported = false;
    }

    private void OnVisibilityChanged(bool visible)
    {
        if (!visible)
        {
            detectionReported = false;
            return;
        }

        if (detectionReported || fieldOfView.playerRef == null)
            return;

        detectionReported = true;
        // Copy once: neither guards nor alarm listeners receive a live player reference.
        Vector3 detectedPosition = fieldOfView.playerRef.transform.position;
        notifiedGuards.Clear();
        if (guards != null)
        {
            foreach (AI guard in guards)
            {
                if (guard != null && notifiedGuards.Add(guard))
                    guard.ReportPlayerDetection(detectedPosition);
            }
        }

        onPlayerDetected.Invoke(detectedPosition);
    }
}

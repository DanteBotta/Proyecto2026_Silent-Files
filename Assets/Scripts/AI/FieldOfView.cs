using UnityEngine;
using System.Collections;
using System;

[DefaultExecutionOrder(50)]
public class FieldOfView : MonoBehaviour
{
    // Keep the serialized field names and public API used by existing guard scenes.
    [SerializeField, Min(0f)] public float delay = 0.2f;
    [SerializeField, Min(0f)] public float radius = 7f;
    [SerializeField, Range(0, 360)]
    public float angle = 180f;
    [SerializeField] public GameObject playerRef;
    [SerializeField] public LayerMask targetMask;
    [SerializeField] public LayerMask obstructionMask;
    public bool canSeePlayer;

    public float distanceToPlayer; // Distancia entre el enemigo y el jugador

    public AI seguirJugadorScript; // Referencia al script seguirjugador

    public float suspicion = 0f; //Sospecha del enemigo, si llega a 100 el enemigo detecta al jugador

    public enum DetectionMode { GuardSuspicion, Instant }

    [SerializeField, Tooltip("GuardSuspicion conserva la sospecha del guardia. SecurityCamera selecciona Instant automáticamente.")]
    private DetectionMode detectionMode;

    public bool IsInstantDetection => detectionMode == DetectionMode.Instant;
    public event Action<bool> VisibilityChanged;

    private Coroutine detectionRoutine;

    private void Awake()
    {
        if (!IsInstantDetection && seguirJugadorScript == null)
            seguirJugadorScript = GetComponentInParent<AI>();

        // Una mascara vacia no detecta ningun collider.
        if (targetMask.value == 0)
            targetMask = LayerMask.GetMask("Target");

        if (obstructionMask.value == 0)
            obstructionMask = LayerMask.GetMask("Obstruction");
    }

    private void OnEnable()
    {
        RefreshVisibility();
        if (!IsInstantDetection)
            detectionRoutine = StartCoroutine(FOVRoutine());
    }

    private void OnDisable()
    {
        StopDetectionRoutine();
        SetVisibility(false);
        suspicion = 0f;
        if (!IsInstantDetection && seguirJugadorScript != null)
            seguirJugadorScript.SetPlayerDetected(false);
    }

    // Camera coordination selects timing; the visibility test stays shared with guards.
    public void UseInstantDetection()
    {
        detectionMode = DetectionMode.Instant;
        suspicion = 0f;
        StopDetectionRoutine();
    }

    private void StopDetectionRoutine()
    {
        if (detectionRoutine == null)
            return;

        StopCoroutine(detectionRoutine);
        detectionRoutine = null;
    }

    private void ResolvePlayer()
    {
        // Volver a buscar si el jugador se instancia despues de iniciar la IA.
        if (!IsInstantDetection && playerRef == null && seguirJugadorScript != null)
            playerRef = seguirJugadorScript.player;

        if (playerRef == null)
            playerRef = GameObject.FindGameObjectWithTag("Player");

        if (!IsInstantDetection && seguirJugadorScript != null && seguirJugadorScript.player == null)
            seguirJugadorScript.player = playerRef;
    }

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(delay);

        while (true)
        {
            yield return wait;
            RefreshVisibility();
        }
    }

    public void RefreshVisibility()
    {
        if (!isActiveAndEnabled)
        {
            SetVisibility(false);
            return;
        }

        ResolvePlayer();
        // Moving a camera, player or door through Transform must affect this frame's
        // instant visibility, even when Physics.autoSyncTransforms is disabled.
        if (IsInstantDetection)
            Physics.SyncTransforms();

        distanceToPlayer = playerRef != null
            ? Vector3.Distance(transform.position, playerRef.transform.position)
            : float.PositiveInfinity;
        SetVisibility(IsPlayerVisible());
    }

    private bool IsPlayerVisible()
    {
        if (playerRef == null || !playerRef.activeInHierarchy)
            return false;

        // Collider bounds can overlap the sphere with the player's pivot outside it.
        if (radius <= 0f || distanceToPlayer > radius)
            return false;

        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask);

        // OverlapSphere no garantiza que el primer collider sea el jugador.
        foreach (Collider candidate in rangeChecks)
        {
            if (candidate.transform != playerRef.transform &&
                !candidate.transform.IsChildOf(playerRef.transform))
                continue;

            Vector3 directionToTarget = (playerRef.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, directionToTarget) > angle / 2)
                continue;

            float distanceToTarget = Vector3.Distance(transform.position, playerRef.transform.position);
            if (TryGetObstruction(directionToTarget, distanceToTarget, out _))
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Queries the same solid obstacle layers used by player detection. A positive
    /// sweep radius is only for conservative visual sampling between cone rays;
    /// player visibility always uses the default zero-radius ray.
    /// This does not detect the player, change visibility or publish events.
    /// </summary>
    public bool TryGetObstruction(Vector3 worldDirection, float maxDistance,
        out RaycastHit hit, float sweepRadius = 0f)
    {
        hit = default;
        if (maxDistance <= 0f || obstructionMask.value == 0 || worldDirection.sqrMagnitude < 0.000001f)
            return false;

        Vector3 direction = worldDirection.normalized;
        if (sweepRadius > 0f)
            return Physics.SphereCast(transform.position, sweepRadius, direction, out hit,
                maxDistance, obstructionMask, QueryTriggerInteraction.Ignore);

        return Physics.Raycast(transform.position, direction, out hit, maxDistance,
            obstructionMask, QueryTriggerInteraction.Ignore);
    }

    private void SetVisibility(bool visible)
    {
        if (canSeePlayer == visible)
            return;

        canSeePlayer = visible;
        VisibilityChanged?.Invoke(visible);
    }

    private void LateUpdate()
    {
        // Runs after player movement and the camera sweep, before AI navigation.
        if (IsInstantDetection)
            RefreshVisibility();
    }
    void Update()
    {
        if (IsInstantDetection || playerRef == null || seguirJugadorScript == null)
            return;

        distanceToPlayer = Vector3.Distance(transform.position, playerRef.transform.position);
        if (canSeePlayer)
        {
            suspicion += (500f * Time.deltaTime) / Mathf.Max(distanceToPlayer, 0.1f); // Aumenta la sospecha del enemigo mientras el jugador está a la vista
        }

        if (!canSeePlayer)
        {
            suspicion -= 20f * Time.deltaTime; // Disminuye la sospecha del enemigo cuando el jugador no está a la vista
        }

        suspicion = Mathf.Clamp(suspicion, 0, 150);

        // Let AI choose movement so hearing cannot override a chase.
        seguirJugadorScript.SetPlayerDetected(suspicion >= 100f);
    }
}

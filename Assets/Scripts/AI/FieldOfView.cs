using UnityEngine;
using System.Collections;

public class FieldOfView : MonoBehaviour
{
    public float delay = 0.2f;
    public float radius = 7f;
    [Range(0, 360)]
    public float angle = 180f;
    public GameObject playerRef;
    public LayerMask targetMask;
    public LayerMask obstructionMask;
    public bool canSeePlayer;

    public float distanceToPlayer; // Distancia entre el enemigo y el jugador

    public AI seguirJugadorScript; // Referencia al script seguirjugador

    public float suspicion = 0f; //Sospecha del enemigo, si llega a 100 el enemigo detecta al jugador

    private void Awake()
    {
        if (seguirJugadorScript == null)
            seguirJugadorScript = GetComponentInParent<AI>();

        // Una mascara vacia no detecta ningun collider.
        if (targetMask.value == 0)
            targetMask = LayerMask.GetMask("Target");

        if (obstructionMask.value == 0)
            obstructionMask = LayerMask.GetMask("Obstruction");
    }

    private void OnEnable()
    {
        FieldOfViewCheck();
        StartCoroutine(FOVRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        canSeePlayer = false;
        suspicion = 0f;
        if (seguirJugadorScript != null)
            seguirJugadorScript.SetPlayerDetected(false);
    }

    private void ResolvePlayer()
    {
        // Volver a buscar si el jugador se instancia despues de iniciar la IA.
        if (playerRef == null && seguirJugadorScript != null)
            playerRef = seguirJugadorScript.player;

        if (playerRef == null)
            playerRef = GameObject.FindGameObjectWithTag("Player");

        if (seguirJugadorScript != null && seguirJugadorScript.player == null)
            seguirJugadorScript.player = playerRef;
    }

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(delay);

        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    private void FieldOfViewCheck()
    {
        canSeePlayer = false;
        ResolvePlayer();
        if (playerRef == null || !playerRef.activeInHierarchy)
            return;

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
            if (Physics.Raycast(transform.position, directionToTarget, distanceToTarget,
                obstructionMask, QueryTriggerInteraction.Ignore))
                continue;

            canSeePlayer = true;
            return;
        }
    }
    void Update()
    {
        if (playerRef == null || seguirJugadorScript == null)
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

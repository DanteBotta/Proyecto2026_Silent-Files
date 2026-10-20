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

    private void Start()
    {
        playerRef = GameObject.FindGameObjectWithTag("Player");
        StartCoroutine(FOVRoutine());
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
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask);

        if (rangeChecks.Length != 0)
        {
            Transform target = rangeChecks[0].transform;
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask))
                    canSeePlayer = true;
                else
                    canSeePlayer = false;
            }
            else
                canSeePlayer = false;
        }
        else if (canSeePlayer)
            canSeePlayer = false;
    }
    void Update()
    {
        if (playerRef == null || seguirJugadorScript == null)
            return;

        distanceToPlayer = Vector3.Distance(transform.position, playerRef.transform.position);
        suspicion = Mathf.Clamp(suspicion, 0, 150);
        if (canSeePlayer)
        {
            suspicion += (500f * Time.deltaTime) / Mathf.Max(distanceToPlayer, 0.1f); // Aumenta la sospecha del enemigo mientras el jugador está a la vista
        }

        if (!canSeePlayer)
        {
            suspicion -= 20f * Time.deltaTime; // Disminuye la sospecha del enemigo cuando el jugador no está a la vista
        }

        // Let AI choose movement so hearing cannot override a chase.
        seguirJugadorScript.SetPlayerDetected(suspicion >= 100f);
    }
}

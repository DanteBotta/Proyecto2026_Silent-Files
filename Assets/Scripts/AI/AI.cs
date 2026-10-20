using GamePlay;
using UnityEngine;
using UnityEngine.AI;

public class AI : MonoBehaviour, IHear
{
    public NavMeshAgent navMeshAgent;
    public Transform[] destinations;
    private int i = 0;

    public bool followPlayer;
    public float distanceToFollowPlayer = 10;
    public float distanceToFollowPath = 2;
    public GameObject player;

    [Min(0.1f)] public float soundArrivalDistance = 0.5f;
    [Min(0.1f)] public float soundNavMeshSearchRadius = 2f;

    private bool playerDetected;
    private bool investigatingSound;

    public bool IsChasingPlayer => player != null &&
        (playerDetected || (followPlayer &&
        Vector3.Distance(transform.position, player.transform.position) <= distanceToFollowPlayer));

    private bool CanNavigate => navMeshAgent != null &&
        navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh;

    private void Awake()
    {
        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (player == null)
        {
            PlayerController controller = Object.FindAnyObjectByType<PlayerController>();
            if (controller != null)
                player = controller.gameObject;
        }
    }

    // Resolve movement after vision and player movement have updated.
    private void LateUpdate()
    {
        if (!CanNavigate)
            return;

        if (IsChasingPlayer)
        {
            FollowPlayer();
            return;
        }

        if (investigatingSound)
        {
            if (navMeshAgent.pathPending)
                return;

            // Resume the interrupted waypoint after arrival or if the path becomes blocked.
            if (navMeshAgent.hasPath &&
                navMeshAgent.pathStatus == NavMeshPathStatus.PathComplete &&
                navMeshAgent.remainingDistance > Mathf.Max(soundArrivalDistance, navMeshAgent.stoppingDistance))
                return;

            investigatingSound = false;
            navMeshAgent.ResetPath();
        }

        EnemyPath();
    }

    public void SetPlayerDetected(bool detected)
    {
        playerDetected = detected;
        if (detected)
            investigatingSound = false;
    }

    public void RespondToSound(Sound sound)
    {
        if (sound == null || !isActiveAndEnabled || IsChasingPlayer || !CanNavigate)
            return;

        var filter = new NavMeshQueryFilter
        {
            agentTypeID = navMeshAgent.agentTypeID,
            areaMask = navMeshAgent.areaMask
        };
        if (!NavMesh.SamplePosition(sound.pos, out NavMeshHit hit, soundNavMeshSearchRadius, filter))
            return;

        var path = new NavMeshPath();
        if (!navMeshAgent.CalculatePath(hit.position, path) ||
            path.status != NavMeshPathStatus.PathComplete || !navMeshAgent.SetPath(path))
            return;

        navMeshAgent.isStopped = false;
        investigatingSound = true;
    }

    public void EnemyPath()
    {
        if (!CanNavigate || destinations == null || destinations.Length == 0)
            return;

        i %= destinations.Length;
        if (destinations[i] == null)
            return;

        navMeshAgent.isStopped = false;
        navMeshAgent.destination = destinations[i].position;

        if (Vector3.Distance(transform.position, destinations[i].position) <= distanceToFollowPath)
            i = (i + 1) % destinations.Length;
    }

    public void FollowPlayer()
    {
        if (!CanNavigate || player == null)
            return;

        investigatingSound = false;
        navMeshAgent.isStopped = false;
        navMeshAgent.destination = player.transform.position;
    }
}
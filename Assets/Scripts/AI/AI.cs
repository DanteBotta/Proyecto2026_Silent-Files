using GamePlay;
using UnityEngine;
using UnityEngine.AI;

public class AI : MonoBehaviour, IHear
{
    public NavMeshAgent navMeshAgent;
    public Transform[] destinations;
    [Tooltip("Descanso en segundos para cada elemento de Destinations, en el mismo orden. 0 = sin descanso.")]
    [Min(0f)] public float[] destinationRestTimes = new float[0];
    private int i = 0;

    public bool followPlayer;
    public float distanceToFollowPlayer = 10;
    public float distanceToFollowPath = 2;
    public GameObject player;

    [Min(0.1f)] public float soundArrivalDistance = 0.5f;
    [Min(0.1f)] public float soundNavMeshSearchRadius = 2f;

    private bool playerDetected;
    private bool investigatingSound;
    private bool restingAtDestination;
    private float restEndTime;

    public bool IsChasingPlayer => player != null &&
        (playerDetected || (followPlayer &&
        Vector3.Distance(transform.position, player.transform.position) <= distanceToFollowPlayer));

    [Header ("Animator Reference")]
    private Animator animator;

    private bool CanNavigate => navMeshAgent != null &&
        navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh;

    private void OnValidate()
    {
        int destinationCount = destinations != null ? destinations.Length : 0;
        if (destinationRestTimes == null || destinationRestTimes.Length != destinationCount)
            System.Array.Resize(ref destinationRestTimes, destinationCount);

        for (int index = 0; index < destinationRestTimes.Length; index++)
            destinationRestTimes[index] = Mathf.Max(0f, destinationRestTimes[index]);
    }

    private void Awake()
    {
        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();

        animator = GetComponent<Animator>();
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

    void Update()
    {
        if (animator == null || navMeshAgent == null)
            return;

        animator.SetFloat("Velocidad", navMeshAgent.velocity.magnitude);
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

        restingAtDestination = false;
        navMeshAgent.isStopped = false;
        investigatingSound = true;
    }

    public void EnemyPath()
    {
        if (!CanNavigate || destinations == null || destinations.Length == 0)
            return;

        if (restingAtDestination)
        {
            if (Time.time < restEndTime)
                return;

            restingAtDestination = false;
        }

        i %= destinations.Length;
        if (destinations[i] == null)
        {
            i = (i + 1) % destinations.Length;
            return;
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.destination = destinations[i].position;

        float arrivalDistance = Mathf.Max(distanceToFollowPath, navMeshAgent.stoppingDistance);
        if (Vector3.Distance(transform.position, destinations[i].position) <= arrivalDistance)
        {
            float restDuration = destinationRestTimes != null && i < destinationRestTimes.Length
                ? Mathf.Max(0f, destinationRestTimes[i])
                : 0f;

            // El punto ya fue visitado: una interrupción continuará hacia el siguiente.
            i = (i + 1) % destinations.Length;

            if (restDuration > 0f)
            {
                restingAtDestination = true;
                restEndTime = Time.time + restDuration;
                navMeshAgent.isStopped = true;
            }
        }
    }

    public void FollowPlayer()
    {
        if (!CanNavigate || player == null)
            return;

        restingAtDestination = false;
        investigatingSound = false;
        navMeshAgent.isStopped = false;
        navMeshAgent.destination = player.transform.position;
    }
}

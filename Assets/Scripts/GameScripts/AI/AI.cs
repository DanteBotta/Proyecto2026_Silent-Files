using GamePlay;
using UnityEngine;
using UnityEngine.AI;

// Resolve FOV reports (50) before movement, then let GuardDoorPassage (100) enforce door waits.
[DefaultExecutionOrder(75)]
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
    private bool investigatingPlayerReport;
    private bool playerReportNeedsPath;
    private bool restingAtDestination;
    private float restEndTime;
    private GuardDoorPassage doorPassage;

    public bool IsChasingPlayer => player != null &&
        (playerDetected || (!investigatingPlayerReport && followPlayer &&
        Vector3.Distance(transform.position, player.transform.position) <= distanceToFollowPlayer));

    public bool IsInvestigatingPlayerReport => investigatingPlayerReport;
    public Vector3 LastKnownPlayerPosition { get; private set; }

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

        doorPassage = GuardDoorPassage.EnsureFor(navMeshAgent);
        if (doorPassage != null)
            doorPassage.enabled = isActiveAndEnabled;

        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (doorPassage != null)
            doorPassage.enabled = true;
    }

    private void OnDisable()
    {
        if (doorPassage != null)
            doorPassage.enabled = false;
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

        // A report can arrive before the agent is on its NavMesh. Keep the snapshot until ready.
        // It also replaces an interrupted patrol/sound route while a door is opening.
        if (investigatingPlayerReport && playerReportNeedsPath)
        {
            playerReportNeedsPath = false;
            if (TrySetInvestigationPath(LastKnownPlayerPosition))
                return;

            investigatingPlayerReport = false;
            navMeshAgent.ResetPath();
        }

        // Keep the current route while the door opens. Investigations and chase can replace it.
        if (doorPassage != null && doorPassage.IsWaitingForDoor)
            return;

        if (investigatingSound || investigatingPlayerReport)
        {
            if (navMeshAgent.pathPending)
                return;

            // Resume the interrupted waypoint after arrival or if the path becomes blocked.
            if (navMeshAgent.hasPath &&
                navMeshAgent.pathStatus == NavMeshPathStatus.PathComplete &&
                navMeshAgent.remainingDistance > Mathf.Max(soundArrivalDistance, navMeshAgent.stoppingDistance))
                return;

            investigatingSound = false;
            investigatingPlayerReport = false;
            navMeshAgent.ResetPath();
        }

        EnemyPath();
    }

    public void SetPlayerDetected(bool detected)
    {
        playerDetected = detected;
        if (detected)
        {
            investigatingSound = false;
            investigatingPlayerReport = false;
            playerReportNeedsPath = false;
        }
    }

    /// <summary>
    /// Receives a position observed by a camera or another alert source. Only this AI owns
    /// navigation; the report never grants ongoing knowledge of the player's Transform.
    /// </summary>
    public void ReportPlayerDetection(Vector3 lastKnownPosition)
    {
        // The guard's own confirmed detection has priority over a remote observation.
        if (playerDetected && player != null)
            return;

        LastKnownPlayerPosition = lastKnownPosition;
        investigatingPlayerReport = true;
        playerReportNeedsPath = true;
        investigatingSound = false;
        restingAtDestination = false;
    }

    public void RespondToSound(Sound sound)
    {
        if (sound == null || !isActiveAndEnabled || IsChasingPlayer ||
            investigatingPlayerReport || !CanNavigate)
            return;

        if (TrySetInvestigationPath(sound.pos))
            investigatingSound = true;
    }

    // Both hearing and camera alerts use the existing complete-path investigation policy.
    private bool TrySetInvestigationPath(Vector3 position)
    {
        var filter = new NavMeshQueryFilter
        {
            agentTypeID = navMeshAgent.agentTypeID,
            areaMask = navMeshAgent.areaMask
        };
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, soundNavMeshSearchRadius, filter))
            return false;

        var path = new NavMeshPath();
        if (!navMeshAgent.CalculatePath(hit.position, path) ||
            path.status != NavMeshPathStatus.PathComplete || !navMeshAgent.SetPath(path))
            return false;

        restingAtDestination = false;
        navMeshAgent.isStopped = false;
        return true;
    }

    public void EnemyPath()
    {
        if (!CanNavigate || investigatingPlayerReport || destinations == null || destinations.Length == 0)
            return;

        if (doorPassage != null && doorPassage.IsWaitingForDoor)
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
        if (!CanNavigate || player == null || (investigatingPlayerReport && !playerDetected))
            return;

        restingAtDestination = false;
        investigatingSound = false;
        investigatingPlayerReport = false;
        playerReportNeedsPath = false;
        navMeshAgent.isStopped = false;
        navMeshAgent.destination = player.transform.position;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Runs after the guard has chosen its patrol, investigation or chase destination.
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class GuardDoorPassage : MonoBehaviour
{
    [SerializeField, Min(0.1f), Tooltip("Distancia del recorrido que se revisa para abrir puertas antes de llegar.")]
    private float lookAheadDistance = 2f;

    [SerializeField, Min(0f), Tooltip("Separación adicional antes de permitir que se cierre una puerta detrás del guarda.")]
    private float closingClearance = 0.2f;

    private sealed class Passage
    {
        public Door Door;
        public bool Entered;
    }

    private readonly List<Passage> passages = new List<Passage>();
    private NavMeshAgent agent;
    private Collider[] bodyColliders;
    private bool ownsStop;
    private bool restoreStopPending;
    private bool stoppedBeforeWait;
    private bool hasPreviousPosition;
    private Vector3 previousPosition;

    public bool IsWaitingForDoor { get; private set; }

    private bool CanNavigate => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
    private Vector3 FeetPosition => agent.transform.position + Vector3.up * agent.baseOffset;

    public static GuardDoorPassage EnsureFor(NavMeshAgent navigationAgent)
    {
        if (navigationAgent == null)
            return null;

        GuardDoorPassage passage = navigationAgent.GetComponent<GuardDoorPassage>();
        if (passage == null)
            passage = navigationAgent.gameObject.AddComponent<GuardDoorPassage>();

        passage.agent = navigationAgent;
        return passage;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        bodyColliders = GetComponentsInChildren<Collider>(true);
    }

    private void LateUpdate()
    {
        if (!CanNavigate)
        {
            ReleaseAllPassages();
            return;
        }

        if (restoreStopPending)
            RestoreMovement();

        Vector3 position = FeetPosition;
        GetBodyClearance(position, out Vector3 bodyPosition, out float bodyRadius, out float bodyHeight);
        bool hasRoute = agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete;
        bool hasTravelRemaining = hasRoute &&
            agent.remainingDistance > Mathf.Max(agent.stoppingDistance, 0.05f);
        bool wantsToMove = (!agent.isStopped || ownsStop) && hasTravelRemaining;
        Vector3[] corners = hasRoute ? agent.path.corners : null;
        float distanceToCheck = Mathf.Max(lookAheadDistance,
            bodyRadius + agent.speed * Time.deltaTime + closingClearance);

        for (int index = passages.Count - 1; index >= 0; index--)
        {
            Passage passage = passages[index];
            Door door = passage.Door;
            if (door == null || !door.isActiveAndEnabled)
            {
                ReleasePassage(index);
                continue;
            }

            bool clear = door.IsGuardClear(bodyPosition, bodyRadius, bodyHeight, closingClearance);
            if (!door.IsGuardClear(bodyPosition, bodyRadius, bodyHeight, 0f) ||
                (hasPreviousPosition && door.TryGetPathIntersection(previousPosition, bodyPosition,
                    bodyRadius, bodyHeight, out _)))
                passage.Entered = true;

            bool stillOnRoute = wantsToMove && IntersectsRoute(door, position, corners, distanceToCheck);
            // A changed/cancelled path may release a door only while the guard is safely outside it.
            // Chase updates can request a new path every frame; an existing route is still usable.
            bool waitingForFirstPath = agent.pathPending && corners == null;
            if (clear && (passage.Entered || (!stillOnRoute && !waitingForFirstPath)))
                ReleasePassage(index);
            else
                // Reconcile a door that was disabled and enabled again between guard updates.
                door.RequestGuardPassage(this);
        }

        if (wantsToMove && corners != null)
        {
            IReadOnlyList<Door> doors = Door.ActiveDoors;
            for (int index = 0; index < doors.Count; index++)
            {
                Door door = doors[index];
                if (door == null || HasPassage(door) ||
                    !IntersectsRoute(door, position, corners, distanceToCheck))
                    continue;

                if (door.RequestGuardPassage(this))
                {
                    passages.Add(new Passage
                    {
                        Door = door,
                        Entered = !door.IsGuardClear(bodyPosition, bodyRadius, bodyHeight, 0f)
                    });
                }
            }
        }

        bool needsToWait = false;
        for (int index = 0; index < passages.Count; index++)
        {
            if (!passages[index].Door.IsPassable && wantsToMove)
            {
                needsToWait = true;
                break;
            }
        }

        IsWaitingForDoor = needsToWait;
        if (needsToWait)
        {
            if (!ownsStop)
            {
                stoppedBeforeWait = agent.isStopped;
                ownsStop = true;
            }

            agent.isStopped = true;
        }
        else
        {
            RestoreMovement();
        }

        previousPosition = bodyPosition;
        hasPreviousPosition = true;
    }

    private void GetBodyClearance(Vector3 feet, out Vector3 bodyFeet, out float radius, out float height)
    {
        radius = agent.radius;
        float minY = feet.y;
        float maxY = feet.y + agent.height;
        if (bodyColliders != null)
        {
            foreach (Collider bodyCollider in bodyColliders)
            {
                if (bodyCollider == null || !bodyCollider.enabled || bodyCollider.isTrigger ||
                    !bodyCollider.gameObject.activeInHierarchy)
                    continue;

                Bounds bounds = bodyCollider.bounds;
                radius = Mathf.Max(radius, Mathf.Abs(bounds.center.x - feet.x) + bounds.extents.x,
                    Mathf.Abs(bounds.center.z - feet.z) + bounds.extents.z);
                minY = Mathf.Min(minY, bounds.min.y);
                maxY = Mathf.Max(maxY, bounds.max.y);
            }
        }

        bodyFeet = new Vector3(feet.x, minY, feet.z);
        height = maxY - minY;
    }

    private bool IntersectsRoute(Door door, Vector3 position, Vector3[] corners, float remainingLookAhead)
    {
        if (corners == null || corners.Length < 2)
            return false;

        Vector3 segmentStart = position;
        // The first corner is the agent's current path position, not a future waypoint.
        for (int index = 1; index < corners.Length && remainingLookAhead > 0f; index++)
        {
            Vector3 segmentEnd = corners[index] + Vector3.up * agent.baseOffset;
            float length = Vector3.Distance(segmentStart, segmentEnd);
            if (length > remainingLookAhead)
                segmentEnd = Vector3.MoveTowards(segmentStart, segmentEnd, remainingLookAhead);

            if (door.TryGetPathIntersection(segmentStart, segmentEnd, agent.radius, agent.height, out _))
                return true;

            remainingLookAhead -= length;
            segmentStart = segmentEnd;
        }

        return false;
    }

    private bool HasPassage(Door door)
    {
        for (int index = 0; index < passages.Count; index++)
        {
            if (passages[index].Door == door)
                return true;
        }

        return false;
    }

    private void ReleasePassage(int index)
    {
        Door door = passages[index].Door;
        passages.RemoveAt(index);
        if (door != null)
            door.ReleaseGuardPassage(this);
    }

    public void ReleaseAllPassages()
    {
        for (int index = passages.Count - 1; index >= 0; index--)
            ReleasePassage(index);

        IsWaitingForDoor = false;
        hasPreviousPosition = false;
        RestoreMovement();
    }

    private void RestoreMovement()
    {
        if (!ownsStop && !restoreStopPending)
            return;

        if (!CanNavigate)
        {
            // Keep restoration separate from an active wait so a cancelled route is never reopened.
            restoreStopPending = true;
            ownsStop = false;
            return;
        }

        // Do not overwrite a movement decision already made by another controller.
        if (agent.isStopped && (!restoreStopPending || agent.hasPath))
            agent.isStopped = stoppedBeforeWait;

        ownsStop = false;
        restoreStopPending = false;
    }

    private void OnDisable()
    {
        ReleaseAllPassages();
    }

    private void OnDestroy()
    {
        ReleaseAllPassages();
    }
}

#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GamePlay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

public class GuardDoorNavigationTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private NavMeshData navigationData;
    private NavMeshDataInstance navigationInstance;
    private int previousCaptureFramerate;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return new EnterPlayMode();
        previousCaptureFramerate = Time.captureFramerate;
        // Multiple render updates per physics step exercise the interpolated hinge.
        Time.captureFramerate = 120;
        var floor = new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Box,
            size = new Vector3(12f, 1f, 12f),
            transform = Matrix4x4.TRS(new Vector3(0f, -0.5f, 0f), Quaternion.identity, Vector3.one),
            area = 0
        };
        navigationData = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),
            new List<NavMeshBuildSource> { floor }, new Bounds(Vector3.zero, new Vector3(20f, 10f, 20f)),
            Vector3.zero, Quaternion.identity);
        Assert.That(navigationData, Is.Not.Null);
        navigationInstance = NavMesh.AddNavMeshData(navigationData);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.captureFramerate = previousCaptureFramerate;
        for (int index = objects.Count - 1; index >= 0; index--)
        {
            if (objects[index] != null)
                Object.Destroy(objects[index]);
        }
        objects.Clear();
        yield return null;
        navigationInstance.Remove();
        if (navigationData != null)
            Object.Destroy(navigationData);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator MovingGuardOpensDoorAndRestoresLockAfterClearingIt()
    {
        Door door = CreateDoor();
        NavMeshAgent agent = CreateAgent(0.8f);
        Assert.That(agent.SetDestination(new Vector3(0f, 0f, 3f)), Is.True);

        bool sawOpenDoor = false;
        float deadline = Time.time + 6f;
        while (agent.transform.position.z < 1.3f && Time.time < deadline)
        {
            sawOpenDoor |= door.IsPassable;
            Assert.That(door.GetComponent<Collider>().enabled, Is.True,
                "Guard passage moves the physical leaf; its collider remains enabled.");
            if (sawOpenDoor && agent.transform.position.z > 0f && agent.transform.position.z <= 0.9f)
                Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Abierta),
                    "The whole physical body must clear the remembered doorway before closing.");
            yield return null;
        }

        Assert.That(sawOpenDoor, Is.True, "A real NavMesh path must open the interpolated hinge automatically.");
        Assert.That(agent.transform.position.z, Is.GreaterThan(1.2f), "The guard did not cross the doorway.");
        Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Bloqueada));
    }

    [UnityTest]
    public IEnumerator GuardWaitsForPhysicalOpeningThenContinuesThroughDoor()
    {
        Door door = CreateDoor(90f);
        NavMeshAgent agent = CreateAgent();
        GuardDoorPassage passage = agent.GetComponent<GuardDoorPassage>();
        Assert.That(agent.SetDestination(new Vector3(0f, 0f, 3f)), Is.True);

        float deadline = Time.time + 3f;
        while (!passage.IsWaitingForDoor && Time.time < deadline)
            yield return null;

        Assert.That(passage.IsWaitingForDoor, Is.True);
        Assert.That(agent.isStopped, Is.True);
        Vector3 waitingPosition = agent.transform.position;
        float holdUntil = Time.time + 0.15f;
        while (Time.time < holdUntil)
            yield return null;
        Assert.That(Vector3.Distance(agent.transform.position, waitingPosition), Is.LessThan(0.05f));

        deadline = Time.time + 6f;
        while (agent.transform.position.z < 1.3f && Time.time < deadline)
            yield return null;

        Assert.That(agent.transform.position.z, Is.GreaterThan(1.2f));
        Assert.That(passage.IsWaitingForDoor, Is.False);
        Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Bloqueada));
        Assert.That(door.GetComponent<Collider>().enabled, Is.True);
    }

    [UnityTest]
    public IEnumerator InitialVisionChaseCrossesInterpolatedDoor()
    {
        return AlertRouteCrossesDoor(true);
    }

    [UnityTest]
    public IEnumerator InitialSoundInvestigationCrossesInterpolatedDoor()
    {
        return AlertRouteCrossesDoor(false);
    }

    private IEnumerator AlertRouteCrossesDoor(bool chase)
    {
        Door door = CreateDoor();
        NavMeshAgent agent = CreateAgent();
        AI guard = agent.gameObject.AddComponent<AI>();
        guard.player = CreateObject("Detected player");
        guard.player.transform.position = new Vector3(0f, 0f, 3f);
        // Detection arrives before the first Start/Update, just as an initial alert can.
        if (chase)
            guard.SetPlayerDetected(true);
        else
            guard.RespondToSound(new Sound(guard.player.transform.position, 10f));

        bool sawWait = false;
        bool sawOpen = false;
        GuardDoorPassage passage = agent.GetComponent<GuardDoorPassage>();
        float deadline = Time.time + 6f;
        while (agent.transform.position.z < 2f && Time.time < deadline)
        {
            sawWait |= passage.IsWaitingForDoor;
            sawOpen |= door.IsPassable;
            yield return null;
        }

        Assert.That(sawWait, Is.True, "Initial detection must reserve the closed door.");
        Assert.That(sawOpen, Is.True, "The hinge must reach its open pose without a test completion callback.");
        Assert.That(agent.transform.position.z, Is.GreaterThanOrEqualTo(2f),
            "The alert route stranded the guard at the door.");
        Assert.That(passage.IsWaitingForDoor, Is.False);
    }

    [UnityTest]
    public IEnumerator DisablingWaitingGuardReleasesDoorAndRestoresMovement()
    {
        Door door = CreateDoor(90f);
        NavMeshAgent agent = CreateAgent();
        GuardDoorPassage passage = agent.GetComponent<GuardDoorPassage>();
        agent.SetDestination(new Vector3(0f, 0f, 3f));
        float deadline = Time.time + 3f;
        while (!passage.IsWaitingForDoor && Time.time < deadline)
            yield return null;
        Assert.That(passage.IsWaitingForDoor, Is.True);

        passage.enabled = false;
        Assert.That(passage.IsWaitingForDoor, Is.False);
        Assert.That(agent.isStopped, Is.False);
        Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Bloqueada));
        Assert.That(door.GetComponent<Collider>().enabled, Is.True);
    }

    [UnityTest]
    public IEnumerator DoorDisabledAndReenabledInSameFrameRestoresGuardReservation()
    {
        Door door = CreateDoor(90f);
        NavMeshAgent agent = CreateAgent();
        GuardDoorPassage passage = agent.GetComponent<GuardDoorPassage>();
        agent.SetDestination(new Vector3(0f, 0f, 3f));
        float deadline = Time.time + 3f;
        while (!passage.IsWaitingForDoor && Time.time < deadline)
            yield return null;
        Assert.That(passage.IsWaitingForDoor, Is.True);

        door.enabled = false;
        door.enabled = true;
        deadline = Time.time + 6f;
        while (agent.transform.position.z < 1.3f && Time.time < deadline)
            yield return null;

        Assert.That(agent.transform.position.z, Is.GreaterThan(1.2f),
            "A stale reservation must not strand the guard after the door is enabled again.");
        Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Bloqueada));
    }

    private Door CreateDoor(float degreesPerSecond = 240f)
    {
        GameObject root = CreateObject("Navigation test door");
        root.SetActive(false);
        GameObject hinge = CreateObject("Hinge");
        hinge.transform.SetParent(root.transform, false);
        hinge.transform.localPosition = Vector3.left;
        Rigidbody body = hinge.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        GameObject leaf = CreateObject("Leaf");
        leaf.transform.SetParent(hinge.transform, false);
        leaf.transform.localPosition = Vector3.right;
        BoxCollider collider = leaf.AddComponent<BoxCollider>();
        collider.center = Vector3.up;
        collider.size = new Vector3(2f, 2f, 0.2f);
        Door door = leaf.AddComponent<Door>();
        door.estadoActual = Door.EstadoPuerta.Bloqueada;
        typeof(Door).GetField("velocidadGuardaGrados", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(door, degreesPerSecond);
        root.SetActive(true);
        return door;
    }

    private NavMeshAgent CreateAgent(float bodyRadius = 0f)
    {
        GameObject instance = CreateObject("Navigation test guard");
        instance.transform.position = new Vector3(0f, 0f, -2f);
        if (bodyRadius > 0f)
        {
            CapsuleCollider body = instance.AddComponent<CapsuleCollider>();
            body.center = Vector3.up;
            body.radius = bodyRadius;
            body.height = 2f;
        }
        NavMeshAgent agent = instance.AddComponent<NavMeshAgent>();
        agent.radius = 0.5f;
        agent.height = 2f;
        agent.speed = 4f;
        agent.acceleration = 20f;
        agent.angularSpeed = 720f;
        Assert.That(agent.isOnNavMesh, Is.True);
        GuardDoorPassage.EnsureFor(agent);
        return agent;
    }

    private GameObject CreateObject(string name)
    {
        var instance = new GameObject(name);
        objects.Add(instance);
        return instance;
    }
}
#endif

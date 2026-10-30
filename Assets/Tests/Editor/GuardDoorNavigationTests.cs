#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

public class GuardDoorNavigationTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private NavMeshData navigationData;
    private NavMeshDataInstance navigationInstance;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return new EnterPlayMode();

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
        Door door = CreateDoor(false);
        NavMeshAgent agent = CreateAgent(0.8f);
        Assert.That(agent.SetDestination(new Vector3(0f, 0f, 3f)), Is.True);

        bool sawOpenDoor = false;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (agent.transform.position.z < 1.2f && Time.realtimeSinceStartup < deadline)
        {
            sawOpenDoor |= door.IsPassable;
            if (sawOpenDoor && agent.transform.position.z > 0f && agent.transform.position.z <= 0.9f)
                Assert.That(door.GetComponent<Collider>().enabled, Is.False,
                    "The physical body is wider than the navigation agent and must clear the doorway.");
            yield return null;
        }

        Assert.That(sawOpenDoor, Is.True, "A real NavMesh path must request passage automatically.");
        Assert.That(agent.transform.position.z, Is.GreaterThan(1.2f), "The guard did not cross the doorway.");
        Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Bloqueada));
        Assert.That(door.GetComponent<Collider>().enabled, Is.True);
    }

    [UnityTest]
    public IEnumerator GuardWaitsForOpeningEventThenContinuesThroughDoor()
    {
        Door door = CreateDoor(true);
        NavMeshAgent agent = CreateAgent();
        GuardDoorPassage passage = agent.GetComponent<GuardDoorPassage>();
        Assert.That(agent.SetDestination(new Vector3(0f, 0f, 3f)), Is.True);

        float deadline = Time.realtimeSinceStartup + 5f;
        while (!passage.IsWaitingForDoor && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(passage.IsWaitingForDoor, Is.True);
        Assert.That(agent.isStopped, Is.True);
        Assert.That(door.GetComponent<Collider>().enabled, Is.True);
        Vector3 waitingPosition = agent.transform.position;
        yield return new WaitForSeconds(0.15f);
        Assert.That(Vector3.Distance(agent.transform.position, waitingPosition), Is.LessThan(0.05f));

        door.NotificarPuertaAbierta();
        deadline = Time.realtimeSinceStartup + 8f;
        while (agent.transform.position.z < 1.2f && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(agent.transform.position.z, Is.GreaterThan(1.2f));
        Assert.That(passage.IsWaitingForDoor, Is.False);
        Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Bloqueada));
        Assert.That(door.GetComponent<Collider>().enabled, Is.False,
            "Closing waits for its animation event as well.");

        door.NotificarPuertaCerrada();
        Assert.That(door.GetComponent<Collider>().enabled, Is.True);
    }

    [UnityTest]
    public IEnumerator DisablingWaitingGuardReleasesDoorAndRestoresMovement()
    {
        Door door = CreateDoor(true);
        NavMeshAgent agent = CreateAgent();
        GuardDoorPassage passage = agent.GetComponent<GuardDoorPassage>();
        Assert.That(agent.SetDestination(new Vector3(0f, 0f, 3f)), Is.True);

        float deadline = Time.realtimeSinceStartup + 5f;
        while (!passage.IsWaitingForDoor && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(passage.IsWaitingForDoor, Is.True);

        passage.enabled = false;
        door.NotificarPuertaCerrada();

        Assert.That(passage.IsWaitingForDoor, Is.False);
        Assert.That(agent.isStopped, Is.False);
        Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Bloqueada));
        Assert.That(door.GetComponent<Collider>().enabled, Is.True);
    }

    [UnityTest]
    public IEnumerator DoorDisabledAndReenabledInSameFrameRestoresGuardReservation()
    {
        Door door = CreateDoor(true);
        NavMeshAgent agent = CreateAgent();
        GuardDoorPassage passage = agent.GetComponent<GuardDoorPassage>();
        Assert.That(agent.SetDestination(new Vector3(0f, 0f, 3f)), Is.True);

        float deadline = Time.realtimeSinceStartup + 5f;
        while (!passage.IsWaitingForDoor && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(passage.IsWaitingForDoor, Is.True);

        door.enabled = false;
        door.enabled = true;
        yield return null;
        yield return null;
        door.NotificarPuertaAbierta();

        deadline = Time.realtimeSinceStartup + 8f;
        while (agent.transform.position.z < 1.2f && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(agent.transform.position.z, Is.GreaterThan(1.2f),
            "A stale local reservation must not strand the guard after the door is enabled again.");
        door.NotificarPuertaCerrada();
        Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Bloqueada));
        Assert.That(door.GetComponent<Collider>().enabled, Is.True);
    }

    private Door CreateDoor(bool waitForAnimation)
    {
        GameObject instance = CreateObject("Navigation test door");
        instance.SetActive(false);
        BoxCollider collider = instance.AddComponent<BoxCollider>();
        collider.center = Vector3.up;
        collider.size = new Vector3(2f, 2f, 0.2f);
        Door door = instance.AddComponent<Door>();
        door.estadoActual = Door.EstadoPuerta.Bloqueada;
        typeof(Door).GetField("esperarEventosAnimacion", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(door, waitForAnimation);
        instance.SetActive(true);
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

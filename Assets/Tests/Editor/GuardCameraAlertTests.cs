#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GamePlay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

public class GuardCameraAlertTests
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
    public IEnumerator PlayerReportInterruptsSoundAndIgnoresFurtherSounds()
    {
        AI guard = CreateGuard();
        Vector3 soundPosition = new Vector3(-3f, 0f, -2f);
        guard.RespondToSound(new Sound(soundPosition, 10f));
        AssertDestination(guard, soundPosition);

        Vector3 report = new Vector3(3f, 0f, 3f);
        guard.ReportPlayerDetection(report);
        yield return null;
        yield return null;
        Assert.That(guard.IsInvestigatingPlayerReport, Is.True);
        AssertDestination(guard, report);

        guard.RespondToSound(new Sound(soundPosition, 10f));
        guard.SetPlayerDetected(false);
        yield return null;
        Assert.That(guard.IsInvestigatingPlayerReport, Is.True);
        AssertDestination(guard, report);
    }

    [UnityTest]
    public IEnumerator ConfirmedGuardVisionReplacesReportAndHasPriorityOverNewReports()
    {
        AI guard = CreateGuard();
        guard.player = CreateObject("Observed player");
        guard.player.transform.position = new Vector3(-3f, 0f, 2f);
        guard.ReportPlayerDetection(new Vector3(3f, 0f, 3f));
        yield return null;
        yield return null;

        guard.SetPlayerDetected(true);
        guard.ReportPlayerDetection(new Vector3(4f, 0f, 4f));
        yield return null;
        yield return null;

        Assert.That(guard.IsInvestigatingPlayerReport, Is.False);
        Assert.That(guard.IsChasingPlayer, Is.True);
        AssertDestination(guard, guard.player.transform.position);
    }

    [UnityTest]
    public IEnumerator ReportWaitsUntilNavigationAgentIsEnabled()
    {
        AI guard = CreateGuard();
        guard.navMeshAgent.enabled = false;
        Vector3 report = new Vector3(3f, 0f, 3f);
        guard.ReportPlayerDetection(report);
        yield return null;
        yield return null;
        Assert.That(guard.IsInvestigatingPlayerReport, Is.True);
        Assert.That(guard.LastKnownPlayerPosition, Is.EqualTo(report));

        guard.navMeshAgent.enabled = true;
        yield return null;
        yield return null;
        Assert.That(guard.IsInvestigatingPlayerReport, Is.True);
        AssertDestination(guard, report);
    }

    [UnityTest]
    public IEnumerator ArrivalCompletesReportAndResumesInterruptedPatrol()
    {
        AI guard = CreateGuard();
        GameObject waypoint = CreateObject("Patrol waypoint");
        waypoint.transform.position = new Vector3(-3f, 0f, -2f);
        guard.destinations = new[] { waypoint.transform };
        guard.distanceToFollowPath = 0.2f;
        guard.ReportPlayerDetection(new Vector3(0f, 0f, -0.5f));

        float deadline = Time.realtimeSinceStartup + 6f;
        while (guard.IsInvestigatingPlayerReport && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(guard.IsInvestigatingPlayerReport, Is.False);
        AssertDestination(guard, waypoint.transform.position);
    }

    [UnityTest]
    public IEnumerator UnreachableReportDoesNotLeaveGuardStuck()
    {
        AI guard = CreateGuard();
        GameObject waypoint = CreateObject("Patrol waypoint");
        waypoint.transform.position = new Vector3(-3f, 0f, -2f);
        guard.destinations = new[] { waypoint.transform };
        guard.ReportPlayerDetection(new Vector3(100f, 0f, 100f));
        yield return null;
        yield return null;

        Assert.That(guard.IsInvestigatingPlayerReport, Is.False);
        AssertDestination(guard, waypoint.transform.position);
    }

    [UnityTest]
    public IEnumerator ReportRouteWaitsForDoorAndContinuesAfterOpening()
    {
        GameObject doorObject = CreateObject("Alert route door");
        doorObject.SetActive(false);
        BoxCollider collider = doorObject.AddComponent<BoxCollider>();
        collider.center = Vector3.up;
        collider.size = new Vector3(2f, 2f, 0.2f);
        Door door = doorObject.AddComponent<Door>();
        door.estadoActual = Door.EstadoPuerta.Bloqueada;
        typeof(Door).GetField("esperarEventosAnimacion", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(door, true);
        doorObject.SetActive(true);

        AI guard = CreateGuard();
        GuardDoorPassage passage = guard.GetComponent<GuardDoorPassage>();
        guard.ReportPlayerDetection(new Vector3(0f, 0f, 3f));

        float deadline = Time.realtimeSinceStartup + 5f;
        while (!passage.IsWaitingForDoor && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(passage.IsWaitingForDoor, Is.True);
        // This suite is driven by the EditMode runner, which does not await WaitForSeconds.
        float holdUntil = Time.realtimeSinceStartup + 0.1f;
        while (Time.realtimeSinceStartup < holdUntil)
            yield return null;
        Assert.That(guard.IsInvestigatingPlayerReport, Is.True);
        Assert.That(guard.navMeshAgent.isStopped, Is.True);
        door.NotificarPuertaAbierta();

        deadline = Time.realtimeSinceStartup + 6f;
        while (guard.IsInvestigatingPlayerReport && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(guard.IsInvestigatingPlayerReport, Is.False);
        Assert.That(guard.transform.position.z, Is.GreaterThan(2f));
        Assert.That(passage.IsWaitingForDoor, Is.False);
    }

    private AI CreateGuard()
    {
        GameObject instance = CreateObject("Camera alert guard");
        instance.transform.position = new Vector3(0f, 0f, -2f);
        NavMeshAgent agent = instance.AddComponent<NavMeshAgent>();
        agent.speed = 4f;
        agent.acceleration = 20f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance = 0.2f;
        Assert.That(agent.isOnNavMesh, Is.True);
        return instance.AddComponent<AI>();
    }

    private static void AssertDestination(AI guard, Vector3 expected)
    {
        Assert.That(Vector3.Distance(guard.navMeshAgent.destination, expected), Is.LessThan(0.1f));
    }

    private GameObject CreateObject(string name)
    {
        var instance = new GameObject(name);
        objects.Add(instance);
        return instance;
    }
}
#endif

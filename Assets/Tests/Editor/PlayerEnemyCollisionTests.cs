#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

public class PlayerEnemyCollisionTests
{
    private static readonly Vector3 Origin = new Vector3(1000f, 0f, 1000f);
    private readonly List<GameObject> objects = new List<GameObject>();
    private NavMeshData navigationData;
    private NavMeshDataInstance navigationInstance;
    private float previousTimeScale;
    private bool previousAutoSync;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return new EnterPlayMode();
        previousTimeScale = Time.timeScale;
        previousAutoSync = Physics.autoSyncTransforms;
        Time.timeScale = 1f;
        Physics.autoSyncTransforms = false;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = previousTimeScale;
        Physics.autoSyncTransforms = previousAutoSync;
        Physics.IgnoreLayerCollision(29, 30, false);
        for (int index = objects.Count - 1; index >= 0; index--)
        {
            if (objects[index] != null)
                Object.Destroy(objects[index]);
        }
        objects.Clear();
        yield return null;
        if (navigationInstance.valid)
            navigationInstance.Remove();
        if (navigationData != null)
            Object.Destroy(navigationData);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator GuardReachingStationaryPlayerCapturesWithoutControllerMove()
    {
        PlayerEnemyCollision player = CreatePlayer();
        BoxCollider body = CreateGuard(new Vector3(3f, 0f, 0f));
        yield return null;
        Assert.That(player.MissionEndManager.MisionTerminada, Is.False);

        // No Rigidbody, CharacterController.Move ni SyncTransforms externo.
        body.transform.position = Origin + new Vector3(0.5f, 0f, 0f);
        yield return null;
        yield return null;

        AssertCaptured(player);
    }

    [UnityTest]
    public IEnumerator MovingPlayerIntoGuardStillCapturesImmediately()
    {
        PlayerEnemyCollision player = CreatePlayer();
        CreateGuard(new Vector3(2f, 0f, 0f));
        Physics.SyncTransforms();

        player.GetComponent<CharacterController>().Move(Vector3.right * 3f);

        AssertCaptured(player);
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshGuardCapturesPlayerWithMovementLocked()
    {
        var floor = new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Box,
            size = new Vector3(12f, 1f, 12f),
            transform = Matrix4x4.TRS(Origin + Vector3.down * 0.5f, Quaternion.identity, Vector3.one),
            area = 0
        };
        navigationData = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),
            new List<NavMeshBuildSource> { floor }, new Bounds(Origin, new Vector3(20f, 10f, 20f)),
            Vector3.zero, Quaternion.identity);
        Assert.That(navigationData, Is.Not.Null);
        navigationInstance = NavMesh.AddNavMeshData(navigationData);

        PlayerEnemyCollision player = CreatePlayer();
        PlayerController movement = player.gameObject.AddComponent<PlayerController>();
        movement.gravity = 0f;
        movement.SetMovementLocked(true);

        GameObject guard = CreateObject("Navigating guard", new Vector3(3f, 0f, 0f));
        BoxCollider body = guard.AddComponent<BoxCollider>();
        body.center = Vector3.up;
        body.size = new Vector3(0.8f, 2f, 0.6f);
        NavMeshAgent agent = guard.AddComponent<NavMeshAgent>();
        agent.speed = 3.5f;
        agent.acceleration = 20f;
        agent.stoppingDistance = 0f;
        Assert.That(agent.isOnNavMesh, Is.True);
        AI enemy = guard.AddComponent<AI>();
        enemy.player = player.gameObject;
        enemy.SetPlayerDetected(true);

        float deadline = Time.realtimeSinceStartup + 5f;
        while (!player.MissionEndManager.MisionTerminada && Time.realtimeSinceStartup < deadline)
            yield return null;

        AssertCaptured(player);
        Assert.That(Vector3.Distance(player.transform.position, Origin), Is.LessThan(0.01f));
    }

    [UnityTest]
    public IEnumerator ChildBodyColliderCapturesWithScaledOffsetPlayerCapsule()
    {
        PlayerEnemyCollision player = CreatePlayer();
        player.transform.localScale = new Vector3(2f, 1.5f, 2f);
        player.GetComponent<CharacterController>().center = new Vector3(0.5f, 0.9f, 0f);
        GameObject guard = CreateObject("Guard parent", new Vector3(1.7f, 0f, 0f));
        guard.AddComponent<AI>();
        GameObject child = CreateObject("Child body", Vector3.zero);
        child.transform.SetParent(guard.transform, false);
        child.transform.localPosition = Vector3.zero;
        BoxCollider body = child.AddComponent<BoxCollider>();
        body.center = Vector3.up;
        body.size = new Vector3(0.8f, 2f, 0.6f);

        yield return null;
        yield return null;

        AssertCaptured(player);
    }

    [UnityTest]
    public IEnumerator TriggerVolumesAndNonEnemyCollidersDoNotCapture()
    {
        PlayerEnemyCollision player = CreatePlayer();
        BoxCollider sensor = CreateGuard(Vector3.zero);
        sensor.isTrigger = true;
        CreateObject("Scenery", Vector3.zero).AddComponent<BoxCollider>();

        yield return null;
        yield return null;

        Assert.That(player.MissionEndManager.MisionTerminada, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [UnityTest]
    public IEnumerator NearbyGuardAcrossWallDoesNotCapture()
    {
        PlayerEnemyCollision player = CreatePlayer();
        CreateGuard(new Vector3(0.9f, 0f, 0f));
        BoxCollider wall = CreateObject("Thin wall", new Vector3(0.4f, 1f, 0f)).AddComponent<BoxCollider>();
        wall.size = new Vector3(0.1f, 2f, 2f);

        yield return null;
        yield return null;

        Assert.That(player.MissionEndManager.MisionTerminada, Is.False);
    }

    [UnityTest]
    public IEnumerator DisabledGuardDoesNotCaptureUntilEnabled()
    {
        PlayerEnemyCollision player = CreatePlayer();
        AI guard = CreateGuard(Vector3.zero).GetComponent<AI>();
        guard.enabled = false;
        yield return null;
        yield return null;
        Assert.That(player.MissionEndManager.MisionTerminada, Is.False);

        guard.enabled = true;
        yield return null;
        yield return null;
        AssertCaptured(player);
    }

    [UnityTest]
    public IEnumerator IgnoredCollisionPairAndLayersDoNotCapture()
    {
        PlayerEnemyCollision player = CreatePlayer();
        BoxCollider body = CreateGuard(Vector3.zero);
        CharacterController controller = player.GetComponent<CharacterController>();
        Physics.IgnoreCollision(controller, body);
        yield return null;
        yield return null;
        Assert.That(player.MissionEndManager.MisionTerminada, Is.False);

        Physics.IgnoreCollision(controller, body, false);
        player.gameObject.layer = 29;
        body.gameObject.layer = 30;
        Physics.IgnoreLayerCollision(29, 30);
        yield return null;
        yield return null;
        Assert.That(player.MissionEndManager.MisionTerminada, Is.False);

        Physics.IgnoreLayerCollision(29, 30, false);
        yield return null;
        yield return null;
        AssertCaptured(player);
    }

    [UnityTest]
    public IEnumerator ContactDoesNotReplaceCompletedVictoryOrCaptureWhilePaused()
    {
        PlayerEnemyCollision player = CreatePlayer();
        CreateGuard(Vector3.zero);
        Time.timeScale = 0f;
        yield return null;
        yield return null;
        Assert.That(player.MissionEndManager.MisionTerminada, Is.False);

        player.MissionEndManager.Victoria = true;
        player.MissionEndManager.MisionTerminada = true;
        Time.timeScale = 1f;
        yield return null;
        yield return null;

        Assert.That(player.MissionEndManager.Victoria, Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [UnityTest]
    public IEnumerator CrowdedContactQueryStillFindsGuard()
    {
        PlayerEnemyCollision player = CreatePlayer();
        for (int index = 0; index < 40; index++)
            CreateObject("Nearby collider " + index, Vector3.up).AddComponent<BoxCollider>();
        CreateGuard(new Vector3(0.5f, 0f, 0f));

        yield return null;
        yield return null;

        AssertCaptured(player);
    }

    [UnityTest]
    public IEnumerator AssignedMissionManagerIsPreservedAndMissingManagerDoesNotThrow()
    {
        PlayerEnemyCollision player = CreatePlayer();
        MissionEndManager assignedManager = player.MissionEndManager;
        CreateObject("Another mission", Vector3.zero).AddComponent<MissionEndManager>().enabled = false;
        yield return null;
        Assert.That(player.MissionEndManager, Is.SameAs(assignedManager));

        foreach (MissionEndManager manager in Object.FindObjectsByType<MissionEndManager>(FindObjectsSortMode.None))
            Object.Destroy(manager.gameObject);
        yield return null;
        CreateGuard(Vector3.zero);
        yield return null;
        yield return null;
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        LogAssert.NoUnexpectedReceived();
    }

    private PlayerEnemyCollision CreatePlayer()
    {
        GameObject instance = CreateObject("Player", Vector3.zero);
        CharacterController controller = instance.AddComponent<CharacterController>();
        controller.height = 1.75f;
        controller.radius = 0.3f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.skinWidth = 0.0001f;
        controller.minMoveDistance = 0f;
        controller.stepOffset = 0f;
        PlayerEnemyCollision contact = instance.AddComponent<PlayerEnemyCollision>();
        MissionEndManager manager = CreateObject("Mission", Vector3.zero).AddComponent<MissionEndManager>();
        manager.enabled = false;
        manager.Victoria = true;
        contact.MissionEndManager = manager;
        return contact;
    }

    private BoxCollider CreateGuard(Vector3 position)
    {
        GameObject instance = CreateObject("Guard", position);
        instance.AddComponent<AI>();
        BoxCollider body = instance.AddComponent<BoxCollider>();
        body.center = Vector3.up;
        body.size = new Vector3(0.8f, 2f, 0.6f);
        return body;
    }

    private GameObject CreateObject(string name, Vector3 position)
    {
        var instance = new GameObject(name);
        instance.transform.position = Origin + position;
        objects.Add(instance);
        return instance;
    }

    private static void AssertCaptured(PlayerEnemyCollision player)
    {
        Assert.That(player.MissionEndManager.MisionTerminada, Is.True);
        Assert.That(player.MissionEndManager.Victoria, Is.False);
        Assert.That(Time.timeScale, Is.Zero);
    }
}
#endif

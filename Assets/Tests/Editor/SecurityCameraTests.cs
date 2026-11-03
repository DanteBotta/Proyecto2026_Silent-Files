#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

public class SecurityCameraTests
{
    private const int TargetLayer = 30;
    private const int ObstacleLayer = 31;
    private static readonly Vector3 Origin = new Vector3(1000f, 0f, 1000f);
    private readonly List<GameObject> objects = new List<GameObject>();
    private NavMeshData navigationData;
    private NavMeshDataInstance navigationInstance;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return new EnterPlayMode();
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
        if (navigationInstance.valid)
            navigationInstance.Remove();
        if (navigationData != null)
            Object.Destroy(navigationData);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator DetectionUsesTheFirstFrameInsteadOfTheGuardPollingDelayOrSuspicion()
    {
        GameObject player = CreatePlayer(new Vector3(0f, 0f, -4f));
        SecurityCamera camera = CreateCamera(player);
        FieldOfView fieldOfView = camera.GetComponent<FieldOfView>();
        CameraVisual visual = camera.gameObject.AddComponent<CameraVisual>();
        fieldOfView.delay = 60f;
        int notifications = 0;
        camera.PlayerDetected.AddListener(_ => notifications++);
        yield return null;
        Assert.That(fieldOfView.canSeePlayer, Is.False);

        player.transform.position = Origin + new Vector3(0f, 0f, 4f);
        Physics.SyncTransforms();
        yield return null;

        Assert.That(fieldOfView.canSeePlayer, Is.True,
            "A camera must detect on its next frame, even with the old guard delay set to 60 seconds.");
        Assert.That(fieldOfView.suspicion, Is.Zero, "A camera has no progressive suspicion state.");
        Assert.That(notifications, Is.EqualTo(1));
        var properties = new MaterialPropertyBlock();
        visual.GeneratedRenderer.GetPropertyBlock(properties);
        Assert.That(properties.GetColor("_BaseColor"), Is.EqualTo(visual.DetectedColor));

        player.transform.position = Origin + new Vector3(0f, 0f, -4f);
        Physics.SyncTransforms();
        yield return null;
        Assert.That(fieldOfView.canSeePlayer, Is.False);
        visual.GeneratedRenderer.GetPropertyBlock(properties);
        Assert.That(properties.GetColor("_BaseColor"), Is.EqualTo(visual.NormalColor));
    }

    [UnityTest]
    public IEnumerator RadiusAngleAndSolidWallsBlockDetectionButTriggerVolumesDoNot()
    {
        GameObject player = CreatePlayer(new Vector3(0f, 0f, 4f));
        SecurityCamera camera = CreateCamera(player);
        FieldOfView fieldOfView = camera.GetComponent<FieldOfView>();
        yield return null;
        Assert.That(fieldOfView.canSeePlayer, Is.True);

        player.transform.position = Origin + new Vector3(0f, 0f, 12f);
        Physics.SyncTransforms();
        yield return null;
        Assert.That(fieldOfView.canSeePlayer, Is.False, "The configured radius must limit visibility.");

        player.transform.position = Origin + new Vector3(4f, 0f, 0f);
        Physics.SyncTransforms();
        yield return null;
        Assert.That(fieldOfView.canSeePlayer, Is.False, "The configured cone angle must limit visibility.");

        player.transform.position = Origin + new Vector3(0f, 0f, 4f);
        GameObject wall = CreateObject("Camera test wall", new Vector3(0f, 0f, 2f));
        wall.layer = ObstacleLayer;
        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = new Vector3(4f, 4f, 0.25f);
        Physics.SyncTransforms();
        yield return null;
        Assert.That(fieldOfView.canSeePlayer, Is.False, "A solid wall must block the line of sight.");

        collider.isTrigger = true;
        Physics.SyncTransforms();
        yield return null;
        Assert.That(fieldOfView.canSeePlayer, Is.True, "Trigger volumes must not act as walls.");

        collider.isTrigger = false;
        wall.layer = 0;
        Physics.SyncTransforms();
        yield return null;
        Assert.That(fieldOfView.canSeePlayer, Is.True, "Only the configured obstruction layers block vision.");
    }

    [UnityTest]
    public IEnumerator OnlyTheConfiguredPlayerAndTargetLayersAreDetectedIncludingChildColliders()
    {
        GameObject player = CreateObject("Camera test player root", new Vector3(0f, 0f, 4f));
        GameObject unrelated = CreatePlayer(new Vector3(0f, 0f, 2f));
        SecurityCamera camera = CreateCamera(player);
        FieldOfView fieldOfView = camera.GetComponent<FieldOfView>();
        yield return null;
        Assert.That(fieldOfView.canSeePlayer, Is.False,
            "A different object on the target layer must not be mistaken for the assigned player.");

        var body = new GameObject("Player child collider");
        body.transform.SetParent(player.transform, false);
        body.AddComponent<SphereCollider>().radius = 0.25f;
        body.layer = 0;
        Physics.SyncTransforms();
        yield return null;
        Assert.That(fieldOfView.canSeePlayer, Is.False);

        body.layer = TargetLayer;
        Physics.SyncTransforms();
        yield return null;
        Assert.That(fieldOfView.canSeePlayer, Is.True, "Child player colliders must be supported.");

        player.SetActive(false);
        Physics.SyncTransforms();
        yield return null;
        Assert.That(fieldOfView.canSeePlayer, Is.False, "An inactive player must immediately stop detection.");
        Assert.That(unrelated.activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator ContinuousVisibilitySendsOneSnapshotAndReacquisitionSendsANewOne()
    {
        GameObject player = CreatePlayer(new Vector3(0f, 0f, -4f));
        SecurityCamera camera = CreateCamera(player);
        var notifications = new List<Vector3>();
        camera.PlayerDetected.AddListener(notifications.Add);
        yield return null;

        Vector3 firstPosition = Origin + new Vector3(0f, 0f, 4f);
        player.transform.position = firstPosition;
        Physics.SyncTransforms();
        yield return null;
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0], Is.EqualTo(firstPosition));

        player.transform.position = Origin + new Vector3(1f, 0f, 4f);
        Physics.SyncTransforms();
        yield return null;
        yield return null;
        yield return null;
        Assert.That(notifications, Has.Count.EqualTo(1),
            "Moving inside the visible cone must not keep publishing the player's live position.");
        Assert.That(notifications[0], Is.EqualTo(firstPosition));

        player.transform.position = Origin + new Vector3(0f, 0f, -4f);
        Physics.SyncTransforms();
        yield return null;
        Vector3 secondPosition = Origin + new Vector3(2f, 0f, 4f);
        player.transform.position = secondPosition;
        Physics.SyncTransforms();
        yield return null;
        Assert.That(notifications, Has.Count.EqualTo(2));
        Assert.That(notifications[1], Is.EqualTo(secondPosition));
    }

    [UnityTest]
    public IEnumerator ReenablingTheCameraRefreshesVisibilityBeforeReportingAndCanDetectAgain()
    {
        GameObject player = CreatePlayer(new Vector3(0f, 0f, -4f));
        SecurityCamera camera = CreateCamera(player);
        FieldOfView fieldOfView = camera.GetComponent<FieldOfView>();
        var notifications = new List<Vector3>();
        camera.PlayerDetected.AddListener(notifications.Add);
        yield return null;

        player.transform.position = Origin + new Vector3(0f, 0f, 4f);
        Physics.SyncTransforms();
        yield return null;
        Assert.That(fieldOfView.canSeePlayer, Is.True);
        Assert.That(notifications, Has.Count.EqualTo(1));

        camera.enabled = false;
        player.transform.position = Origin + new Vector3(0f, 0f, -4f);
        Physics.SyncTransforms();
        camera.enabled = true;
        Assert.That(fieldOfView.canSeePlayer, Is.False,
            "Reactivation must refresh the previous visibility result before sending any report.");
        Assert.That(notifications, Has.Count.EqualTo(1),
            "A stale positive FOV result must not report a player who moved out while the camera was disabled.");
        yield return null;
        Assert.That(notifications, Has.Count.EqualTo(1));

        Vector3 secondPosition = Origin + new Vector3(1f, 0f, 4f);
        player.transform.position = secondPosition;
        Physics.SyncTransforms();
        yield return null;
        Assert.That(notifications, Has.Count.EqualTo(2));
        Assert.That(notifications[1], Is.EqualTo(secondPosition));
    }

    [UnityTest]
    public IEnumerator MultipleGuardsNavigateToTheReportedSnapshotWithoutTrackingLaterPlayerMovement()
    {
        BuildNavigation();
        GameObject player = CreatePlayer(new Vector3(0f, 0f, -4f));
        AI first = CreateGuard(player, new Vector3(-1f, 0f, -3f));
        AI second = CreateGuard(player, new Vector3(1f, 0f, -3f));
        SecurityCamera camera = CreateCamera(player, first, null, second, first);
        int notifications = 0;
        camera.PlayerDetected.AddListener(_ => notifications++);
        yield return null;

        Vector3 snapshot = Origin + new Vector3(0f, 0f, 4f);
        player.transform.position = snapshot;
        Physics.SyncTransforms();
        yield return null;
        Assert.That(notifications, Is.EqualTo(1));
        foreach (AI guard in new[] { first, second })
        {
            Assert.That(guard.IsInvestigatingPlayerReport, Is.True);
            Assert.That(guard.LastKnownPlayerPosition, Is.EqualTo(snapshot));
            Assert.That(Vector3.Distance(guard.navMeshAgent.destination, snapshot), Is.LessThan(0.2f));
            guard.SetPlayerDetected(false);
        }

        Vector3 firstStart = first.transform.position;
        Vector3 secondStart = second.transform.position;
        float firstStartDistance = Vector3.Distance(firstStart, snapshot);
        float secondStartDistance = Vector3.Distance(secondStart, snapshot);
        player.transform.position = Origin + new Vector3(2f, 0f, 4f);
        Physics.SyncTransforms();

        // This remains an EditMode IEnumerator after EnterPlayMode: its runner does not wait
        // for WaitForSeconds. Await actual navigation progress with a bounded frame loop.
        float deadline = Time.realtimeSinceStartup + 3f;
        while ((Vector3.Distance(first.transform.position, snapshot) > firstStartDistance - 0.25f ||
                Vector3.Distance(second.transform.position, snapshot) > secondStartDistance - 0.25f) &&
               Time.realtimeSinceStartup < deadline)
        {
            foreach (AI guard in new[] { first, second })
            {
                Assert.That(guard.IsInvestigatingPlayerReport, Is.True);
                Assert.That(Vector3.Distance(guard.navMeshAgent.destination, snapshot), Is.LessThan(0.2f));
            }
            yield return null;
        }

        foreach (AI guard in new[] { first, second })
        {
            Assert.That(guard.IsInvestigatingPlayerReport, Is.True,
                "The guard FOV resetting its own detection must not cancel a camera report.");
            Assert.That(guard.LastKnownPlayerPosition, Is.EqualTo(snapshot));
            Assert.That(Vector3.Distance(guard.navMeshAgent.destination, snapshot), Is.LessThan(0.2f),
                "followPlayer must not give a reported guard knowledge of later player movement.");
        }
        Assert.That(Vector3.Distance(first.transform.position, snapshot),
            Is.LessThanOrEqualTo(firstStartDistance - 0.25f),
            "The first guard must make real NavMesh progress toward the reported position within three seconds.");
        Assert.That(Vector3.Distance(second.transform.position, snapshot),
            Is.LessThanOrEqualTo(secondStartDistance - 0.25f),
            "The second guard must make real NavMesh progress toward the reported position within three seconds.");
        Assert.That(notifications, Is.EqualTo(1));
    }

    private SecurityCamera CreateCamera(GameObject player, params AI[] guards)
    {
        GameObject instance = CreateObject("Security camera test sensor", Vector3.zero);
        instance.SetActive(false);
        FieldOfView fieldOfView = instance.AddComponent<FieldOfView>();
        fieldOfView.radius = 8f;
        fieldOfView.angle = 90f;
        fieldOfView.playerRef = player;
        fieldOfView.targetMask = 1 << TargetLayer;
        fieldOfView.obstructionMask = 1 << ObstacleLayer;
        SecurityCamera camera = instance.AddComponent<SecurityCamera>();
        typeof(SecurityCamera).GetField("guards", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(camera, guards);
        Physics.SyncTransforms();
        instance.SetActive(true);
        return camera;
    }

    private GameObject CreatePlayer(Vector3 offset)
    {
        GameObject player = CreateObject("Camera test player", offset);
        player.layer = TargetLayer;
        player.AddComponent<SphereCollider>().radius = 0.25f;
        Physics.SyncTransforms();
        return player;
    }

    private AI CreateGuard(GameObject player, Vector3 offset)
    {
        GameObject instance = CreateObject("Camera test guard", offset);
        instance.SetActive(false);
        NavMeshAgent agent = instance.AddComponent<NavMeshAgent>();
        agent.speed = 2f;
        agent.acceleration = 40f;
        agent.angularSpeed = 720f;
        AI guard = instance.AddComponent<AI>();
        guard.navMeshAgent = agent;
        guard.player = player;
        guard.followPlayer = true;
        guard.distanceToFollowPlayer = 100f;
        instance.SetActive(true);
        Assert.That(agent.isOnNavMesh, Is.True);
        return guard;
    }

    private void BuildNavigation()
    {
        var floor = new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Box,
            size = new Vector3(24f, 1f, 24f),
            transform = Matrix4x4.TRS(Origin + Vector3.down * 0.5f, Quaternion.identity, Vector3.one),
            area = 0
        };
        navigationData = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),
            new List<NavMeshBuildSource> { floor }, new Bounds(Origin, new Vector3(30f, 10f, 30f)),
            Vector3.zero, Quaternion.identity);
        Assert.That(navigationData, Is.Not.Null);
        navigationInstance = NavMesh.AddNavMeshData(navigationData);
    }

    private GameObject CreateObject(string name, Vector3 offset)
    {
        var instance = new GameObject(name);
        instance.transform.position = Origin + offset;
        objects.Add(instance);
        return instance;
    }
}
#endif

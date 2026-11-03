#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CameraPresentationTests
{
    private const int TargetLayer = 30;
    private const int ObstacleLayer = 31;
    private static readonly Vector3 TestOrigin = new Vector3(1000f, 1000f, 1000f);
    private readonly List<GameObject> objects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int index = objects.Count - 1; index >= 0; index--)
        {
            if (objects[index] != null)
                Object.DestroyImmediate(objects[index]);
        }
        objects.Clear();
    }

    [Test]
    public void SweepReversesAtBothLimitsAndPreservesTheAuthoredOrientation()
    {
        Quaternion authoredRotation = Quaternion.Euler(15f, 65f, 7f);
        CameraRotation sweep = CreateSweep(authoredRotation);

        Advance(sweep, 1.5f);
        Assert.That(sweep.CurrentYawOffset, Is.EqualTo(45f).Within(0.001f));
        Assert.That(Quaternion.Angle(sweep.transform.localRotation,
            Quaternion.AngleAxis(45f, Vector3.up) * authoredRotation), Is.LessThan(0.01f));

        Advance(sweep, 3f);
        Assert.That(sweep.CurrentYawOffset, Is.EqualTo(-45f).Within(0.001f));
        Advance(sweep, 0.5f);
        Assert.That(sweep.CurrentYawOffset, Is.EqualTo(-30f).Within(0.001f));
    }

    [Test]
    public void SweepHasTheSameResultAcrossFrameRatesAndLargeFrameOvershoots()
    {
        CameraRotation oneFrame = CreateSweep(Quaternion.identity);
        CameraRotation manyFrames = CreateSweep(Quaternion.identity);
        Advance(oneFrame, 13f);
        for (int frame = 0; frame < 1300; frame++)
            Advance(manyFrames, 0.01f);

        Assert.That(oneFrame.CurrentYawOffset, Is.EqualTo(manyFrames.CurrentYawOffset).Within(0.001f));
        Assert.That(oneFrame.CurrentYawOffset, Is.InRange(-45f, 45f));
        Assert.That(Quaternion.Angle(oneFrame.transform.localRotation, manyFrames.transform.localRotation),
            Is.LessThan(0.01f));
    }

    [Test]
    public void SweepAcceptsReversedLimitsEqualLimitsAndZeroSpeed()
    {
        CameraRotation sweep = CreateSweep(Quaternion.identity);
        SetField(sweep, "minimumAngle", 30f);
        SetField(sweep, "maximumAngle", -30f);
        Advance(sweep, 1f);
        Assert.That(sweep.CurrentYawOffset, Is.EqualTo(30f).Within(0.001f));
        Advance(sweep, 1f);
        Assert.That(sweep.CurrentYawOffset, Is.EqualTo(0f).Within(0.001f));

        SetField(sweep, "rotationSpeed", 0f);
        Advance(sweep, 10f);
        Assert.That(sweep.CurrentYawOffset, Is.EqualTo(0f).Within(0.001f));
        SetField(sweep, "minimumAngle", 12f);
        SetField(sweep, "maximumAngle", 12f);
        Advance(sweep, 10f);
        Assert.That(sweep.CurrentYawOffset, Is.EqualTo(12f).Within(0.001f));
    }

    [Test]
    public void ConeMatchesFovGeometryAndRebuildsWhenInspectorValuesChange()
    {
        CameraVisual visual = CreateVisual(out FieldOfView fieldOfView);
        fieldOfView.radius = 7f;
        fieldOfView.angle = 90f;
        Invoke(visual, "LateUpdate");
        AssertCone(visual.GeneratedMesh, 7f, 90f);

        fieldOfView.radius = 3f;
        fieldOfView.angle = 120f;
        Invoke(visual, "LateUpdate");
        AssertCone(visual.GeneratedMesh, 3f, 120f);
        Assert.That(visual.GeneratedRenderer.sharedMaterial, Is.Not.Null);
        Assert.That(visual.GeneratedRenderer.sharedMaterial.renderQueue, Is.GreaterThanOrEqualTo(3000));
        Assert.That(visual.NormalColor.a, Is.InRange(0.01f, 0.99f));
        Assert.That(visual.DetectedColor.a, Is.InRange(0.01f, 0.99f));
    }

    [Test]
    public void ConeFollowsWorldRotationAndRadiusWithoutInheritedScaleOrPhysicsColliders()
    {
        CameraVisual visual = CreateVisual(out FieldOfView fieldOfView);
        fieldOfView.radius = 5f;
        GameObject owner = visual.gameObject;
        owner.transform.position = new Vector3(3f, 2f, 4f);
        owner.transform.rotation = Quaternion.Euler(12f, 87f, 5f);
        owner.transform.localScale = new Vector3(2f, 3f, 4f);
        Invoke(visual, "LateUpdate");

        Transform cone = visual.GeneratedRenderer.transform;
        Assert.That(Vector3.Distance(cone.position, owner.transform.position),
            Is.LessThan(0.0001f));
        Assert.That(Quaternion.Angle(cone.rotation, owner.transform.rotation), Is.LessThan(0.01f));
        foreach (Vector3 vertex in visual.GeneratedMesh.vertices)
        {
            if (vertex.sqrMagnitude > 0.000001f)
                Assert.That(Vector3.Distance(cone.TransformPoint(vertex), cone.position),
                    Is.EqualTo(5f).Within(0.001f));
        }
        Assert.That(cone.GetComponentsInChildren<Collider>(), Is.Empty);
        Assert.That(cone.gameObject.layer, Is.EqualTo(2));
    }

    [Test]
    public void WideWallClipsTheWholeVolumeAtItsNearSurface()
    {
        CameraVisual visual = CreateVisual(out FieldOfView fieldOfView);
        fieldOfView.radius = 7f;
        fieldOfView.angle = 90f;
        CreateObstacle(new Vector3(0f, 0f, 2.5f), new Vector3(20f, 20f, 0.4f));
        Refresh(visual);

        Assert.That(visual.GeneratedMesh.triangles.Length, Is.GreaterThan(0));
        float furthestForward = 0f;
        foreach (Vector3 vertex in visual.GeneratedMesh.vertices)
        {
            Assert.That(vertex.z, Is.LessThanOrEqualTo(2.301f),
                "The entire cone must stop before the wall, not just its central ray.");
            furthestForward = Mathf.Max(furthestForward, vertex.z);
        }
        Assert.That(furthestForward, Is.GreaterThan(1f), "Occlusion must retain the visible beam in front of the wall.");
        AssertNoOccludedTrianglePoints(visual);
    }

    [Test]
    public void ObliqueWallClipsTheVolumeWithoutLeavingTrianglesBehindItsSurface()
    {
        CameraVisual visual = CreateVisual(out FieldOfView fieldOfView);
        fieldOfView.radius = 7f;
        fieldOfView.angle = 90f;
        BoxCollider wall = CreateObstacle(new Vector3(0f, 0f, 2.5f), new Vector3(20f, 20f, 0.4f));
        wall.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
        Refresh(visual);

        Assert.That(visual.GeneratedMesh.triangles.Length, Is.GreaterThan(0));
        foreach (Vector3 vertex in visual.GeneratedMesh.vertices)
            Assert.That(vertex.magnitude, Is.LessThan(6.99f), "This oblique wall covers every ray in the cone.");
        AssertNoOccludedTrianglePoints(visual);
    }

    [TestCase(0f, 0f, 0.5f, 3f, 0.5f)]
    [TestCase(0.37f, 0.13f, 0.18f, 0.7f, 0.2f)]
    public void PartialObstacleLeavesLightOnBothSidesWithoutTrianglesBridgingItsShadow(
        float x, float y, float width, float height, float depth)
    {
        CameraVisual visual = CreateVisual(out FieldOfView fieldOfView);
        fieldOfView.radius = 7f;
        fieldOfView.angle = 90f;
        CreateObstacle(new Vector3(x, y, 3f), new Vector3(width, height, depth));
        Refresh(visual);

        Assert.That(visual.GeneratedMesh.bounds.min.x, Is.LessThan(-3f),
            "A partial obstacle must leave the left side of the cone visible.");
        Assert.That(visual.GeneratedMesh.bounds.max.x, Is.GreaterThan(3f),
            "A partial obstacle must leave the right side of the cone visible.");
        Assert.That(visual.GeneratedMesh.bounds.max.z, Is.GreaterThan(5f),
            "Unobstructed directions must keep their range.");
        AssertNoOccludedTrianglePoints(visual);
    }

    [Test]
    public void MovingDisablingOrRemovingWallsRefreshesGeometryWithoutRunningPlayerDetection()
    {
        CameraVisual visual = CreateVisual(out FieldOfView fieldOfView);
        fieldOfView.radius = 7f;
        fieldOfView.angle = 90f;
        GameObject player = CreateObject("Visual refresh player");
        player.transform.position = TestOrigin + Vector3.forward * 6f;
        player.layer = TargetLayer;
        player.AddComponent<SphereCollider>().radius = 0.2f;
        fieldOfView.playerRef = player;
        fieldOfView.targetMask = 1 << TargetLayer;
        Physics.SyncTransforms();
        fieldOfView.RefreshVisibility();
        Assert.That(fieldOfView.canSeePlayer, Is.True);
        int visibilityEvents = 0;
        fieldOfView.VisibilityChanged += _ => visibilityEvents++;

        BoxCollider wall = CreateObstacle(new Vector3(0f, 0f, 2.5f), new Vector3(20f, 20f, 0.4f));
        Refresh(visual);
        float firstExtent = visual.GeneratedMesh.bounds.max.z;
        Assert.That(firstExtent, Is.LessThan(2.31f));

        wall.transform.position += Vector3.forward * 1.5f;
        Refresh(visual);
        Assert.That(visual.GeneratedMesh.bounds.max.z, Is.GreaterThan(firstExtent + 1f));
        AssertNoOccludedTrianglePoints(visual);

        wall.enabled = false;
        Refresh(visual);
        AssertCone(visual.GeneratedMesh, 7f, 90f);
        wall.enabled = true;
        Refresh(visual);
        Assert.That(visual.GeneratedMesh.bounds.max.z, Is.LessThan(3.81f));

        Object.DestroyImmediate(wall.gameObject);
        Refresh(visual);
        AssertCone(visual.GeneratedMesh, 7f, 90f);
        Assert.That(fieldOfView.canSeePlayer, Is.True,
            "Updating visual geometry must not recalculate the FOV's player visibility.");
        Assert.That(visibilityEvents, Is.Zero, "Visual physics queries must not publish detection events.");
    }

    [Test]
    public void VisualOcclusionUsesOnlySolidCollidersOnTheFovObstructionMask()
    {
        CameraVisual visual = CreateVisual(out FieldOfView fieldOfView);
        fieldOfView.radius = 7f;
        fieldOfView.angle = 90f;
        BoxCollider wall = CreateObstacle(new Vector3(0f, 0f, 2.5f), new Vector3(20f, 20f, 0.4f));
        wall.isTrigger = true;
        Refresh(visual);
        AssertCone(visual.GeneratedMesh, 7f, 90f);

        wall.isTrigger = false;
        wall.gameObject.layer = TargetLayer;
        Refresh(visual);
        AssertCone(visual.GeneratedMesh, 7f, 90f);

        fieldOfView.obstructionMask = 1 << TargetLayer;
        Refresh(visual);
        Assert.That(visual.GeneratedMesh.bounds.max.z, Is.LessThan(2.31f),
            "Changing the shared FOV mask must affect visual clipping on the next refresh.");
    }

    [TestCase(0f)]
    [TestCase(180f)]
    [TestCase(360f)]
    public void BoundaryAnglesProduceFiniteGeometryAndZeroAngleHidesTheRenderer(float angle)
    {
        CameraVisual visual = CreateVisual(out FieldOfView fieldOfView);
        fieldOfView.radius = 7f;
        fieldOfView.angle = angle;
        Refresh(visual);

        Mesh mesh = visual.GeneratedMesh;
        Assert.That(mesh, Is.Not.Null);
        Vector3[] vertices = mesh.vertices;
        foreach (Vector3 vertex in vertices)
        {
            Assert.That(float.IsNaN(vertex.x) || float.IsInfinity(vertex.x), Is.False);
            Assert.That(float.IsNaN(vertex.y) || float.IsInfinity(vertex.y), Is.False);
            Assert.That(float.IsNaN(vertex.z) || float.IsInfinity(vertex.z), Is.False);
        }
        foreach (int index in mesh.triangles)
            Assert.That(index, Is.InRange(0, vertices.Length - 1));
        Assert.That(visual.GeneratedRenderer.enabled, Is.EqualTo(angle > 0f));
        if (angle > 0f)
            AssertCone(mesh, 7f, angle);
        if (angle == 360f)
            Assert.That(mesh.bounds.min.z, Is.LessThan(-6.99f), "A 360 degree FOV includes the rear hemisphere.");
    }

    [Test]
    public void ConeUpdatesColorsFromVisibilityEventsAndReleasesTemporaryObjectsWhenDisabled()
    {
        CameraVisual visual = CreateVisual(out FieldOfView fieldOfView);
        var properties = new MaterialPropertyBlock();
        visual.GeneratedRenderer.GetPropertyBlock(properties);
        Assert.That(properties.GetColor("_BaseColor"), Is.EqualTo(visual.NormalColor));

        // Use the FOV event publisher; no visual LateUpdate is needed for the immediate color change.
        Invoke(fieldOfView, "SetVisibility", true);
        visual.GeneratedRenderer.GetPropertyBlock(properties);
        Assert.That(properties.GetColor("_BaseColor"), Is.EqualTo(visual.DetectedColor));
        Invoke(fieldOfView, "SetVisibility", false);
        visual.GeneratedRenderer.GetPropertyBlock(properties);
        Assert.That(properties.GetColor("_BaseColor"), Is.EqualTo(visual.NormalColor));

        Mesh previousMesh = visual.GeneratedMesh;
        MeshRenderer previousRenderer = visual.GeneratedRenderer;
        visual.enabled = false;
        Assert.That(visual.GeneratedMesh, Is.Null);
        Assert.That(visual.GeneratedRenderer, Is.Null);
        Assert.That(previousMesh == null, Is.True, "The temporary procedural mesh must be destroyed.");
        Assert.That(previousRenderer == null, Is.True, "The temporary visual object must be destroyed.");
        visual.enabled = true;
        Assert.That(visual.GeneratedMesh, Is.Not.Null);
        Assert.That(visual.GeneratedRenderer, Is.Not.Null);
    }

    private CameraRotation CreateSweep(Quaternion rotation)
    {
        GameObject instance = CreateObject("Camera sweep test");
        instance.transform.localRotation = rotation;
        CameraRotation sweep = instance.AddComponent<CameraRotation>();
        Invoke(sweep, "Awake");
        return sweep;
    }

    private CameraVisual CreateVisual(out FieldOfView fieldOfView)
    {
        GameObject instance = CreateObject("Camera cone test");
        instance.transform.position = TestOrigin;
        fieldOfView = instance.AddComponent<FieldOfView>();
        fieldOfView.UseInstantDetection();
        fieldOfView.obstructionMask = 1 << ObstacleLayer;
        return instance.AddComponent<CameraVisual>();
    }

    private BoxCollider CreateObstacle(Vector3 offset, Vector3 size)
    {
        GameObject instance = CreateObject("Visual occlusion obstacle");
        instance.transform.position = TestOrigin + offset;
        instance.layer = ObstacleLayer;
        BoxCollider obstacle = instance.AddComponent<BoxCollider>();
        obstacle.size = size;
        return obstacle;
    }

    private GameObject CreateObject(string name)
    {
        var instance = new GameObject(name);
        objects.Add(instance);
        return instance;
    }

    private static void AssertCone(Mesh mesh, float radius, float angle)
    {
        Assert.That(mesh, Is.Not.Null);
        Vector3[] vertices = mesh.vertices;
        Assert.That(vertices.Length, Is.GreaterThanOrEqualTo(5));
        Assert.That(vertices[0], Is.EqualTo(Vector3.zero));
        float minimumY = 0f;
        float maximumY = 0f;
        float greatestAngle = 0f;
        foreach (Vector3 vertex in vertices)
        {
            minimumY = Mathf.Min(minimumY, vertex.y);
            maximumY = Mathf.Max(maximumY, vertex.y);
            if (vertex.sqrMagnitude <= 0.000001f)
                continue;

            Assert.That(vertex.magnitude, Is.EqualTo(radius).Within(0.001f));
            float vertexAngle = Vector3.Angle(Vector3.forward, vertex);
            Assert.That(vertexAngle, Is.LessThanOrEqualTo(angle * 0.5f + 0.02f));
            greatestAngle = Mathf.Max(greatestAngle, vertexAngle);
        }
        Assert.That(greatestAngle, Is.EqualTo(angle * 0.5f).Within(0.02f));
        Assert.That(minimumY, Is.LessThan(-radius * 0.1f), "The cone must extend below its forward axis.");
        Assert.That(maximumY, Is.GreaterThan(radius * 0.1f), "The cone must extend above its forward axis.");
        Assert.That(mesh.triangles.Length, Is.GreaterThan(0));
        Assert.That(mesh.triangles.Length % 3, Is.Zero);
    }

    private static void Refresh(CameraVisual visual)
    {
        Physics.SyncTransforms();
        Invoke(visual, "RefreshVisual");
    }

    private static void AssertNoOccludedTrianglePoints(CameraVisual visual)
    {
        // Checking only vertices misses triangles bridging a pillar's shadow. Sample edges and
        // interiors as well, independently from the implementation's sampling directions.
        const int subdivisions = 8;
        Mesh mesh = visual.GeneratedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Transform cone = visual.GeneratedRenderer.transform;
        Vector3 origin = visual.transform.position;
        int mask = visual.GetComponent<FieldOfView>().obstructionMask;
        for (int triangle = 0; triangle < triangles.Length; triangle += 3)
        {
            Vector3 a = cone.TransformPoint(vertices[triangles[triangle]]);
            Vector3 b = cone.TransformPoint(vertices[triangles[triangle + 1]]);
            Vector3 c = cone.TransformPoint(vertices[triangles[triangle + 2]]);
            for (int first = 0; first <= subdivisions; first++)
            {
                for (int second = 0; second <= subdivisions - first; second++)
                {
                    float u = first / (float)subdivisions;
                    float v = second / (float)subdivisions;
                    Vector3 point = a * (1f - u - v) + b * u + c * v;
                    Vector3 ray = point - origin;
                    float distance = ray.magnitude;
                    if (distance <= 0.002f)
                        continue;

                    if (Physics.Raycast(origin, ray / distance, out RaycastHit hit, distance - 0.001f,
                        mask, QueryTriggerInteraction.Ignore))
                    {
                        Assert.Fail("Triangle {0} contains a visible point behind {1}: beam distance {2}, wall distance {3}.",
                            triangle / 3, hit.collider.name, distance, hit.distance);
                    }
                }
            }
        }
    }

    private static void Advance(CameraRotation sweep, float deltaTime)
    {
        Invoke(sweep, "AdvanceRotation", deltaTime);
    }

    private static void SetField(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static void Invoke(object target, string method, params object[] arguments)
    {
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
    }
}
#endif

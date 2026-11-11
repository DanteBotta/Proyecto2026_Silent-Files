using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Displays the existing FieldOfView as a clipped 3D volume. Obstacle queries are
/// shared with FieldOfView; player detection and guard notifications remain there.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(60)]
[DisallowMultipleComponent]
[RequireComponent(typeof(FieldOfView))]
public class CameraVisual : MonoBehaviour
{
    private const string DefaultMaterialPath = "SecurityCamera/Cone";
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [SerializeField] private Color normalColor = new Color(1f, 0.85f, 0.12f, 0.3f);
    [SerializeField] private Color detectedColor = new Color(1f, 0.08f, 0.05f, 0.45f);
    [Tooltip("Optional transparent material supporting _BaseColor or _Color. Empty uses the included URP material.")]
    [SerializeField] private Material coneMaterial;
    [Tooltip("Samples around the circular cone. More samples improve shadow outlines at a higher physics cost.")]
    [SerializeField, Range(3, 256)] private int arcSegments = 64;
    [Tooltip("Samples from the cone axis to its edge. Interior samples let small objects block part of the beam.")]
    [SerializeField, Range(2, 32)] private int radialSegments = 12;
    [Tooltip("Distance before solid surfaces where the beam stops, avoiding intersection and flickering.")]
    [SerializeField, Min(0f)] private float obstructionPadding = 0.02f;

    private FieldOfView fieldOfView;
    private GameObject visualObject;
    private Mesh coneMesh;
    private MeshRenderer coneRenderer;
    private Material defaultMaterial;
    private MaterialPropertyBlock properties;
    private float builtAngle = -1f;
    private int builtSegments = -1;
    private int builtRadialSegments = -1;
    private bool warnedMissingMaterial;

    // Topology and query buffers are rebuilt only when resolution/angle changes.
    // Animation and moving walls update these arrays without per-frame allocations.
    private Vector3[] directions;
    private Vector3[] vertices;
    private float[] distances;
    private float[] clippedDistances;
    private Collider[] hitColliders;
    private Vector3[] hitNormals;
    private Vector3[] hitPoints;
    private int[] surfaceTriangles;
    private Vector3[] patchDirections;
    private float[] patchWidths;

    public Color NormalColor => normalColor;
    public Color DetectedColor => detectedColor;
    public Color CurrentColor => fieldOfView != null && fieldOfView.isActiveAndEnabled && fieldOfView.canSeePlayer
        ? detectedColor : normalColor;
    public Mesh GeneratedMesh => coneMesh;
    public MeshRenderer GeneratedRenderer => coneRenderer;

    private void OnEnable()
    {
        fieldOfView = GetComponent<FieldOfView>();
        fieldOfView.VisibilityChanged += OnVisibilityChanged;
        RefreshVisual();
    }

    private void LateUpdate()
    {
        // ExecuteAlways also refreshes Inspector edits in Scene View without entering Play Mode.
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (fieldOfView == null)
            return;

        EnsureVisual();
        if (coneRenderer == null)
            return;

        // A separate, temporary transform avoids inheriting non-uniform model scale: FOV radius
        // is measured in world units. Moving it to the owner's scene also supports prefab previews.
        // The origin is exactly the detection origin; translating after clipping would leak
        // through nearby walls. Place the sensor pivot at the camera's lens instead.
        visualObject.transform.SetPositionAndRotation(transform.position, transform.rotation);

        float radius = Mathf.Max(0f, fieldOfView.radius);
        float angle = Mathf.Clamp(fieldOfView.angle, 0f, 360f);
        int segments = Mathf.Clamp(arcSegments, 3, 256);
        int rings = Mathf.Clamp(radialSegments, 2, 32);
        if (builtAngle != angle || builtSegments != segments || builtRadialSegments != rings)
            RebuildTopology(angle, segments, rings);

        UpdateMesh(radius, angle);

        if (coneMaterial == null && defaultMaterial == null)
            defaultMaterial = Resources.Load<Material>(DefaultMaterialPath);

        Material material = coneMaterial != null ? coneMaterial : defaultMaterial;
        coneRenderer.sharedMaterial = material;
        coneRenderer.enabled = fieldOfView.isActiveAndEnabled && radius > 0f && angle > 0f && material != null;
        if (material == null && !warnedMissingMaterial)
        {
            warnedMissingMaterial = true;
            Debug.LogWarning("CameraVisual needs the included Resources/SecurityCamera/Cone material or a transparent material override.", this);
        }

        ApplyColor();
    }

    private void EnsureVisual()
    {
        if (visualObject != null)
            return;

        visualObject = new GameObject(name + " (Security Camera Vision)");
        visualObject.hideFlags = HideFlags.HideAndDontSave;
        visualObject.layer = 2; // Unity's built-in Ignore Raycast layer; no Collider is ever created.
        if (gameObject.scene.IsValid() && gameObject.scene.isLoaded)
            SceneManager.MoveGameObjectToScene(visualObject, gameObject.scene);

        coneMesh = new Mesh { name = "Security Camera Vision Cone", hideFlags = HideFlags.HideAndDontSave };
        coneMesh.MarkDynamic();
        visualObject.AddComponent<MeshFilter>().sharedMesh = coneMesh;
        coneRenderer = visualObject.AddComponent<MeshRenderer>();
        coneRenderer.shadowCastingMode = ShadowCastingMode.Off;
        coneRenderer.receiveShadows = false;
        coneRenderer.lightProbeUsage = LightProbeUsage.Off;
        coneRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        coneRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        properties = new MaterialPropertyBlock();
        builtAngle = -1f;
        builtSegments = -1;
        builtRadialSegments = -1;
    }

    private void RebuildTopology(float angle, int segments, int rings)
    {
        bool fullSphere = angle >= 360f;
        int regularRings = fullSphere ? rings - 1 : rings;
        int vertexCount = 2 + regularRings * segments + (fullSphere ? 1 : 0);
        directions = new Vector3[vertexCount];
        vertices = new Vector3[vertexCount];
        distances = new float[vertexCount];
        clippedDistances = new float[vertexCount];
        hitColliders = new Collider[vertexCount];
        hitNormals = new Vector3[vertexCount];
        hitPoints = new Vector3[vertexCount];
        var uv = new Vector2[vertexCount];
        uv[0] = new Vector2(0.5f, 0.5f);
        directions[1] = Vector3.forward;
        uv[1] = new Vector2(0.5f, 0.5f);

        // FOV uses Vector3.Angle and distance from its origin, so its outer surface
        // is a spherical cap, rather than a flat disk at a constant forward distance.
        float halfAngle = angle * 0.5f * Mathf.Deg2Rad;
        for (int ring = 1; ring <= regularRings; ring++)
        {
            float polarAngle = halfAngle * ring / rings;
            float radial = Mathf.Sin(polarAngle);
            float forward = Mathf.Cos(polarAngle);
            for (int segment = 0; segment < segments; segment++)
            {
                float azimuth = 2f * Mathf.PI * segment / segments;
                int index = 2 + (ring - 1) * segments + segment;
                directions[index] = new Vector3(radial * Mathf.Cos(azimuth),
                    radial * Mathf.Sin(azimuth), forward);
                uv[index] = new Vector2(directions[index].x * 0.5f + 0.5f,
                    directions[index].y * 0.5f + 0.5f);
            }
        }

        var triangles = new List<int>(rings * segments * 6);
        for (int segment = 0; segment < segments; segment++)
        {
            int next = (segment + 1) % segments;
            AddTriangle(triangles, 1, 2 + segment, 2 + next);
            for (int ring = 1; ring < regularRings; ring++)
            {
                int inner = 2 + (ring - 1) * segments;
                int outer = inner + segments;
                AddTriangle(triangles, inner + segment, outer + segment, outer + next);
                AddTriangle(triangles, inner + segment, outer + next, inner + next);
            }
        }

        int lastRing = 2 + (regularRings - 1) * segments;
        if (fullSphere)
        {
            int backPole = vertexCount - 1;
            directions[backPole] = Vector3.back;
            uv[backPole] = new Vector2(0.5f, 0.5f);
            for (int segment = 0; segment < segments; segment++)
                AddTriangle(triangles, lastRing + segment, backPole,
                    lastRing + (segment + 1) % segments);
        }

        surfaceTriangles = triangles.ToArray();
        patchDirections = new Vector3[surfaceTriangles.Length / 3];
        patchWidths = new float[patchDirections.Length];
        for (int patch = 0; patch < patchDirections.Length; patch++)
        {
            Vector3 a = directions[surfaceTriangles[patch * 3]];
            Vector3 b = directions[surfaceTriangles[patch * 3 + 1]];
            Vector3 c = directions[surfaceTriangles[patch * 3 + 2]];
            Vector3 center = (a + b + c).normalized;
            patchDirections[patch] = center;
            float minimumDot = Mathf.Clamp01(Mathf.Min(Vector3.Dot(center, a),
                Mathf.Min(Vector3.Dot(center, b), Vector3.Dot(center, c))));
            patchWidths[patch] = Mathf.Sqrt(Mathf.Max(0f, 1f - minimumDot * minimumDot));
        }

        // Close the cone's sides. A 360-degree FOV is a complete sphere and has
        // neither an outside ring nor side faces back to the origin.
        if (!fullSphere)
            for (int segment = 0; segment < segments; segment++)
                AddTriangle(triangles, 0, lastRing + (segment + 1) % segments, lastRing + segment);

        coneMesh.Clear();
        coneMesh.vertices = vertices;
        coneMesh.normals = directions;
        coneMesh.uv = uv;
        coneMesh.SetTriangles(triangles, 0);
        builtAngle = angle;
        builtSegments = segments;
        builtRadialSegments = rings;
    }

    private static void AddTriangle(List<int> triangles, int a, int b, int c)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    private void UpdateMesh(float radius, float angle)
    {
        bool queryObstacles = radius > 0f && angle > 0f && fieldOfView.obstructionMask.value != 0;
        if (queryObstacles)
            Physics.SyncTransforms();

        Quaternion rotation = transform.rotation;
        float padding = Mathf.Max(0f, obstructionPadding);
        for (int index = 1; index < directions.Length; index++)
        {
            distances[index] = radius;
            hitColliders[index] = null;
            if (queryObstacles && fieldOfView.TryGetObstruction(rotation * directions[index], radius, out RaycastHit hit))
            {
                distances[index] = Mathf.Max(0f, hit.distance - padding);
                hitColliders[index] = hit.collider;
                hitNormals[index] = hit.normal;
                hitPoints[index] = hit.point;
            }
            clippedDistances[index] = distances[index];
        }

        if (queryObstacles)
        {
            for (int patch = 0; patch < patchDirections.Length; patch++)
            {
                int a = surfaceTriangles[patch * 3];
                int b = surfaceTriangles[patch * 3 + 1];
                int c = surfaceTriangles[patch * 3 + 2];
                Vector3 direction = rotation * patchDirections[patch];
                bool centerBlocked = fieldOfView.TryGetObstruction(direction, radius, out RaycastHit centerHit);
                bool sweepBlocked = fieldOfView.TryGetObstruction(direction, radius, out RaycastHit sweepHit,
                    radius * patchWidths[patch]);

                // Exact ray endpoints on one plane already form an unobstructed
                // triangle. Keep the beam flush with broad, flat walls.
                Collider collider = hitColliders[a];
                bool samePlane = collider != null && collider == hitColliders[b] && collider == hitColliders[c] &&
                    centerBlocked && centerHit.collider == collider && sweepBlocked && sweepHit.collider == collider &&
                    Vector3.Dot(hitNormals[a], hitNormals[b]) > 0.9999f &&
                    Vector3.Dot(hitNormals[a], hitNormals[c]) > 0.9999f &&
                    Vector3.Dot(hitNormals[a], centerHit.normal) > 0.9999f &&
                    Vector3.Dot(hitNormals[a], sweepHit.normal) > 0.9999f &&
                    IsOnPlane(hitPoints[b], hitPoints[a], hitNormals[a]) &&
                    IsOnPlane(hitPoints[c], hitPoints[a], hitNormals[a]) &&
                    IsOnPlane(centerHit.point, hitPoints[a], hitNormals[a]) &&
                    IsOnPlane(sweepHit.point, hitPoints[a], hitNormals[a]);
                if (samePlane)
                    continue;

                float limit = Mathf.Min(distances[a], Mathf.Min(distances[b], distances[c]));
                if (centerBlocked)
                    limit = Mathf.Min(limit, Mathf.Max(0f, centerHit.distance - padding));
                if (sweepBlocked)
                    limit = Mathf.Min(limit, Mathf.Max(0f, sweepHit.distance - padding));

                // The swept patch covers the space between its rays, so even a
                // narrow pillar cannot leave a long triangle behind it. Shadows
                // are deliberately conservative at silhouettes; more segments
                // tighten their outline. These are visual queries, not detection.
                clippedDistances[a] = Mathf.Min(clippedDistances[a], limit);
                clippedDistances[b] = Mathf.Min(clippedDistances[b], limit);
                clippedDistances[c] = Mathf.Min(clippedDistances[c], limit);
            }
        }

        vertices[0] = Vector3.zero;
        for (int index = 1; index < directions.Length; index++)
            vertices[index] = directions[index] * clippedDistances[index];

        coneMesh.vertices = vertices;
        coneMesh.RecalculateBounds();
    }

    private static bool IsOnPlane(Vector3 point, Vector3 origin, Vector3 normal)
    {
        // A MeshCollider can contain several parallel surfaces: sharing a normal
        // and collider alone does not mean the contacts belong to the same plane.
        return Mathf.Abs(Vector3.Dot(point - origin, normal)) < 0.001f;
    }

    private void OnVisibilityChanged(bool visible)
    {
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (coneRenderer == null || properties == null)
            return;

        properties.SetColor(BaseColorId, CurrentColor);
        properties.SetColor(ColorId, CurrentColor);
        coneRenderer.SetPropertyBlock(properties);
    }

    private void OnDisable()
    {
        if (fieldOfView != null)
            fieldOfView.VisibilityChanged -= OnVisibilityChanged;

        ReleaseVisual();
    }

    private void OnDestroy()
    {
        ReleaseVisual();
    }

    private void ReleaseVisual()
    {
        // Disable before deferred destruction so a disable/enable in one frame cannot double-render.
        if (coneRenderer != null)
            coneRenderer.enabled = false;

        DestroyTemporary(coneMesh);
        DestroyTemporary(visualObject);
        coneMesh = null;
        visualObject = null;
        coneRenderer = null;
        properties = null;
    }

    private void DestroyTemporary(Object value)
    {
        if (value == null)
            return;

        if (Application.IsPlaying(gameObject))
            Destroy(value);
        else
            DestroyImmediate(value);
    }
}

#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GamePlay;
using NUnit.Framework;
using UnityEditor;
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
    public IEnumerator ConfirmedVisionRunsAndLosingDetectionRestoresPatrolSpeed()
    {
        AI guard = CreateGuard();
        float patrolSpeed = guard.navMeshAgent.speed;
        guard.player = CreateObject("Visible player");
        guard.player.transform.position = new Vector3(0f, 0f, 5f);
        GameObject waypoint = CreateObject("Patrol waypoint");
        waypoint.transform.position = new Vector3(-3f, 0f, -2f);
        guard.destinations = new[] { waypoint.transform };

        guard.SetPlayerDetected(true);
        float deadline = Time.realtimeSinceStartup + 3f;
        while (guard.navMeshAgent.velocity.magnitude <= patrolSpeed && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(guard.navMeshAgent.speed, Is.EqualTo(guard.chaseSpeed));
        Assert.That(guard.navMeshAgent.velocity.magnitude, Is.GreaterThan(patrolSpeed),
            "Confirmed vision must make the guard actually move faster than its patrol.");

        guard.SetPlayerDetected(false);
        yield return null;
        yield return null;
        Assert.That(guard.navMeshAgent.speed, Is.EqualTo(patrolSpeed));
        AssertDestination(guard, waypoint.transform.position);

        guard.ReportPlayerDetection(new Vector3(3f, 0f, 3f));
        yield return null;
        yield return null;
        Assert.That(guard.navMeshAgent.speed, Is.EqualTo(patrolSpeed),
            "A camera report must not give the guard direct-vision running speed.");

        guard.SetPlayerDetected(true);
        yield return null;
        yield return null;
        Assert.That(guard.navMeshAgent.speed, Is.EqualTo(guard.chaseSpeed));
        guard.enabled = false;
        Assert.That(guard.navMeshAgent.speed, Is.EqualTo(patrolSpeed));
    }

    [UnityTest]
    public IEnumerator GuardAnimatorsSwitchBetweenWalkingRunningAndIdle()
    {
        foreach (string controllerName in new[] { "GuardiaGrandeAnimator", "VillanoCasaAnimator" })
        {
            string modelName = controllerName.Replace("Animator", "");
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Models/PersonajesModels/" + modelName + ".fbx");
            Assert.That(model, Is.Not.Null);
            GameObject instance = Object.Instantiate(model);
            objects.Add(instance);
            Animator animator = instance.GetComponent<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Models/Animators/" + controllerName + ".controller");
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            AnimationClip runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/Models/Model Animations/Run_Player.anim");
            Assert.That(runClip.isLooping, Is.True);
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(runClip))
                if (!string.IsNullOrEmpty(binding.path))
                    Assert.That(instance.transform.Find(binding.path), Is.Not.Null, modelName + ": " + binding.path);

            string idle = controllerName == "GuardiaGrandeAnimator" ? "Idle_Villano" : "Idle";
            string walking = controllerName == "GuardiaGrandeAnimator" ? "Walking_Villano" : "Walking_Player";
            animator.SetFloat("Velocidad", 3.5f);
            AdvanceAnimator(animator, walking);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(walking), Is.True, controllerName);

            animator.SetBool("Corriendo", true);
            animator.SetFloat("Velocidad", 6f);
            AdvanceAnimator(animator, "Run_Player");
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Run_Player"), Is.True, controllerName);

            animator.SetBool("Corriendo", false);
            animator.SetFloat("Velocidad", 3.5f);
            AdvanceAnimator(animator, walking);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(walking), Is.True, controllerName);

            animator.SetBool("Corriendo", true);
            AdvanceAnimator(animator, "Run_Player");
            animator.SetBool("Corriendo", false);
            animator.SetFloat("Velocidad", 0f);
            AdvanceAnimator(animator, idle);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(idle), Is.True, controllerName);
        }
        yield return null;
    }

    private static void AdvanceAnimator(Animator animator, string stateName)
    {
        animator.Update(0f);
        // Existing walk transitions use normalized durations, so their time depends on the idle clip.
        for (int step = 0; step < 100; step++)
        {
            animator.Update(0.1f);
            if (!animator.IsInTransition(0) && animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
                return;
        }
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
        GameObject root = CreateObject("Alert route door");
        root.SetActive(false);
        GameObject hinge = CreateObject("Hinge");
        hinge.transform.SetParent(root.transform, false);
        hinge.transform.localPosition = Vector3.left;
        Rigidbody body = hinge.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        GameObject doorObject = CreateObject("Leaf");
        doorObject.transform.SetParent(hinge.transform, false);
        doorObject.transform.localPosition = Vector3.right;
        BoxCollider collider = doorObject.AddComponent<BoxCollider>();
        collider.center = Vector3.up;
        collider.size = new Vector3(2f, 2f, 0.2f);
        Door door = doorObject.AddComponent<Door>();
        door.estadoActual = Door.EstadoPuerta.Bloqueada;
        typeof(Door).GetField("velocidadGuardaGrados", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(door, 90f);
        root.SetActive(true);

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

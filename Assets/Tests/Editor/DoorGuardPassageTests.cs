#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

public class DoorGuardPassageTests
{
    private readonly List<GameObject> objects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            if (objects[i] == null)
                continue;

            foreach (MonoBehaviour component in objects[i].GetComponents<MonoBehaviour>())
                InvokeLifecycle(component, "OnDisable");

            Object.DestroyImmediate(objects[i]);
        }

        objects.Clear();
    }

    [TestCase(Door.EstadoPuerta.Cerrada)]
    [TestCase(Door.EstadoPuerta.Trabada)]
    [TestCase(Door.EstadoPuerta.Bloqueada)]
    [TestCase(Door.EstadoPuerta.Abierta)]
    public void PassageRestoresOriginalDoorState(Door.EstadoPuerta originalState)
    {
        Door door = CreateDoor(originalState);
        GuardDoorPassage guard = CreateGuard();
        Collider blocker = door.GetComponent<Collider>();

        door.RequestGuardPassage(guard);

        Assert.That(door.IsPassable, Is.True);
        Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Abierta));
        Assert.That(blocker.enabled, Is.False, "The guard must physically fit through the door.");

        door.ReleaseGuardPassage(guard);

        Assert.That(door.estadoActual, Is.EqualTo(originalState));
        Assert.That(door.IsPassable, Is.EqualTo(originalState == Door.EstadoPuerta.Abierta));
        Assert.That(blocker.enabled, Is.EqualTo(originalState != Door.EstadoPuerta.Abierta));
    }

    [Test]
    public void ConcurrentGuardsKeepDoorOpenUntilLastGuardLeaves()
    {
        Door door = CreateDoor(Door.EstadoPuerta.Bloqueada);
        GuardDoorPassage first = CreateGuard();
        GuardDoorPassage second = CreateGuard();

        door.RequestGuardPassage(first);
        door.RequestGuardPassage(second);
        door.ReleaseGuardPassage(first);

        Assert.That(door.IsPassable, Is.True);
        Assert.That(door.GetComponent<Collider>().enabled, Is.False);

        door.ReleaseGuardPassage(second);

        Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Bloqueada));
        Assert.That(door.IsPassable, Is.False);
    }

    [Test]
    public void DuplicateRequestsAndUnrelatedReleasesDoNotChangeReservationCount()
    {
        Door door = CreateDoor(Door.EstadoPuerta.Trabada);
        GuardDoorPassage guard = CreateGuard();
        GuardDoorPassage unrelated = CreateGuard();

        door.RequestGuardPassage(guard);
        door.RequestGuardPassage(guard);
        door.ReleaseGuardPassage(unrelated);
        Assert.That(door.IsPassable, Is.True);

        door.ReleaseGuardPassage(guard);
        door.ReleaseGuardPassage(guard);

        Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Trabada));
        Assert.That(door.IsPassable, Is.False);
    }

    [Test]
    public void AnimationCallbacksControlWhenThePassageOpensAndCloses()
    {
        Door door = CreateDoor(Door.EstadoPuerta.Bloqueada);
        GuardDoorPassage guard = CreateGuard();
        ConfigureAnimationEvents(door, out UnityEvent open, out UnityEvent close);
        int openingRequests = 0;
        int closingRequests = 0;
        open.AddListener(() => openingRequests++);
        close.AddListener(() => closingRequests++);

        door.RequestGuardPassage(guard);
        Assert.That(openingRequests, Is.EqualTo(1));
        Assert.That(door.IsPassable, Is.False, "Movement must wait for the future opening clip.");
        Assert.That(door.GetComponent<Collider>().enabled, Is.True);

        door.NotificarPuertaAbierta();
        Assert.That(door.IsPassable, Is.True);
        Assert.That(door.GetComponent<Collider>().enabled, Is.False);

        door.ReleaseGuardPassage(guard);
        Assert.That(closingRequests, Is.EqualTo(1));
        Assert.That(door.GetComponent<Collider>().enabled, Is.False,
            "The collider must not snap shut before the closing clip completes.");

        door.NotificarPuertaCerrada();
        Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Bloqueada));
        Assert.That(door.IsPassable, Is.False);
        Assert.That(door.GetComponent<Collider>().enabled, Is.True);
    }

    [Test]
    public void NewGuardDuringClosingCannotBeClosedOnByAnOldAnimationCallback()
    {
        Door door = CreateDoor(Door.EstadoPuerta.Trabada);
        GuardDoorPassage first = CreateGuard();
        GuardDoorPassage second = CreateGuard();
        ConfigureAnimationEvents(door, out _, out _);

        door.RequestGuardPassage(first);
        door.NotificarPuertaAbierta();
        door.ReleaseGuardPassage(first);
        door.RequestGuardPassage(second);
        door.NotificarPuertaCerrada();
        door.NotificarPuertaAbierta();

        Assert.That(door.IsPassable, Is.True);
        Assert.That(door.GetComponent<Collider>().enabled, Is.False);

        door.ReleaseGuardPassage(second);
        door.NotificarPuertaCerrada();
        Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Trabada),
            "Reopening must preserve the original lock, rather than snapshot the closing transition.");
    }

    [Test]
    public void CancellingBeforeOpeningFinishesDoesNotLeaveDoorOpen()
    {
        Door door = CreateDoor(Door.EstadoPuerta.Bloqueada);
        GuardDoorPassage guard = CreateGuard();
        ConfigureAnimationEvents(door, out _, out _);

        door.RequestGuardPassage(guard);
        door.ReleaseGuardPassage(guard);
        door.NotificarPuertaAbierta();
        door.NotificarPuertaCerrada();

        Assert.That(door.estadoActual, Is.EqualTo(Door.EstadoPuerta.Bloqueada));
        Assert.That(door.IsPassable, Is.False);
        Assert.That(door.GetComponent<Collider>().enabled, Is.True);
    }

    [Test]
    public void PathDetectionAccountsForGuardWidthAndRejectsOtherFloors()
    {
        Door door = CreateDoor(Door.EstadoPuerta.Cerrada);

        Assert.That(door.TryGetPathIntersection(new Vector3(0f, 0f, -3f),
            new Vector3(0f, 0f, 3f), 0.5f, 2f, out float distance), Is.True);
        Assert.That(distance, Is.GreaterThan(0f).And.LessThan(3f));
        Assert.That(door.TryGetPathIntersection(new Vector3(1.3f, 0f, -3f),
            new Vector3(1.3f, 0f, 3f), 0.5f, 2f, out _), Is.True,
            "A guard whose body clips the edge still needs the door to open.");
        Assert.That(door.TryGetPathIntersection(new Vector3(3f, 0f, -3f),
            new Vector3(3f, 0f, 3f), 0.5f, 2f, out _), Is.False);
        Assert.That(door.TryGetPathIntersection(new Vector3(0f, 5f, -3f),
            new Vector3(0f, 5f, 3f), 0.5f, 2f, out _), Is.False);
    }

    [Test]
    public void DoorWaitsUntilWholeGuardClearsTheClosedDoorway()
    {
        Door door = CreateDoor(Door.EstadoPuerta.Cerrada);
        GuardDoorPassage guard = CreateGuard();
        door.RequestGuardPassage(guard);

        Assert.That(door.IsGuardClear(new Vector3(0f, 0f, 0.5f), 0.5f, 2f, 0.1f), Is.False);
        Assert.That(door.IsGuardClear(new Vector3(0f, 0f, 0.8f), 0.5f, 2f, 0.1f), Is.True);
        Assert.That(door.IsGuardClear(new Vector3(0f, 0f, -0.8f), 0.5f, 2f, 0.1f), Is.True);
    }

    [Test]
    public void AnimatedLeafMovementDoesNotMoveTheRememberedDoorway()
    {
        Door door = CreateDoor(Door.EstadoPuerta.Cerrada);
        door.RequestGuardPassage(CreateGuard());
        door.GetComponent<BoxCollider>().center = new Vector3(10f, 1f, 0f);
        Physics.SyncTransforms();

        Assert.That(door.TryGetPathIntersection(new Vector3(0f, 0f, -3f),
            new Vector3(0f, 0f, 3f), 0.5f, 2f, out _), Is.True);
        Assert.That(door.IsGuardClear(Vector3.zero, 0.5f, 2f, 0.1f), Is.False);
    }

    [Test]
    public void RotatedDoorDetectsCrossingInItsOwnOrientation()
    {
        Door door = CreateDoor(Door.EstadoPuerta.Cerrada, Quaternion.Euler(0f, 90f, 0f));

        Assert.That(door.TryGetPathIntersection(new Vector3(-3f, 0f, 0f),
            new Vector3(3f, 0f, 0f), 0.5f, 2f, out _), Is.True);
        Assert.That(door.TryGetPathIntersection(new Vector3(-3f, 0f, 2f),
            new Vector3(3f, 0f, 2f), 0.5f, 2f, out _), Is.False);
    }

    private Door CreateDoor(Door.EstadoPuerta state, Quaternion? rotation = null)
    {
        GameObject doorObject = CreateObject("Test door");
        doorObject.SetActive(false);
        doorObject.transform.rotation = rotation ?? Quaternion.identity;
        BoxCollider collider = doorObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1f, 0f);
        collider.size = new Vector3(2f, 2f, 0.2f);
        Door door = doorObject.AddComponent<Door>();
        door.estadoActual = state;
        doorObject.SetActive(true);
        Physics.SyncTransforms();

        // EditMode tests explicitly initialize runtime lifecycle methods.
        InvokeLifecycle(door, "Awake");
        InvokeLifecycle(door, "OnEnable");
        return door;
    }

    private GuardDoorPassage CreateGuard()
    {
        GuardDoorPassage guard = CreateObject("Test guard").AddComponent<GuardDoorPassage>();
        InvokeLifecycle(guard, "Awake");
        return guard;
    }

    private GameObject CreateObject(string name)
    {
        var instance = new GameObject(name);
        objects.Add(instance);
        return instance;
    }

    private static void ConfigureAnimationEvents(Door door, out UnityEvent open, out UnityEvent close)
    {
        SetField(door, "esperarEventosAnimacion", true);
        open = new UnityEvent();
        close = new UnityEvent();
        SetField(door, "alSolicitarApertura", open);
        SetField(door, "alSolicitarCierre", close);
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing Inspector field: " + name);
        field.SetValue(target, value);
    }

    private static void InvokeLifecycle(MonoBehaviour target, string name)
    {
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(target, null);
    }
}
#endif

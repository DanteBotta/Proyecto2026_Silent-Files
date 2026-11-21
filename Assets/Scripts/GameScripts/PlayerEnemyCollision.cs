using UnityEngine;

// Se coloca en el jugador, junto al CharacterController.
[DefaultExecutionOrder(150)]
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class PlayerEnemyCollision : MonoBehaviour
{
    public MissionEndManager MissionEndManager;

    private CharacterController controller;
    private Collider[] contacts = new Collider[16];

    private bool CanDetectContact => isActiveAndEnabled && controller != null &&
        controller.enabled && controller.detectCollisions && Time.timeScale > 0f &&
        (MissionEndManager == null || !MissionEndManager.MisionTerminada);

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (MissionEndManager == null)
            MissionEndManager = FindAnyObjectByType<MissionEndManager>();
    }

    private void LateUpdate()
    {
        if (!CanDetectContact)
            return;

        // El NavMeshAgent puede alcanzar al jugador sin que este llame a Move.
        // Sincronizar los colliders antes de consultar el contacto de este frame.
        Physics.SyncTransforms();
        Vector3 scale = transform.lossyScale;
        float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(controller.height * Mathf.Abs(scale.y), radius * 2f);
        Vector3 center = transform.TransformPoint(controller.center);
        Vector3 offset = transform.up * (height * 0.5f - radius);

        int count;
        while (true)
        {
            count = Physics.OverlapCapsuleNonAlloc(center - offset, center + offset,
                radius, contacts, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            if (count < contacts.Length)
                break;

            // No perder al enemigo si hay muchos colliders cerca del jugador.
            System.Array.Resize(ref contacts, contacts.Length * 2);
        }

        for (int index = 0; index < count; index++)
        {
            if (TryCapture(contacts[index]))
                return;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Conserva la deteccion inmediata durante el movimiento del jugador.
        TryCapture(hit.collider);
    }

    private bool TryCapture(Collider other)
    {
        if (!CanDetectContact || other == null || other == controller ||
            !other.enabled || other.isTrigger || !other.gameObject.activeInHierarchy ||
            Physics.GetIgnoreLayerCollision(gameObject.layer, other.gameObject.layer) ||
            Physics.GetIgnoreCollision(controller, other))
            return false;

        // Permite reconocer al enemigo aunque el collider este en un objeto hijo.
        AI enemy = other.GetComponentInParent<AI>();
        if (enemy == null || !enemy.isActiveAndEnabled)
            return false;

        if (MissionEndManager == null)
            MissionEndManager = FindAnyObjectByType<MissionEndManager>();

        if (MissionEndManager == null || MissionEndManager.MisionTerminada)
            return false;

        MissionEndManager.Victoria = false;
        MissionEndManager.MisionTerminada = true;
        Time.timeScale = 0f;
        CerrarMinijuegos();
        return true;
    }

    private void CerrarMinijuegos()
    {
        foreach (WindowMinigameManager ventana in
                 FindObjectsByType<WindowMinigameManager>())
            ventana.CerrarPorCaptura();

        foreach (LockpickMinigameController ganzua in
                 FindObjectsByType<LockpickMinigameController>())
            ganzua.CerrarPorCaptura();
    }
}

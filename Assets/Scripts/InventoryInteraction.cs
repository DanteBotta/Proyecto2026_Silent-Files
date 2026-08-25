using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Inventario de 3 slots:
/// 1 principal + 2 secundarios.
/// Al hacer click en cualquier slot, ese objeto pasa a ser el principal.
/// También detecta clicks del mouse sobre puertas y usa el objeto principal.
///
/// FIX importante: "interactionDistance" ahora tiene un default más alto
/// (20f en vez de 6f). El raycast sale desde la posición de la CÁMARA, no
/// desde el jugador. Si tu CameraController usa distance=8 y height=10, la
/// cámara queda a más de 12 unidades de distancia recta del jugador, así
/// que una interactionDistance de 6 nunca iba a poder llegar a nada cercano
/// al jugador. Ajustá este valor según la distancia real de tu cámara.
/// </summary>
public class InventoryInteraction : MonoBehaviour
{
    public enum ItemType
    {
        Hand,
        Lockpick,
        Lever,
        None
    }

    [Header("Slots")]
    public Button primaryButton;
    public Button secondaryButton1;
    public Button secondaryButton2;

    [Header("Imágenes de los slots")]
    public Image primaryImage;
    public Image secondaryImage1;
    public Image secondaryImage2;

    [Header("Sprites")]
    public Sprite handSprite;
    public Sprite lockpickSprite;
    public Sprite leverSprite;

    [Tooltip("Sprite para un slot vacío (ej. cuando la ganzúa se rompe y desaparece). Podés dejarlo vacío para que el slot simplemente quede sin dibujar nada.")]
    public Sprite emptySprite;

    [Header("Objetos iniciales")]
    public ItemType primaryItem = ItemType.Hand;
    public ItemType secondaryItem1 = ItemType.Lockpick;
    public ItemType secondaryItem2 = ItemType.Lever;

    [Header("Escala")]
    public float selectedScale = 1.15f;
    public float secondaryScale = 1f;

    [Header("Interacción con puertas")]
    public Camera interactionCamera;

    [Tooltip("IMPORTANTE: el raycast sale desde la posición de la CÁMARA, no del jugador. " +
             "Si tu cámara está lejos (top-down elevado), este valor tiene que ser mayor " +
             "que la distancia cámara-jugador + el alcance que quieras dar. Con una cámara " +
             "típica top-down (distance=8, height=10) probá con 20 o más.")]
    public float interactionDistance = 20f;

    public LayerMask doorLayer = ~0;

    [Header("Estado de herramientas")]
    [Tooltip("Se pone en true automáticamente cuando la ganzúa se rompe. No se edita a mano.")]
    public bool lockpickBroken = false;

    [Header("Debug")]
    [Tooltip("Mientras esté activo, imprime en consola qué está pasando en cada click " +
             "(qué golpeó el raycast, si encontró una puerta, etc). Desactivalo cuando ya " +
             "confirmes que todo funciona, para no llenar la consola.")]
    public bool debugLogs = true;

    private RectTransform primaryRect;
    private RectTransform secondaryRect1;
    private RectTransform secondaryRect2;

    private void Awake()
    {
        if (interactionCamera == null)
            interactionCamera = Camera.main;

        if (interactionCamera == null && debugLogs)
        {
            Debug.Log("InventoryInteraction: Camera.main es null");
        }

        if (primaryImage != null)
            primaryRect = primaryImage.rectTransform;

        if (secondaryImage1 != null)
            secondaryRect1 = secondaryImage1.rectTransform;

        if (secondaryImage2 != null)
            secondaryRect2 = secondaryImage2.rectTransform;

        if (primaryButton != null)
            primaryButton.onClick.AddListener(() => SelectSlot(0));

        if (secondaryButton1 != null)
            secondaryButton1.onClick.AddListener(() => SelectSlot(1));

        if (secondaryButton2 != null)
            secondaryButton2.onClick.AddListener(() => SelectSlot(2));

        RefreshUI();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        // Si estamos haciendo click sobre el inventario (o cualquier UI),
        // no intentamos interactuar con una puerta.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        TryInteractWithDoor();
    }

    // =========================================================
    // SELECCIONAR SLOT
    // =========================================================

    private void SelectSlot(int slot)
    {
        if (slot == 0)
        {
            RefreshUI();
            return;
        }

        if (slot == 1)
        {
            Swap(ref primaryItem, ref secondaryItem1);
        }
        else if (slot == 2)
        {
            Swap(ref primaryItem, ref secondaryItem2);
        }

        RefreshUI();
    }

    private void Swap(ref ItemType a, ref ItemType b)
    {
        ItemType temporary = a;
        a = b;
        b = temporary;
    }

    // =========================================================
    // ACTUALIZAR INVENTARIO
    // =========================================================

    private void RefreshUI()
    {
        UpdateImage(primaryImage, primaryItem);
        UpdateImage(secondaryImage1, secondaryItem1);
        UpdateImage(secondaryImage2, secondaryItem2);

        if (primaryRect != null)
            primaryRect.localScale = Vector3.one * selectedScale;

        if (secondaryRect1 != null)
            secondaryRect1.localScale = Vector3.one * secondaryScale;

        if (secondaryRect2 != null)
            secondaryRect2.localScale = Vector3.one * secondaryScale;
    }

    private void UpdateImage(Image image, ItemType item)
    {
        if (image == null)
            return;

        // Si es la ganzúa y ya está rota, se "hace desaparecer" del slot
        // (se deja de mostrar el sprite) en vez de mostrarla normal.
        if (item == ItemType.Lockpick && lockpickBroken)
        {
            image.sprite = null;
            image.enabled = false;
            return;
        }

        switch (item)
        {
            case ItemType.None:
                image.sprite = emptySprite;
                break;

            case ItemType.Hand:
                image.sprite = handSprite;
                break;

            case ItemType.Lockpick:
                image.sprite = lockpickSprite;
                break;

            case ItemType.Lever:
                image.sprite = leverSprite;
                break;
        }

        image.enabled = image.sprite != null;
    }

    // =========================================================
    // CLICK SOBRE PUERTA
    // =========================================================

    private void TryInteractWithDoor()
    {
        if (interactionCamera == null)
            interactionCamera = Camera.main;

        if (interactionCamera == null)
        {
            Debug.LogError("InventoryInteraction: no hay una cámara asignada.");
            return;
        }

        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);

        if (debugLogs)
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red, 1f);
        }

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            doorLayer))
        {
            if (debugLogs) Debug.Log("[Inventory] El raycast no golpeó nada dentro de " + interactionDistance + " unidades.");
            return;
        }

        if (debugLogs) Debug.Log("[Inventory] Raycast golpeó: " + hit.collider.gameObject.name);

        SimpleDoor door = hit.collider.GetComponentInParent<SimpleDoor>();

        if (door == null)
        {
            if (debugLogs) Debug.Log("[Inventory] El objeto golpeado no tiene SimpleDoor (ni en él ni en sus padres).");
            return;
        }

        if (debugLogs) Debug.Log("[Inventory] Interactuando con la puerta usando: " + primaryItem);

        UseItemOnDoor(door);
    }

    // =========================================================
    // USAR OBJETO
    // =========================================================

    private void UseItemOnDoor(SimpleDoor door)
    {
        switch (primaryItem)
        {
            case ItemType.Hand:
                door.UseHand();
                break;

            case ItemType.Lockpick:
                if (lockpickBroken)
                {
                    if (debugLogs) Debug.Log("[Inventory] Intentaste usar la ganzúa, pero está rota.");
                    return;
                }
                door.UseLockpick();
                break;

            case ItemType.Lever:
                door.UseLever();
                break;
        }
    }

    // =========================================================
    // GANZÚA ROTA
    // =========================================================

    /// <summary>
    /// Llamar desde el evento "On Lockpick Broken" de SimpleDoor (Inspector),
    /// o desde cualquier otro lugar que necesite invalidar la ganzúa.
    /// La oculta visualmente de donde esté (slot principal o secundario) y
    /// bloquea que se pueda volver a usar.
    /// </summary>
    public void OnLockpickBroken()
    {
        lockpickBroken = true;
        RefreshUI();

        if (debugLogs) Debug.Log("La ganzúa se rompió y ya no está disponible.");
    }
}

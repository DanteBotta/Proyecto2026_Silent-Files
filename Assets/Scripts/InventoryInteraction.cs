using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Inventario de 3 slots: 1 principal + 2 secundarios.
/// - Click en un slot secundario (o tecla 1 / 2): intercambia ese objeto con el principal.
/// - Implementa IToolProvider: el InteractionManager le pregunta directamente qué
///   herramienta está en el slot principal, ya no hace falta que este script
///   detecte puertas por su cuenta (eso lo hace el InteractionManager con la tecla F).
///
/// Para conectarlo: arrastrá este GameObject al campo "proveedorHerramientaComponent"
/// del InteractionManager.
/// </summary>
public class InventoryInteraction : MonoBehaviour, IToolProvider
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

    [Header("Teclas rápidas")]
    [Tooltip("Tecla que intercambia el slot secundario 1 con el principal.")]
    public KeyCode teclaSlot1 = KeyCode.Alpha1;
    [Tooltip("Tecla que intercambia el slot secundario 2 con el principal.")]
    public KeyCode teclaSlot2 = KeyCode.Alpha2;

    [Header("Estado de herramientas")]
    [Tooltip("Se pone en true automáticamente cuando la ganzúa se rompe. No se edita a mano.")]
    public bool lockpickBroken = false;

    [Header("Debug")]
    [Tooltip("Mientras esté activo, imprime en consola los cambios de slot y el estado de la ganzúa.")]
    public bool debugLogs = false;

    private RectTransform primaryRect;
    private RectTransform secondaryRect1;
    private RectTransform secondaryRect2;

    private void Awake()
    {
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
        if (Input.GetKeyDown(teclaSlot1)) SelectSlot(1);
        if (Input.GetKeyDown(teclaSlot2)) SelectSlot(2);
    }

    // =========================================================
    // IToolProvider — acá es donde el InteractionManager pregunta
    // qué herramienta está equipada en este momento.
    // =========================================================

    public ToolType GetHerramientaEquipada()
    {
        return ItemTypeToToolType(primaryItem);
    }

    private ToolType ItemTypeToToolType(ItemType item)
    {
        switch (item)
        {
            case ItemType.Hand:
                return ToolType.Mano;

            case ItemType.Lockpick:
                // Si está rota, es como no tener nada equipado: el InteractionManager
                // no va a poder iniciar el minijuego con ToolType.Ninguna.
                return lockpickBroken ? ToolType.Ninguna : ToolType.Ganzua;

            case ItemType.Lever:
                return ToolType.Palanca;

            case ItemType.None:
            default:
                return ToolType.Ninguna;
        }
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

        if (debugLogs) Debug.Log($"[Inventory] Slot principal ahora: {primaryItem}");

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

        // Si es la ganzúa y ya está rota, se reemplaza por el sprite de slot vacío
        // (en vez de ocultarla). Para esto, la ganzúa rota siempre termina viviendo
        // en el slot secundario 2 (ver OnLockpickBroken), así que en la práctica
        // este caso solo se va a dar ahí.
        if (item == ItemType.Lockpick && lockpickBroken)
        {
            image.sprite = emptySprite;
            image.enabled = emptySprite != null;
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
    // GANZÚA ROTA
    // =========================================================

    /// <summary>
    /// Llamar desde el evento "On Ganzúa Rota" del InteractionManager (Inspector),
    /// o desde cualquier otro lugar que necesite invalidar la ganzúa.
    ///
    /// Mueve la ganzúa (esté donde esté: principal, secundario 1 o ya en
    /// secundario 2) específicamente al slot secundario 2, la reemplaza por
    /// el sprite de "vacío", y desactiva ese botón para que ya no se pueda
    /// volver a clickear ni intercambiar con nada.
    /// </summary>
    public void OnLockpickBroken()
    {
        lockpickBroken = true;

        if (primaryItem == ItemType.Lockpick)
        {
            // Lo que estaba en el slot 2 pasa a ocupar el principal,
            // y la ganzúa rota toma su lugar en el slot 2.
            primaryItem = secondaryItem2;
            secondaryItem2 = ItemType.Lockpick;
        }
        else if (secondaryItem1 == ItemType.Lockpick)
        {
            // Mismo intercambio, pero entre secundario 1 y secundario 2.
            secondaryItem1 = secondaryItem2;
            secondaryItem2 = ItemType.Lockpick;
        }
        // Si ya estaba en secundario 2, no hace falta mover nada.

        // El slot 2 queda bloqueado: no se puede volver a clickear,
        // así que tampoco se puede volver a intercambiar con otro slot.
        if (secondaryButton2 != null)
        {
            secondaryButton2.interactable = false;
        }

        RefreshUI();

        if (debugLogs) Debug.Log("[Inventory] La ganzúa se rompió, se movió al slot 2 y quedó bloqueada.");
    }

    /// <summary>
    /// Llamar cuando el jugador consiga/repare una ganzúa nueva, para reactivar el slot 2.
    /// No la mueve de lugar ni cambia qué hay ahí — solo levanta el bloqueo del botón.
    /// </summary>
    public void OnLockpickRepaired()
    {
        lockpickBroken = false;

        if (secondaryButton2 != null)
            secondaryButton2.interactable = true;

        RefreshUI();

        if (debugLogs) Debug.Log("[Inventory] La ganzúa fue reparada/reemplazada.");
    }
}
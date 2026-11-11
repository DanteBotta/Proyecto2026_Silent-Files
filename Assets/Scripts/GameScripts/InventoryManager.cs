using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public enum Herramienta
    {
        Mano,
        Ganzua,
        Palanca,
        Vacio
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
    public Sprite emptySprite;

    [Header("Escala")]
    public float selectedScale = 1.15f;
    public float secondaryScale = 1f;

    [Header("Teclas rápidas")]
    public KeyCode teclaSlot1 = KeyCode.Alpha1;
    public KeyCode teclaSlot2 = KeyCode.Alpha2;

    // Herramientas de cada slot
    private Herramienta primaryObject = Herramienta.Mano;
    private Herramienta secondaryObject1 = Herramienta.Ganzua;
    private Herramienta secondaryObject2 = Herramienta.Palanca;

    // Sprites de cada slot
    private Sprite primarySprite;
    private Sprite secondarySprite1;
    private Sprite secondarySprite2;

    // Devuelve la herramienta seleccionada
    public Herramienta ObjetoEnMano
    {
        get { return primaryObject; }
    }

    void Start()
    {
        // Imágenes iniciales
        primarySprite = handSprite;
        secondarySprite1 = lockpickSprite;
        secondarySprite2 = leverSprite;

        // Acciones de los botones
        primaryButton.onClick.AddListener(SeleccionarPrincipal);
        secondaryButton1.onClick.AddListener(IntercambiarSlot1);
        secondaryButton2.onClick.AddListener(IntercambiarSlot2);

        ActualizarInterfaz();
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaSlot1))
        {
            IntercambiarSlot1();
        }

        if (Input.GetKeyDown(teclaSlot2))
        {
            IntercambiarSlot2();
        }
    }

    void SeleccionarPrincipal()
    {
        ActualizarInterfaz();
    }

    void IntercambiarSlot1()
    {
        Herramienta herramientaTemporal = primaryObject;
        primaryObject = secondaryObject1;
        secondaryObject1 = herramientaTemporal;

        Sprite spriteTemporal = primarySprite;
        primarySprite = secondarySprite1;
        secondarySprite1 = spriteTemporal;

        ActualizarInterfaz();
    }

    void IntercambiarSlot2()
    {
        Herramienta herramientaTemporal = primaryObject;
        primaryObject = secondaryObject2;
        secondaryObject2 = herramientaTemporal;

        Sprite spriteTemporal = primarySprite;
        primarySprite = secondarySprite2;
        secondarySprite2 = spriteTemporal;

        ActualizarInterfaz();
    }

    void ActualizarInterfaz()
    {
        primaryImage.sprite =
            primarySprite != null ? primarySprite : emptySprite;

        secondaryImage1.sprite =
            secondarySprite1 != null ? secondarySprite1 : emptySprite;

        secondaryImage2.sprite =
            secondarySprite2 != null ? secondarySprite2 : emptySprite;

        primaryImage.transform.localScale =
            Vector3.one * selectedScale;

        secondaryImage1.transform.localScale =
            Vector3.one * secondaryScale;

        secondaryImage2.transform.localScale =
            Vector3.one * secondaryScale;
    }
}
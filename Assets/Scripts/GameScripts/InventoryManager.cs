using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Im�genes de los slots")]
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

    [Header("Teclas r�pidas")]
    public KeyCode teclaSlot1 = KeyCode.Alpha1;
    public KeyCode teclaSlot2 = KeyCode.Alpha2;

    [Header("Texto al cambiar de herramienta")]
    public TMP_Text textoHerramienta;
    [Tooltip("Se muestra antes del nombre de la herramienta.")]
    public string prefijoTexto = "Ahora tenés: ";
    public string nombreMano = "Mano";
    public string nombreGanzua = "Ganzúa";
    public string nombrePalanca = "Palanca";
    [Min(0f)] public float duracionFadeIn = 0.1f;
    [Min(0f)] public float tiempoVisible = 0.6f;
    [Min(0f)] public float duracionFadeOut = 0.5f;

    private Coroutine corrutinaTexto;

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

    // Pasa la mano al slot principal (si ya está en mano no hace nada).
    // Devuelve true si tuvo que cambiar de herramienta.
    public bool EquiparMano()
    {
        if (primaryObject == Herramienta.Mano)
            return false;

        if (secondaryObject1 == Herramienta.Mano)
            IntercambiarSlot1();
        else if (secondaryObject2 == Herramienta.Mano)
            IntercambiarSlot2();
        else
            return false;

        return true;
    }

    void Start()
    {
        // Im�genes iniciales
        primarySprite = handSprite;
        secondarySprite1 = lockpickSprite;
        secondarySprite2 = leverSprite;

        // Acciones de los botones
        primaryButton.onClick.AddListener(SeleccionarPrincipal);
        secondaryButton1.onClick.AddListener(IntercambiarSlot1);
        secondaryButton2.onClick.AddListener(IntercambiarSlot2);

        if (textoHerramienta != null)
            AplicarAlfaTexto(0f);

        ActualizarInterfaz();
    }

    void AplicarAlfaTexto(float alfa)
    {
        Color c = textoHerramienta.color;
        c.a = alfa;
        textoHerramienta.color = c;
    }

    string NombreHerramienta(Herramienta h)
    {
        switch (h)
        {
            case Herramienta.Mano: return nombreMano;
            case Herramienta.Ganzua: return nombreGanzua;
            case Herramienta.Palanca: return nombrePalanca;
            default: return string.Empty;
        }
    }

    void MostrarTextoHerramienta()
    {
        if (textoHerramienta == null)
            return;

        if (corrutinaTexto != null)
            StopCoroutine(corrutinaTexto);
        corrutinaTexto = StartCoroutine(CorrutinaTextoHerramienta());
    }

    IEnumerator CorrutinaTextoHerramienta()
    {
        textoHerramienta.text = prefijoTexto + NombreHerramienta(primaryObject);

        // Fade in (rápido)
        for (float t = 0f; t < duracionFadeIn; t += Time.unscaledDeltaTime)
        {
            AplicarAlfaTexto(t / duracionFadeIn);
            yield return null;
        }
        AplicarAlfaTexto(1f);

        yield return new WaitForSecondsRealtime(tiempoVisible);

        // Fade out (más lento)
        for (float t = 0f; t < duracionFadeOut; t += Time.unscaledDeltaTime)
        {
            AplicarAlfaTexto(1f - t / duracionFadeOut);
            yield return null;
        }
        AplicarAlfaTexto(0f);
        corrutinaTexto = null;
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
        MostrarTextoHerramienta();
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
        MostrarTextoHerramienta();
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
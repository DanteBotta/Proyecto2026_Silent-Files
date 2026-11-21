using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra el panel de información de la nueva versión junto con el panelInterruption,
/// una sola vez por apertura del juego.
/// - NO se muestra solo: lo dispara otro script llamando a Mostrar() (ej. IntroJuego).
/// - Al tocar el panelVersion no pasa nada.
/// - Al tocar el Button del panelInterruption se cierran ambos paneles.
/// - Al recargar la escena o cargarla desde otra, no se vuelven a mostrar de esta forma.
///
/// Uso: poner este script en un objeto que esté SIEMPRE activo, NO en los paneles.
/// </summary>
public class VersionInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelVersion;      // Panel con la info de la nueva versión
    [SerializeField] private Button interruptionButton;    // Button del panelInterruption (ya existente)

    // static: sobrevive a recargas y cambios de escena, pero se reinicia al abrir el juego de nuevo.
    private static bool alreadyShownThisSession = false;

    private void Awake()
    {
        // El panelVersion arranca siempre oculto; se muestra cuando alguien llame a Mostrar().
        // El panelInterruption no se toca acá porque lo usan otros scripts.
        if (panelVersion != null) panelVersion.SetActive(false);
    }

    private void OnEnable()
    {
        if (interruptionButton != null)
            interruptionButton.onClick.AddListener(ClosePanels);
    }

    private void OnDisable()
    {
        if (interruptionButton != null)
            interruptionButton.onClick.RemoveListener(ClosePanels);
    }

    /// <summary>Activa ambos paneles, solo la primera vez desde que se abrió el juego.</summary>
    public void Mostrar()
    {
        if (alreadyShownThisSession || panelVersion == null) return;

        alreadyShownThisSession = true;
        panelVersion.SetActive(true);
        if (interruptionButton != null) interruptionButton.gameObject.SetActive(true);
    }

    /// <summary>Cierra ambos paneles (se llama al tocar el panelInterruption).</summary>
    public void ClosePanels()
    {
        if (panelVersion != null) panelVersion.SetActive(false);
        if (interruptionButton != null) interruptionButton.gameObject.SetActive(false);
    }
}
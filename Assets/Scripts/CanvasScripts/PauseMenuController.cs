using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class PauseMenuController : MonoBehaviour
{
    public Image PanelMenuPausa;
    public MissionBriefing MissionBriefing;

    [Header("Canvas a ocultar")]
    public Canvas CanvasMinimapa;
    public Canvas CanvasInventario;

    bool MenuPausaDesplegado = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PanelMenuPausa.gameObject.SetActive(false);
        MissionBriefing = FindAnyObjectByType<MissionBriefing>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && MissionBriefing.BriefingTerminado)
        {
            if (!MenuPausaDesplegado)
            {
                PanelMenuPausa.gameObject.SetActive(true);
                MenuPausaDesplegado = true;

                CanvasMinimapa.gameObject.SetActive(false);
                CanvasInventario.gameObject.SetActive(false);

                // Pausar el juego
                Time.timeScale = 0f;
            }
            else
            {
                BotonContinuar();
            }
        }
    }

    public void BotonContinuar()
    {
        // Reanudar el juego
        Time.timeScale = 1f;
        PanelMenuPausa.gameObject.SetActive(false);
        MenuPausaDesplegado = false;

        CanvasMinimapa.gameObject.SetActive(true);
        CanvasInventario.gameObject.SetActive(true);
    }

    public void BotonReiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BotonSalir()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("PantallaInicio");
    }
}

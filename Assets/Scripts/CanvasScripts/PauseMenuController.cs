
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class PauseMenuController : MonoBehaviour
{
    public Image PanelMenuPausa;
    public MissionBriefing MissionBriefing;
    public WindowMinigameManager[] WindowMinigames;

    [Header("Canvas a ocultar")]
    public Canvas CanvasMinimapa;
    public Canvas CanvasInventario;

    bool MenuPausaDesplegado = false;

    void Start()
    {
        PanelMenuPausa.gameObject.SetActive(false);
        MissionBriefing = FindAnyObjectByType<MissionBriefing>();
        WindowMinigames = FindObjectsByType<WindowMinigameManager>();
    }

    void Update()
    {
        if (!MissionBriefing.BriefingTerminado)
            return;

        // Comprobar si hay algún minijuego activo
        WindowMinigames = FindObjectsByType<WindowMinigameManager>();

        foreach (WindowMinigameManager Minijuego in WindowMinigames)
        {
            if (Minijuego.MinijuegoActivo)
                return;
        }

        // Si un minijuego se cerró con este mismo ESC, no abrir el menú
        if (WindowMinigameManager.FrameCierrePorEsc == Time.frameCount)
            return;

        // ESC para abrir o cerrar el menú de pausa
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!MenuPausaDesplegado)
            {
                PanelMenuPausa.gameObject.SetActive(true);
                MenuPausaDesplegado = true;

                CanvasMinimapa.gameObject.SetActive(false);
                CanvasInventario.gameObject.SetActive(false);

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

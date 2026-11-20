using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MissionEndManager : MonoBehaviour
{
    public Image PanelInterruption;

    [Header("Canvas Victoria")]
    public GameObject CanvasVictoria;

    [Header("Canvas Derrota")]
    public GameObject CanvasDerrota;

    [Header("Panel a Ocultar")]
    public Canvas PanelInventario;
    public Canvas Minimapa;
    public Camera UICamera;

    [Header("Speedrun")]
    public SpeedrunManager speedrunManager;

    [Header("Parametros (no tocar)")]
    public bool MisionTerminada = false;
    public bool Victoria;

    private bool finProcesado = false;

    void Start()
    {
        speedrunManager = FindAnyObjectByType<SpeedrunManager>();

        PanelInterruption.enabled = false;
        CanvasVictoria.SetActive(false);
        CanvasDerrota.SetActive(false);
    }

    void Update()
    {
        if (!MisionTerminada || finProcesado)
        {
            return;
        }

        finProcesado = true;

        PanelInterruption.enabled = true;
        PanelInventario.gameObject.SetActive(false);
        Minimapa.gameObject.SetActive(false);
        UICamera.gameObject.SetActive(false);

        if (Victoria)
        {
            CanvasVictoria.SetActive(true);

            // Frena el cronómetro; el tiempo final queda visible
            if (speedrunManager != null)
            {
                speedrunManager.PausarTimer();
            }
        }
        else
        {
            CanvasDerrota.SetActive(true);
        }
    }

    public void BotonSalir()
    {
        SceneManager.LoadScene("PantallaInicio", LoadSceneMode.Single);
    }

    public void BotonReiniciar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BotonContinuar()
    {
        SceneManager.LoadScene("EleccionMapa");
    }
}
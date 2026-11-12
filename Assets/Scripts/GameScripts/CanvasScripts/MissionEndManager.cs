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

    [Header("Parametros (no tocar)")]
    public bool MisionTerminada = false;
    public bool Victoria;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PanelInterruption.enabled = false;
        CanvasVictoria.SetActive(false);
        CanvasDerrota.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (MisionTerminada)
        {
            PanelInterruption.enabled = true;
            PanelInventario.gameObject.SetActive(false);
            Minimapa.gameObject.SetActive(false);
            UICamera.gameObject.SetActive(false);
            if (Victoria)
            {
                CanvasVictoria.SetActive(true);
            }
            else if (!Victoria)
            {
                CanvasDerrota.SetActive(true);
            }
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

using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ConfigurationOpen : MonoBehaviour
{
    public GameObject PanelInterrution;
    public GameObject PanelConfiguracion;
    public GameObject PanelSalir;

    bool PanelAbierto = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PanelInterrution.SetActive(false);
        PanelConfiguracion.SetActive(false);
        PanelSalir.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            AbrirPanelSalir();
        }
    }

    public void BotonJugar()
    {
        SceneManager.LoadScene("EleccionMapa");
    }

    public void AbrirPanelConfiguracion()
    {
        PanelInterrution.SetActive(true);
        PanelConfiguracion.SetActive(true);
        PanelAbierto = true;
    }

    public void AbrirPanelSalir()
    {
        if (!PanelAbierto)
        {
            PanelInterrution.SetActive(true);
            PanelConfiguracion.SetActive(false);
            PanelSalir.SetActive(true);
            PanelAbierto = true;
        }
        else
        {
            CerrarPaneles();
            PanelAbierto = false;
        }
    }

    public void CerrarJuego()
    {
        #if UNITY_EDITOR
                EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    public void CerrarPaneles()
    {
        PanelInterrution.SetActive(false);
        PanelConfiguracion.SetActive(false);
        PanelSalir.SetActive(false);
        PanelAbierto = false;
    }
}

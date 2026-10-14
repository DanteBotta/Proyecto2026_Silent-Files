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

    bool PanelSalirAbierto = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PanelInterrution.SetActive(false);
        PanelConfiguracion.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!PanelSalirAbierto)
            {
                PanelInterrution.SetActive(true);
                PanelSalir.SetActive(true);
                PanelSalirAbierto = true;
            }
            else
            {
                CerrarPaneles();
                PanelSalirAbierto = false;
            }
            
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
        PanelSalirAbierto = false;
    }
}

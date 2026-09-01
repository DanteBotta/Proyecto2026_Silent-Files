using UnityEngine;

public class ConfigurationOpen : MonoBehaviour
{
    public GameObject PanelInterrution;
    public GameObject PanelConfiguracion;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PanelInterrution.SetActive(false);
        PanelConfiguracion.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AbrirPanelConfiguracion()
    {
        PanelInterrution.SetActive(true);
        PanelConfiguracion.SetActive(true);
    }

    public void CerrarPaneles()
    {
        PanelInterrution.SetActive(false);
        PanelConfiguracion.SetActive(false);
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class MissionBriefing : MonoBehaviour
{
    public GameObject Personaje;
    public GameObject PuntoSpawn;


    [Header("Panel")]
    public GameObject Panel;
    public GameObject PanelInventario;
        
    [Header("Textos")]
    public TextMeshProUGUI Fecha;
    public TextMeshProUGUI Hora;
    public TextMeshProUGUI Ubicacion;

    public TextMeshProUGUI Linea1;

    public TextMeshProUGUI TituloMision;
    public TextMeshProUGUI Descripcion;
    public TextMeshProUGUI Objetivo;

    public TextMeshProUGUI Linea2;

    public TextMeshProUGUI PresionaF;

    [Header("Configuracion")]
    public float VelocidadEscritura = 0.03f;
    public float TiempoEntreTextos = 0.3f;

    bool BriefingTerminado = false;


    private void Awake()
    {
        Instantiate(Personaje, PuntoSpawn.transform.position, PuntoSpawn.transform.rotation);
        PanelInventario.SetActive(false);
    }
    void Start()
    {
        Panel.SetActive(true);

        Fecha.text = "";
        Hora.text = "";
        Ubicacion.text = "";

        Linea1.text = "";

        TituloMision.text = "";
        Descripcion.text = "";
        Objetivo.text = "";

        Linea2.text = "";

        PresionaF.text = "";

        Time.timeScale = 0;

        StartCoroutine(MostrarBriefing());
    }

    void Update()
    {
        if (BriefingTerminado && Input.GetKeyDown(KeyCode.F))
        {
            ContinuarMision();
        }

        if (!BriefingTerminado && Input.GetKeyDown(KeyCode.F))
        {
            // Si el briefing no ha terminado y se presiona F, se completa inmediatamente
            StopAllCoroutines();
            Fecha.text = "FECHA\t\t: 06.10.2026";
            Hora.text = "HORA\t\t: 02:37 AM";
            Ubicacion.text = "UBICACIÓN\t\t: LANUS, ARGENTINA";
            Linea1.text = "________________________________________________________________________";
            TituloMision.text = "MISIÓN 01 - El Falsificador";
            Descripcion.text = "El mejor falsificador de identidades de la ciudad vive aquí";
            Objetivo.text = "OBJETIVO                : INGRESAR SIN SER DETECTADO Y RECUPERAR   \t\t\t      DOCUMENTOS";
            Linea2.text = "________________________________________________________________________";
            PresionaF.text = "Presiona [ F ] para EMPEZAR";
            BriefingTerminado = true;
        }
    }

    IEnumerator MostrarBriefing()
    {
        yield return StartCoroutine(EscribirTexto(Fecha, "FECHA\t\t: 06.10.2026"));
        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        yield return StartCoroutine(EscribirTexto(Hora, "HORA\t\t: 02:37 AM"));
        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        yield return StartCoroutine(EscribirTexto(Ubicacion, "UBICACIÓN\t\t: LANUS, ARGENTINA"));
        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        Linea1.text = "________________________________________________________________________";

        yield return StartCoroutine(EscribirTexto(TituloMision, "MISIÓN 01 - El Falsificador"));
        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        yield return StartCoroutine(EscribirTexto(Descripcion, "El mejor falsificador de identidades de la ciudad vive aquí"));
        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        yield return StartCoroutine(EscribirTexto(Objetivo, "OBJETIVO                : INGRESAR SIN SER DETECTADO Y RECUPERAR   \t\t\t      DOCUMENTOS"));
        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        Linea2.text = "________________________________________________________________________";

        // Espera 1 segundo antes de mostrar el botón
        yield return new WaitForSecondsRealtime(1f);

        PresionaF.text = "Presiona [ F ] para EMPEZAR";

        BriefingTerminado = true;


    }

    IEnumerator EscribirTexto(TextMeshProUGUI texto, string contenido)
    {
        texto.text = "";

        foreach (char letra in contenido)
        {
            texto.text += letra;

            yield return new WaitForSecondsRealtime(VelocidadEscritura);
        }
    }

    void ContinuarMision()
    {
        Panel.SetActive(false);
        PanelInventario.SetActive(true);
        Time.timeScale = 1;
    }
}
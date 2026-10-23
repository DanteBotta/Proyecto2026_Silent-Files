using System.Collections;
using TMPro;
using UnityEngine;

public class MissionBriefing : MonoBehaviour
{
    [Header("Personaje")]
    public GameObject Personaje;
    public GameObject PuntoSpawn;

    [Header("Panel")]
    public GameObject Panel;

    [Header("Panel a Ocultar")]
    public GameObject PanelInventario;
    public GameObject Minimapa;

    [Header("Textos")]
    public TextMeshProUGUI Fecha;
    public TextMeshProUGUI Hora;
    public TextMeshProUGUI Ubicacion;

    public TextMeshProUGUI TituloMision;
    public TextMeshProUGUI Descripcion;
    public TextMeshProUGUI Objetivo;
    public TextMeshProUGUI PresionaF;

    public TextMeshProUGUI Linea1;
    public TextMeshProUGUI Linea2;

    [Header("Configuracion")]
    public float VelocidadEscritura = 0.03f;
    public float TiempoEntreTextos = 0.3f;
    public bool ActivarBriefing = true;

    [Header("Estado")]
    public bool BriefingTerminado = false;
    public bool SalteandoBriefing = false;
    public bool BloquearFHastaSoltar = false;
    public bool MisionIniciada = false;

    private bool personajeInstanciado = false;

    private void Awake()
    {
        PanelInventario.SetActive(false);
        Minimapa.SetActive(false);
        InstanciarPersonaje();
    }

    private void Start()
    {
        Time.timeScale = 0f;

        if (!ActivarBriefing)
        {
            Panel.SetActive(false);
            TerminarBriefing();
            ContinuarMision();
            return;
        }

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

        StartCoroutine(MostrarBriefing());
    }

    private void Update()
    {
        if (!ActivarBriefing)
        {
            return;
        }

        // Liberar el bloqueo cuando se suelta la F
        if (BloquearFHastaSoltar)
        {
            if (Input.GetKeyUp(KeyCode.F))
            {
                BloquearFHastaSoltar = false;
            }

            return;
        }

        // Una vez iniciada la misión, este script ya no usa la F
        if (MisionIniciada)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.F))
        {
            return;
        }

        if (!BriefingTerminado)
        {
            StopAllCoroutines();

            Fecha.text = "FECHA\t\t: 06.10.2026";
            Hora.text = "HORA\t\t: 02:37 AM";
            Ubicacion.text = "UBICACIÓN\t\t: LANUS, ARGENTINA";
            Linea1.text = "________________________________________________________________________";
            TituloMision.text = "MISIÓN 01 - El Falsificador";
            Descripcion.text = "El mejor falsificador de identidades de la ciudad vive aquí";
            Objetivo.text = "OBJETIVO : INGRESAR SIN SER DETECTADO Y RECUPERAR DOCUMENTOS";
            Linea2.text = "________________________________________________________________________";
            PresionaF.text = "Presiona [ F ] para EMPEZAR";

            SalteandoBriefing = true;
            BriefingTerminado = true;
            BloquearFHastaSoltar = true;

            return;
        }

        BloquearFHastaSoltar = true;
        ContinuarMision();
    }

    private IEnumerator MostrarBriefing()
    {
        yield return StartCoroutine(
            EscribirTexto(Fecha, "FECHA\t\t: 06.10.2026"));

        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        yield return StartCoroutine(
            EscribirTexto(Hora, "HORA\t\t: 02:37 AM"));

        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        yield return StartCoroutine(
            EscribirTexto(Ubicacion, "UBICACI�N\t\t: LANUS, ARGENTINA"));

        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        Linea1.text = "________________________________________________________________________";

        yield return StartCoroutine(
            EscribirTexto(TituloMision, "MISI�N 01 - El Falsificador"));

        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        yield return StartCoroutine(
            EscribirTexto(Descripcion, "El mejor falsificador de identidades de la ciudad vive aqu�"));

        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        yield return StartCoroutine(
            EscribirTexto(Objetivo, "OBJETIVO : INGRESAR SIN SER DETECTADO Y RECUPERAR DOCUMENTOS"));

        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        Linea2.text = "________________________________________________________________________";

        yield return new WaitForSecondsRealtime(1f);

        PresionaF.text = "Presiona [ F ] para EMPEZAR";

        TerminarBriefing();
    }

    private IEnumerator EscribirTexto(
        TextMeshProUGUI texto, string contenido)
    {
        texto.text = "";

        foreach (char letra in contenido)
        {
            texto.text += letra;
            yield return new WaitForSecondsRealtime(VelocidadEscritura);
        }
    }

    private void TerminarBriefing()
    {
        BriefingTerminado = true;
    }

    private void InstanciarPersonaje()
    {
        if (personajeInstanciado)
        {
            return;
        }

        Instantiate(
            Personaje,
            PuntoSpawn.transform.position,
            PuntoSpawn.transform.rotation
        );

        personajeInstanciado = true;
    }

    private void ContinuarMision()
    {
        if (!BriefingTerminado)
        {
            return;
        }

        Panel.SetActive(false);
        PanelInventario.SetActive(true);
        Minimapa.SetActive(true);

        Time.timeScale = 1f;

        SalteandoBriefing = false;
        MisionIniciada = true;
    }
}
using System.Collections;
using TMPro;
using UnityEngine;

public class MissionBriefing : MonoBehaviour
{
    [Header("Personaje")]
    public GameObject Personaje;
    public GameObject PuntoSpawn;

    [Header("Guardia")]
    public GameObject Guardia;
    public GameObject PuntoSpawnGuardia;

    [Header("Panel")]
    public GameObject Panel;

    [Header("Panel a Ocultar")]
    public GameObject PanelInventario;
    public GameObject Minimapa;

    [Header("Speedrun")]
    public SpeedrunManager speedrunManager;

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
    private bool guardiaInstanciado = false;

    private const string TXT_FECHA = "FECHA\t\t: 06.10.2026";
    private const string TXT_HORA = "HORA\t\t: 02:37 AM";
    private const string TXT_UBICACION = "UBICACIÓN\t\t: LANUS, ARGENTINA";
    private const string TXT_LINEA = "________________________________________________________________________";
    private const string TXT_TITULO = "MISIÓN 01 - El Falsificador";
    private const string TXT_DESCRIPCION = "El mejor falsificador de identidades de la ciudad vive aquí";
    private const string TXT_OBJETIVO = "OBJETIVO : INGRESAR SIN SER DETECTADO Y RECUPERAR DOCUMENTOS";
    private const string TXT_PRESIONA_F = "Presiona [ F ] para EMPEZAR";

    private void Awake()
    {
        PanelInventario.SetActive(false);
        Minimapa.SetActive(false);

        InstanciarPersonaje();
        InstanciarGuardia();
    }

    private void Start()
    {
        speedrunManager = FindAnyObjectByType<SpeedrunManager>();

        Time.timeScale = 0f;

        if (!ActivarBriefing)
        {
            Panel.SetActive(false);
            TerminarBriefing();
            ContinuarMision();
            return;
        }

        Panel.SetActive(true);

        // Modo speedrun: todo el texto aparece de una, sin efecto de escritura
        if (ConfigurationVariables.Instance.SpeedrunMode)
        {
            MostrarTodoElTexto();
            SalteandoBriefing = true;
            TerminarBriefing();
            return;
        }

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
            MostrarTodoElTexto();

            SalteandoBriefing = true;
            BriefingTerminado = true;
            BloquearFHastaSoltar = true;

            return;
        }

        BloquearFHastaSoltar = true;
        ContinuarMision();
    }

    private void MostrarTodoElTexto()
    {
        Fecha.text = TXT_FECHA;
        Hora.text = TXT_HORA;
        Ubicacion.text = TXT_UBICACION;
        Linea1.text = TXT_LINEA;
        TituloMision.text = TXT_TITULO;
        Descripcion.text = TXT_DESCRIPCION;
        Objetivo.text = TXT_OBJETIVO;
        Linea2.text = TXT_LINEA;
        PresionaF.text = TXT_PRESIONA_F;
    }

    private IEnumerator MostrarBriefing()
    {
        yield return StartCoroutine(EscribirTexto(Fecha, TXT_FECHA));
        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        yield return StartCoroutine(EscribirTexto(Hora, TXT_HORA));
        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        yield return StartCoroutine(EscribirTexto(Ubicacion, TXT_UBICACION));
        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        Linea1.text = TXT_LINEA;

        yield return StartCoroutine(EscribirTexto(TituloMision, TXT_TITULO));
        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        yield return StartCoroutine(EscribirTexto(Descripcion, TXT_DESCRIPCION));
        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        yield return StartCoroutine(EscribirTexto(Objetivo, TXT_OBJETIVO));
        yield return new WaitForSecondsRealtime(TiempoEntreTextos);

        Linea2.text = TXT_LINEA;

        yield return new WaitForSecondsRealtime(1f);

        PresionaF.text = TXT_PRESIONA_F;

        TerminarBriefing();
    }

    private IEnumerator EscribirTexto(TextMeshProUGUI texto, string contenido)
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

        Instantiate(Personaje, PuntoSpawn.transform.position, PuntoSpawn.transform.rotation);
        personajeInstanciado = true;
    }

    private void InstanciarGuardia()
    {
        if (guardiaInstanciado)
        {
            return;
        }

        Instantiate(Guardia, PuntoSpawnGuardia.transform.position, PuntoSpawnGuardia.transform.rotation);
        guardiaInstanciado = true;
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

        // Arranca el cronómetro al empezar la misión
        if (speedrunManager != null)
        {
            speedrunManager.IniciarTimer();
        }
    }
}
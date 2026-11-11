using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExitMisionCar : MonoBehaviour
{
    public ShowCanvasNearPlayer ShowCanvasNearPlayer;
    public MissionBriefing MissionBriefing;

    [Header("Camaras")]
    public GameObject MainCamera;
    public GameObject ExitMisionCamera;

    [Header("Referencias")]
    public Image ImageFilled;
    public TextMeshProUGUI TextoAlerta;
    public GameObject Player;
    public GameObject Car;

    [Header("Panel a Ocultar")]
    public Canvas PanelInventario;
    public Canvas Minimapa;

    [Header("Fade de pantalla")]
    public Image PanelNegro;
    public float DuracionFade = 0.2f;
    public float TiempoNegro = 0.1f;

    [Header("Movimiento del auto")]
    public float TiempoQuieto = 0.5f;
    public float Aceleracion = 8f;
    public float VelocidadMaxima = 20f;

    public bool MisionTerminada = false;
    public bool NivelTerminado = false;
    bool mostrandoAlerta = false;

    float fillAmount = 0;
    bool completado = false;

    void Start()
    {
        ShowCanvasNearPlayer = GetComponent<ShowCanvasNearPlayer>();
        MissionBriefing = FindAnyObjectByType<MissionBriefing>();
        Player = FindAnyObjectByType<PlayerController>().gameObject;
        ImageFilled.fillAmount = 0;

        MainCamera.SetActive(true);
        ExitMisionCamera.SetActive(false);

        TextoAlerta.enabled = false;

        // La pantalla comienza transparente
        Color color = PanelNegro.color;
        color.a = 0;
        PanelNegro.color = color;
    }

    void Update()
    {
        // Esperar a que el briefing haya terminado Y la misión haya empezado
        if (!MissionBriefing.MisionIniciada)
        {
            return;
        }

        // Esperar a que se suelte la F usada para empezar la misión
        if (MissionBriefing.BloquearFHastaSoltar)
        {
            return;
        }

        if (Input.GetKey(KeyCode.F) && MisionTerminada && !NivelTerminado && ShowCanvasNearPlayer.FuncionCanvas)
        {
            fillAmount = Mathf.Clamp01(
                fillAmount + Time.deltaTime
            );

            ImageFilled.fillAmount = fillAmount;
        }
        else
        {
            fillAmount = 0f;
            ImageFilled.fillAmount = 0f;
        }

        if (fillAmount >= 1f && !completado)
        {
            completado = true;
            PanelInventario.enabled = false;
            Minimapa.enabled = false;

            StartCoroutine(ExitMision());
        }

        if (Input.GetKey(KeyCode.F) && !MisionTerminada && !NivelTerminado && ShowCanvasNearPlayer.FuncionCanvas && !mostrandoAlerta)
        {
            StartCoroutine(MostrarAlerta());
        }

    }

    IEnumerator ExitMision()
    {
        // Fade a negro
        yield return CambiarAlpha(0f, 1f);

        // Cambiar las cámaras con la pantalla negra
        MainCamera.SetActive(false);
        ExitMisionCamera.SetActive(true);
        Player.SetActive(false);

        StartCoroutine(MoverAuto());

        // Mantener la pantalla negra un instante
        yield return new WaitForSeconds(TiempoNegro);

        // Fade para volver a mostrar la imagen
        yield return CambiarAlpha(1f, 0f);

        NivelTerminado = true;
    }

    IEnumerator CambiarAlpha(float inicio, float fin)
    {
        float tiempo = 0f;

        while (tiempo < DuracionFade)
        {
            tiempo += Time.deltaTime;

            float alpha = Mathf.Lerp(
                inicio,
                fin,
                tiempo / DuracionFade
            );

            Color color = PanelNegro.color;
            color.a = alpha;
            PanelNegro.color = color;

            yield return null;
        }

        Color colorFinal = PanelNegro.color;
        colorFinal.a = fin;
        PanelNegro.color = colorFinal;
    }

    IEnumerator MostrarAlerta()
    {
        mostrandoAlerta = true;

        TextoAlerta.enabled = true;
        TextoAlerta.text = "No se puede salir\r\nNo terminaste el objetivo principal";

        yield return new WaitForSeconds(1.5f);

        TextoAlerta.enabled = false;
        mostrandoAlerta = false;
    }

    IEnumerator MoverAuto()
    {
        // Esperar medio segundo
        yield return new WaitForSeconds(TiempoQuieto);

        float velocidad = 0f;

        // Avanzar hasta Z = -30
        while (Car.transform.position.z > -30f)
        {
            velocidad += Aceleracion * Time.deltaTime;
            velocidad = Mathf.Min(velocidad, VelocidadMaxima);

            Car.transform.position +=
                Vector3.back * velocidad * Time.deltaTime;

            yield return null;
        }

        // Asegurar la posición final
        Vector3 posicion = Car.transform.position;
        posicion.z = -30f;
        Car.transform.position = posicion;

        // Destruir el auto
        Destroy(Car);
    }
}
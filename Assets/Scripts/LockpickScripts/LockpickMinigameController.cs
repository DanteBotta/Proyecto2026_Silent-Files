using UnityEngine;
using UnityEngine.UI;

public class LockpickMinigameController : MonoBehaviour
{
    public enum Resultado
    {
        SinTerminar,
        Correcto,
        Incorrecto
    }

    [Header("Top Indicators")]
    public Image TopIndicatorPanel;
    public Image[] TopIndicators = new Image[5];

    [Header("Lockpick Circle")]
    public Image LockpickCircle;
    public Image GreenZone;
    public Image RotatingLine;

    [Header("Configuración")]
    [Range(0f, 1f)]
    public float GreenFillAmount = 0.2f;

    public float RotationSpeed = 180f;

    [Header("Resultado")]
    public Resultado ResultadoActual = Resultado.SinTerminar;

    // Estado del minijuego
    private bool MinigameActive = false;
    private int IntentoActual = 0;
    private int Aciertos = 0;
    private int Errores = 0;

    // Ángulo central de la zona verde
    private float GreenCenterAngle;

    public bool MinigameFinished
    {
        get { return ResultadoActual != Resultado.SinTerminar; }
    }

    public bool MinigameSuccess
    {
        get { return ResultadoActual == Resultado.Correcto; }
    }

    void Start()
    {
        foreach (Image imagen in TopIndicators)
        {
            imagen.color = Color.white;
        }

        TopIndicatorPanel.gameObject.SetActive(false);
        LockpickCircle.gameObject.SetActive(false);

        GreenZone.fillAmount = GreenFillAmount;
    }

    void Update()
    {
        if (!MinigameActive)
        {
            return;
        }

        // Girar la línea constantemente
        RotatingLine.transform.Rotate(
            0f,
            0f,
            RotationSpeed * Time.deltaTime
        );

        // Intentar con clic izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            ComprobarIntento();
        }
    }

    public void StartLockpickMinigame()
    {
        // Reiniciar el minijuego
        IntentoActual = 0;
        Aciertos = 0;
        Errores = 0;

        ResultadoActual = Resultado.SinTerminar;
        MinigameActive = true;

        // Mostrar interfaz
        TopIndicatorPanel.gameObject.SetActive(true);
        LockpickCircle.gameObject.SetActive(true);

        // Reiniciar indicadores
        foreach (Image imagen in TopIndicators)
        {
            imagen.color = Color.white;
        }

        // Configurar zona verde
        GreenZone.fillAmount = GreenFillAmount;

        PrepararIntento();
    }

    void PrepararIntento()
    {
        GreenCenterAngle = Random.Range(0f, 360f);

        GreenZone.transform.localRotation =
            Quaternion.Euler(0f, 0f, GreenCenterAngle);
    }

    void ComprobarIntento()
    {
        float lineAngle =
            RotatingLine.transform.localEulerAngles.z;

        float greenAngle =
            GreenZone.transform.localEulerAngles.z;

        float diferencia = Mathf.Abs(
            Mathf.DeltaAngle(greenAngle, lineAngle)
        );

        float mitadZonaVerde = GreenZone.fillAmount * 180f;

        bool acierto = diferencia <= mitadZonaVerde;

        if (acierto)
        {
            Aciertos++;
        }
        else
        {
            Errores++;
        }

        ChangeTopIndicatorColor(acierto, IntentoActual);

        IntentoActual++;

        if (Aciertos >= 3)
        {
            FinalizarMinijuego(true);
            return;
        }

        if (Errores >= 3)
        {
            FinalizarMinijuego(false);
            return;
        }

        PrepararIntento();
    }

    void FinalizarMinijuego(bool correcto)
    {
        MinigameActive = false;

        ResultadoActual = correcto
            ? Resultado.Correcto
            : Resultado.Incorrecto;

        LockpickCircle.gameObject.SetActive(false);
        TopIndicatorPanel.gameObject.SetActive(false);
    }

    void ChangeTopIndicatorColor(bool Activado, int Indicator)
    {
        if (Activado)
        {
            TopIndicators[Indicator].color = Color.green;
        }
        else
        {
            TopIndicators[Indicator].color = Color.red;
        }
    }
}
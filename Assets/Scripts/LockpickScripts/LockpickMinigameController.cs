using System.Collections;
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

    [Header("UI a ocultar")]
    [Tooltip("Panel de inventario que se oculta mientras dura el minijuego")]
    public GameObject InventoryPanel;
    [Tooltip("Si esta activo, el inventario vuelve a mostrarse al terminar")]
    public bool RestaurarInventarioAlFinalizar = true;

    [Header("Top Indicators")]
    public Image TopIndicatorPanel;
    public Image[] TopIndicators = new Image[5];

    [Header("Lockpick Circle")]
    public Image LockpickCircle;
    public Image GreenZone;
    public Image RotatingLine;

    [Header("Configuracion")]
    [Range(0.01f, 1f)]
    [Tooltip("Tamano de la zona verde (0.2 = 20% del circulo = 72 grados)")]
    public float GreenFillAmount = 0.2f;

    [Tooltip("Velocidad de giro de la linea en grados por segundo")]
    public float RotationSpeed = 180f;

    [Tooltip("Si la linea gira en sentido horario (true) o antihorario (false)")]
    public bool GiroHorario = true;

    [Tooltip("Ajuste en grados si el sprite de la linea no apunta hacia ARRIBA en rotacion 0")]
    public float LineAngleOffset = 0f;

    [Header("Reglas")]
    public int TotalCirculos = 5;
    public int AciertosNecesarios = 3;
    public int ErroresMaximos = 3;

    [Header("Delays (segundos)")]
    [Tooltip("Espera antes de que la linea empiece a girar en cada circulo")]
    public float DelayInicioCirculo = 0.4f;
    [Tooltip("Espera despues de cada click para ver si acerto o fallo")]
    public float DelayEntreCirculos = 0.8f;
    [Tooltip("Espera despues del ultimo click antes de cerrar el minijuego")]
    public float DelayFinal = 1.2f;

    [Header("Colores")]
    public Color ColorNormal = Color.white;
    public Color ColorAcierto = Color.green;
    public Color ColorError = Color.red;

    [Header("Debug")]
    public bool MostrarDebugs = true;

    [Header("Resultado (leer desde otros scripts)")]
    public Resultado ResultadoActual = Resultado.SinTerminar;

    // Estado interno
    private bool MinigameActive = false;
    private bool LineaGirando = false;
    private int IntentoActual = 0;
    private int Aciertos = 0;
    private int Errores = 0;
    private float GreenStartAngle; // rotacion Z de la zona verde
    private Coroutine rutina;

    // ---- Propiedades publicas de lectura ----
    public bool MinigameRunning { get { return MinigameActive; } }

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
        ConfigurarZonaVerde();
        ResetearIndicadores();

        TopIndicatorPanel.gameObject.SetActive(false);
        LockpickCircle.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!MinigameActive || !LineaGirando)
        {
            return;
        }

        // Girar la linea
        float direccion = GiroHorario ? -1f : 1f;
        RotatingLine.transform.Rotate(0f, 0f, direccion * RotationSpeed * Time.deltaTime);

        // Click izquierdo: frena la linea al instante y comprueba
        if (Input.GetMouseButtonDown(0))
        {
            LineaGirando = false;
            ComprobarIntento();
        }
    }

    // ------------------------------------------------------------

    public void StartLockpickMinigame()
    {
        if (MinigameActive)
        {
            return;
        }

        if (rutina != null)
        {
            StopCoroutine(rutina);
        }

        IntentoActual = 0;
        Aciertos = 0;
        Errores = 0;
        ResultadoActual = Resultado.SinTerminar;
        MinigameActive = true;
        LineaGirando = false;

        if (InventoryPanel != null)
        {
            InventoryPanel.SetActive(false);
        }

        TopIndicatorPanel.gameObject.SetActive(true);
        LockpickCircle.gameObject.SetActive(true);

        ConfigurarZonaVerde();
        ResetearIndicadores();

        Log("Minijuego iniciado.");

        rutina = StartCoroutine(PrepararIntentoConDelay());
    }

    void ConfigurarZonaVerde()
    {
        // Para que el calculo de angulos funcione, la zona verde debe ser
        // radial, empezar arriba y llenarse en sentido horario.
        GreenZone.type = Image.Type.Filled;
        GreenZone.fillMethod = Image.FillMethod.Radial360;
        GreenZone.fillOrigin = (int)Image.Origin360.Top;
        GreenZone.fillClockwise = true;
        GreenZone.fillAmount = GreenFillAmount;
    }

    void ResetearIndicadores()
    {
        foreach (Image imagen in TopIndicators)
        {
            if (imagen != null)
            {
                imagen.color = ColorNormal;
            }
        }
    }

    IEnumerator PrepararIntentoConDelay()
    {
        // Rotacion aleatoria de la zona verde (eje Z)
        GreenStartAngle = Random.Range(0f, 360f);
        GreenZone.transform.localRotation = Quaternion.Euler(0f, 0f, GreenStartAngle);
        GreenZone.fillAmount = GreenFillAmount;

        // La linea arranca en una posicion aleatoria
        RotatingLine.transform.localRotation =
            Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        Log("Circulo " + (IntentoActual + 1) + "/" + TotalCirculos +
            " | Zona verde empieza en " + GreenStartAngle.ToString("F1") +
            " y mide " + (GreenFillAmount * 360f).ToString("F1") + " grados.");

        // Este delay tambien evita que el click que abrio el minijuego cuente
        yield return new WaitForSeconds(DelayInicioCirculo);

        LineaGirando = true;
    }

    void ComprobarIntento()
    {
        // Angulo Z actual de la linea (con ajuste por orientacion del sprite)
        float lineAngle = RotatingLine.transform.localEulerAngles.z + LineAngleOffset;
        float greenStart = GreenZone.transform.localEulerAngles.z;

        // La zona verde empieza en greenStart y se extiende en sentido horario
        // (angulos Z decrecientes) fill * 360 grados.
        float tamanoZona = GreenFillAmount * 360f;
        float distanciaDesdeInicio = Mathf.Repeat(greenStart - lineAngle, 360f);

        bool acierto = distanciaDesdeInicio <= tamanoZona;

        if (acierto) Aciertos++;
        else Errores++;

        Log("Click en circulo " + (IntentoActual + 1) +
            " | linea=" + Mathf.Repeat(lineAngle, 360f).ToString("F1") +
            " | distancia al inicio de la zona=" + distanciaDesdeInicio.ToString("F1") +
            " / " + tamanoZona.ToString("F1") +
            " -> " + (acierto ? "ACIERTO (zona verde)" : "ERROR (anillo rojo)") +
            " | Aciertos=" + Aciertos + " Errores=" + Errores);

        ChangeTopIndicatorColor(acierto, IntentoActual);
        IntentoActual++;

        bool terminado =
            Aciertos >= AciertosNecesarios ||
            Errores >= ErroresMaximos ||
            IntentoActual >= TotalCirculos;

        if (terminado)
        {
            bool correcto = Aciertos >= AciertosNecesarios;
            rutina = StartCoroutine(FinalizarConDelay(correcto));
        }
        else
        {
            rutina = StartCoroutine(SiguienteCirculo());
        }
    }

    IEnumerator SiguienteCirculo()
    {
        yield return new WaitForSeconds(DelayEntreCirculos);
        yield return PrepararIntentoConDelay();
    }

    IEnumerator FinalizarConDelay(bool correcto)
    {
        yield return new WaitForSeconds(DelayFinal);
        FinalizarMinijuego(correcto);
    }

    void FinalizarMinijuego(bool correcto)
    {
        MinigameActive = false;
        LineaGirando = false;

        ResultadoActual = correcto ? Resultado.Correcto : Resultado.Incorrecto;

        LockpickCircle.gameObject.SetActive(false);
        TopIndicatorPanel.gameObject.SetActive(false);

        if (InventoryPanel != null && RestaurarInventarioAlFinalizar)
        {
            InventoryPanel.SetActive(true);
        }

        Log("Minijuego terminado: " + ResultadoActual +
            " (Aciertos=" + Aciertos + ", Errores=" + Errores + ")");
    }

    void ChangeTopIndicatorColor(bool activado, int indicator)
    {
        if (indicator < 0 || indicator >= TopIndicators.Length || TopIndicators[indicator] == null)
        {
            return;
        }

        TopIndicators[indicator].color = activado ? ColorAcierto : ColorError;
    }

    void Log(string mensaje)
    {
        if (MostrarDebugs)
        {
            Debug.Log("[Lockpick] " + mensaje);
        }
    }
}
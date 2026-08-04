using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
 
/// <summary>
/// Controla UN círculo individual del minijuego de ganzúa: la línea que gira,
/// la zona verde/roja, la detección del clic izquierdo, y el fade in/out.
///
/// Requiere en el mismo GameObject (o asignados desde el Inspector):
/// - Un CanvasGroup (para el fade y para bloquear el clic durante la aparición).
/// - Una Image "redRingImage" con Image Type = Filled, Fill Method = Radial 360.
/// - Una Image "greenZoneImage" con el mismo tipo de Fill, hija del círculo.
/// - Un RectTransform "lineTransform" que representa la línea giratoria (un
///   rectángulo fino, con el Pivot en el centro-abajo, tipo aguja de reloj).
/// </summary>
public class LockpickCircleUI : MonoBehaviour
{
    [Header("Referencias visuales")]
    [Tooltip("Anillo rojo de fondo (ocupa todo el círculo, Fill Amount = 1).")]
    public Image redRingImage;
 
    [Tooltip("Sector verde. Debe tener Image Type = Filled, Fill Method = Radial 360.")]
    public Image greenZoneImage;
 
    [Tooltip("RectTransform de la línea que gira alrededor del centro.")]
    public RectTransform lineTransform;
 
    [Tooltip("CanvasGroup usado para el fade in/out y para bloquear clics durante la aparición.")]
    public CanvasGroup canvasGroup;
 
    [Header("Tiempos")]
    [Tooltip("Duración del fade de aparición/desaparición, en segundos.")]
    public float fadeDuration = 0.15f;
 
    // --- Estado interno, seteado por Setup() ---
    private float speedDegreesPerSecond;
    private float zoneWidthDegrees;
    private float zoneStartAngle;
 
    private float currentAngle;
    private bool isSpinning;
    private bool inputEnabled;
    private bool resultAlreadyRegistered;
 
    private System.Action<bool> onResultCallback;
 
    /// <summary>
    /// Inicializa este círculo con los parámetros de dificultad actuales y
    /// arranca la secuencia de aparición. onResult se llama con true (acierto)
    /// o false (fallo) apenas el jugador hace clic.
    /// </summary>
    public void Setup(float speed, float zoneWidth, System.Action<bool> onResult)
    {
        speedDegreesPerSecond = speed;
        zoneWidthDegrees = Mathf.Clamp(zoneWidth, 5f, 360f);
        onResultCallback = onResult;
 
        // Posición aleatoria de la zona verde alrededor del círculo (0-360°)
        zoneStartAngle = UnityEngine.Random.Range(0f, 360f);
 
        currentAngle = 0f;
        isSpinning = false;
        inputEnabled = false;
        resultAlreadyRegistered = false;
 
        ConfigureGreenZoneVisual();
 
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
 
        StartCoroutine(FadeInRoutine());
    }
 
    private void ConfigureGreenZoneVisual()
    {
        // El Fill Amount representa qué porción del círculo completo (360°) ocupa el verde
        greenZoneImage.fillAmount = zoneWidthDegrees / 360f;
 
        // Rotamos el propio sector verde para que empiece en zoneStartAngle.
        // Como el fill "Top" + "Clockwise" arranca en 0° = arriba, rotamos en Z
        // en sentido negativo para que avance en el mismo sentido horario que la línea.
        greenZoneImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -zoneStartAngle);
    }
 
    private IEnumerator FadeInRoutine()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
 
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
 
        // Recién ahora se habilita el clic y empieza a girar la línea
        inputEnabled = true;
        isSpinning = true;
    }
 
    private void Update()
    {
        if (isSpinning)
        {
            currentAngle += speedDegreesPerSecond * Time.deltaTime;
            currentAngle %= 360f;
 
            // Rotamos la línea en sentido horario a medida que currentAngle crece
            lineTransform.localRotation = Quaternion.Euler(0f, 0f, -currentAngle);
        }
 
        if (inputEnabled && !resultAlreadyRegistered && Input.GetMouseButtonDown(0))
        {
            ResolveClick();
        }
    }
 
    private void ResolveClick()
    {
        resultAlreadyRegistered = true;
        inputEnabled = false;
        isSpinning = false;
 
        bool success = IsAngleInsideGreenZone(currentAngle);
 
        if (onResultCallback != null)
        {
            onResultCallback.Invoke(success);
        }
 
        StartCoroutine(FadeOutAndDestroyRoutine());
    }
 
    private bool IsAngleInsideGreenZone(float angle)
    {
        float zoneEndAngle = zoneStartAngle + zoneWidthDegrees;
 
        if (zoneEndAngle <= 360f)
        {
            // Caso simple: la zona no "da la vuelta" por el 0°/360°
            return angle >= zoneStartAngle && angle <= zoneEndAngle;
        }
        else
        {
            // Caso en el que la zona verde cruza el punto 360° -> 0°
            float wrappedEnd = zoneEndAngle - 360f;
            return angle >= zoneStartAngle || angle <= wrappedEnd;
        }
    }
 
    private IEnumerator FadeOutAndDestroyRoutine()
    {
        float t = 0f;
        float startAlpha = canvasGroup.alpha;
 
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / fadeDuration);
            yield return null;
        }
 
        canvasGroup.alpha = 0f;
        Destroy(gameObject);
    }
}
 
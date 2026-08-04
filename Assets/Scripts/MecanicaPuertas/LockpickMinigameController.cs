using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Controla el minijuego completo de ganzúa: genera los 5 círculos en secuencia,
/// maneja la dificultad (que aumenta en cada reintento), cuenta aciertos/fallos,
/// actualiza los 5 indicadores superiores, calcula el resultado final y decide
/// si la puerta se abre, y si la ganzúa se rompe.
///
/// Para usarlo: llamar a StartMinigame() cuando el jugador interactúa con una
/// puerta cerrada. Escuchar los eventos onDoorUnlocked / onAttemptFailed /
/// onToolBroken desde el script de la puerta o del inventario.
/// </summary>
public class LockpickMinigameController : MonoBehaviour
{
    [Header("Referencias de escena")]
    [Tooltip("Prefab del círculo individual (debe tener el script LockpickCircleUI).")]
    public GameObject circlePrefab;
 
    [Tooltip("Transform (dentro del Canvas) donde se instancian los círculos.")]
    public RectTransform circleSpawnPoint;
 
    [Tooltip("Los 5 indicadores pequeños de arriba, en orden (círculo 1 a 5).")]
    public Image[] topIndicators = new Image[5];
 
    [Tooltip("Texto que muestra el objetivo, ej: 'Conseguí 3 para acceder'. Opcional.")]
    public TextMeshProUGUI objectiveText;
 
    [Header("Colores de los indicadores")]
    public Color colorNeutral = Color.gray;
    public Color colorAcierto = Color.green;
    public Color colorFallo = Color.red;
 
    [Header("Reglas generales")]
    [Tooltip("Cantidad de círculos por intento.")]
    public int totalCirculos = 5;
 
    [Tooltip("Aciertos mínimos necesarios para desbloquear la puerta.")]
    public int aciertosNecesarios = 3;
 
    [Header("Dificultad base (intento 1)")]
    [Tooltip("Grados por segundo que gira la línea en el primer intento.")]
    public float velocidadBase = 90f;
 
    [Tooltip("Ancho de la zona verde en grados, en el primer intento.")]
    public float anchoZonaVerdeBase = 70f;
 
    [Tooltip("Probabilidad (0 a 1) de que la ganzúa se rompa al fallar, en el primer intento.")]
    [Range(0f, 1f)]
    public float probabilidadRoturaBase = 0.1f;
 
    [Header("Aumento de dificultad por reintento")]
    public float aumentoVelocidadPorIntento = 25f;
    public float reduccionAnchoVerdePorIntento = 8f;
    [Range(0f, 1f)] public float aumentoRoturaPorIntento = 0.08f;
 
    [Tooltip("Ancho mínimo permitido para la zona verde, para que nunca sea imposible acertar.")]
    public float anchoZonaVerdeMinimo = 20f;
 
    [Header("Eventos")]
    [Tooltip("Se dispara cuando el jugador consigue los aciertos necesarios. La puerta debería abrirse.")]
    public UnityEvent onDoorUnlocked;
 
    [Tooltip("Se dispara cuando el intento falla (menos aciertos de los necesarios) pero todavía se puede reintentar.")]
    public UnityEvent onAttemptFailed;
 
    [Tooltip("Se dispara si la ganzúa se rompe. El juego debería quitarla del inventario y ya no permitir reintentar sin una nueva.")]
    public UnityEvent onToolBroken;
 
    // --- Estado interno ---
    private int intentoActual = 1;
    private int aciertosActuales;
    private bool herramientaRota;
    private bool minijuegoEnCurso;
 
    private void Start()
    {
        ActualizarTextoObjetivo();
        ResetearIndicadores();
    }
 
    /// <summary>
    /// Llamar a este método desde el script de la puerta para arrancar el minijuego.
    /// </summary>
    public void StartMinigame()
    {
        if (minijuegoEnCurso) return;
 
        if (herramientaRota)
        {
            Debug.Log("No se puede intentar: la ganzúa ya está rota.");
            return;
        }
 
        aciertosActuales = 0;
        ResetearIndicadores();
        ActualizarTextoObjetivo();
 
        StartCoroutine(RunMinigameRoutine());
    }
 
    private void ResetearIndicadores()
    {
        for (int i = 0; i < topIndicators.Length; i++)
        {
            if (topIndicators[i] != null)
            {
                topIndicators[i].color = colorNeutral;
            }
        }
    }
 
    private void ActualizarTextoObjetivo()
    {
        if (objectiveText != null)
        {
            objectiveText.text = "Conseguí " + aciertosNecesarios + " para acceder";
        }
    }
 
    private IEnumerator RunMinigameRoutine()
    {
        minijuegoEnCurso = true;
 
        // Calculamos la dificultad de ESTE intento (se mantiene fija durante los 5 círculos,
        // según lo definido en el documento de diseño)
        float velocidadIntento = velocidadBase + (aumentoVelocidadPorIntento * (intentoActual - 1));
        float anchoVerdeIntento = Mathf.Max(
            anchoZonaVerdeMinimo,
            anchoZonaVerdeBase - (reduccionAnchoVerdePorIntento * (intentoActual - 1))
        );
        float probabilidadRoturaIntento = Mathf.Clamp01(
            probabilidadRoturaBase + (aumentoRoturaPorIntento * (intentoActual - 1))
        );
 
        for (int i = 0; i < totalCirculos; i++)
        {
            bool resultadoListo = false;
            bool acierto = false;
 
            GameObject circuloObj = Instantiate(circlePrefab, circleSpawnPoint);
            LockpickCircleUI circuloUI = circuloObj.GetComponent<LockpickCircleUI>();
 
            circuloUI.Setup(velocidadIntento, anchoVerdeIntento, (bool resultado) =>
            {
                acierto = resultado;
                resultadoListo = true;
            });
 
            // Esperamos a que el jugador haga clic y se resuelva este círculo
            yield return new WaitUntil(() => resultadoListo);
 
            if (acierto)
            {
                aciertosActuales++;
                if (topIndicators.Length > i && topIndicators[i] != null)
                {
                    topIndicators[i].color = colorAcierto;
                }
            }
            else
            {
                if (topIndicators.Length > i && topIndicators[i] != null)
                {
                    topIndicators[i].color = colorFallo;
                }
 
                // Solo se evalúa la rotura si la herramienta todavía no se rompió antes
                if (!herramientaRota)
                {
                    float roll = Random.Range(0f, 1f);
                    if (roll <= probabilidadRoturaIntento)
                    {
                        herramientaRota = true;
                    }
                }
            }
 
            // Pequeña pausa entre círculos (además del fade propio del círculo)
            yield return new WaitForSeconds(0.1f);
        }
 
        FinalizarIntento();
 
        minijuegoEnCurso = false;
    }
 
    private void FinalizarIntento()
    {
        bool exito = aciertosActuales >= aciertosNecesarios;
 
        if (exito)
        {
            if (onDoorUnlocked != null) onDoorUnlocked.Invoke();
 
            if (herramientaRota && onToolBroken != null)
            {
                // La puerta se abrió, pero la ganzúa se rompió en el proceso
                onToolBroken.Invoke();
            }
        }
        else
        {
            if (onAttemptFailed != null) onAttemptFailed.Invoke();
 
            if (herramientaRota)
            {
                if (onToolBroken != null) onToolBroken.Invoke();
                // No se incrementa el intento: sin herramienta no hay reintento posible
            }
            else
            {
                // Se puede reintentar, con más dificultad la próxima vez
                intentoActual++;
            }
        }
    }
}
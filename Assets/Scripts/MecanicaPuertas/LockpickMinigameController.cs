using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Controla el minijuego completo de ganzúa: genera los 5 círculos en secuencia,
/// maneja la dificultad (definida manualmente por nivel/intento), cuenta aciertos/
/// fallos, actualiza los 5 indicadores superiores, calcula el resultado final y
/// decide si la puerta se abre, y si la ganzúa se rompe.
///
/// Para usarlo: llamar a StartMinigame() cuando el jugador interactúa con una
/// puerta cerrada. Escuchar los eventos onDoorUnlocked / onAttemptFailed /
/// onToolBroken desde el script de la puerta o del inventario.
/// </summary>
public class LockpickMinigameController : MonoBehaviour
{
    /// <summary>
    /// Representa la dificultad de UN intento completo (los 5 círculos de ese
    /// intento usan siempre estos mismos valores). Se define un elemento por
    /// intento en el array "niveles" de abajo, directamente desde el Inspector.
    /// </summary>
    [Serializable]
    public class NivelDificultad
    {
        [Tooltip("Nombre solo para identificarlo en el Inspector (no se usa en el código).")]
        public string nombre = "Nivel";
 
        [Tooltip("Grados por segundo que gira la línea en este nivel.")]
        public float velocidad = 90f;
 
        [Tooltip("Ancho de la zona verde en grados, en este nivel (más chico = más difícil).")]
        [Range(5f, 360f)]
        public float anchoZonaVerde = 70f;
 
        [Tooltip("Probabilidad (0 a 1) de que la ganzúa se rompa al fallar un círculo en este nivel.")]
        [Range(0f, 1f)]
        public float probabilidadRotura = 0.1f;
    }
 
    [Header("Referencias de escena")]
    [Tooltip("Prefab del círculo individual (debe tener el script LockpickCircleUI).")]
    public GameObject circlePrefab;
 
    [Tooltip("Transform (dentro del Canvas) donde se instancian los círculos.")]
    public RectTransform circleSpawnPoint;
 
    [Tooltip("El panel/objeto padre que contiene los 5 indicadores. El script lo activa al empezar y lo desactiva al terminar.")]
    public GameObject indicatorsPanel;
 
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
 
    [Tooltip("Segundos que quedan visibles los indicadores mostrando el resultado final antes de ocultarse.")]
    public float tiempoMostrarResultado = 1f;
 
    [Header("Niveles de dificultad")]
    [Tooltip("Un elemento por intento: el elemento 0 es el intento 1, el elemento 1 es el intento 2, etc. " +
             "Si el jugador reintenta más veces que niveles definidos, se repite el último nivel de la lista.")]
    public NivelDificultad[] niveles = new NivelDificultad[]
    {
        new NivelDificultad { nombre = "Fácil", velocidad = 80f, anchoZonaVerde = 80f, probabilidadRotura = 0.05f },
        new NivelDificultad { nombre = "Medio",  velocidad = 110f, anchoZonaVerde = 60f, probabilidadRotura = 0.15f },
        new NivelDificultad { nombre = "Difícil", velocidad = 140f, anchoZonaVerde = 45f, probabilidadRotura = 0.30f },
    };

    [Header("Dificultad seleccionada")]
    [Tooltip("La seleccionará otro script antes de comenzar el nivel.")]
    [Range(1,10)]
    public int nivelSeleccionado = 1;
 
    [Header("Eventos")]
    [Tooltip("Se dispara cuando el jugador consigue los aciertos necesarios. La puerta debería abrirse.")]
    public UnityEvent onDoorUnlocked;
 
    [Tooltip("Se dispara cuando el intento falla (menos aciertos de los necesarios) pero todavía se puede reintentar.")]
    public UnityEvent onAttemptFailed;
 
    [Tooltip("Se dispara si la ganzúa se rompe. El juego debería quitarla del inventario y ya no permitir reintentar sin una nueva.")]
    public UnityEvent onToolBroken;
 


    // --- Estado interno ---
    private int aciertosActuales;
    private bool herramientaRota;
    private bool minijuegoEnCurso;
 
    private void Awake()
    {
        // Al arrancar la escena, el panel de indicadores queda oculto hasta que
        // el jugador realmente empiece a forzar una cerradura.
        if (indicatorsPanel != null)
        {
            indicatorsPanel.SetActive(false);
        }
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
 
        if (indicatorsPanel != null)
        {
            indicatorsPanel.SetActive(true);
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
 
    /// <summary>
    /// Devuelve la dificultad correspondiente al intento actual. Si hay más
    /// intentos que niveles definidos, se queda repitiendo el último nivel.
    /// </summary>
    private NivelDificultad ObtenerNivelActual()
    {
        if (niveles == null || niveles.Length == 0)
        {
            return new NivelDificultad
            {
                velocidad = 90f,
                anchoZonaVerde = 60f,
                probabilidadRotura = 0.1f
            };
        }

        int index = Mathf.Clamp(nivelSeleccionado - 1, 0, niveles.Length - 1);

        return niveles[index];
    }
 
    private IEnumerator RunMinigameRoutine()
    {
        minijuegoEnCurso = true;
 
        // La dificultad de ESTE intento se mantiene fija durante los 5 círculos,
        // según lo definido en el documento de diseño.
        NivelDificultad nivel = ObtenerNivelActual();
 
        for (int i = 0; i < totalCirculos; i++)
        {
            bool resultadoListo = false;
            bool acierto = false;
 
            GameObject circuloObj = Instantiate(circlePrefab, circleSpawnPoint);
            LockpickCircleUI circuloUI = circuloObj.GetComponent<LockpickCircleUI>();
 
            circuloUI.Setup(nivel.velocidad, nivel.anchoZonaVerde, (bool resultado) =>
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
            }
 
            // Pequeña pausa entre círculos (además del fade propio del círculo)
            yield return new WaitForSeconds(0.1f);
        }
 
        FinalizarIntento();
 
        // Dejamos el resultado final visible un momento antes de ocultar el panel
        yield return new WaitForSeconds(tiempoMostrarResultado);
 
        if (indicatorsPanel != null)
        {
            indicatorsPanel.SetActive(false);
        }
 
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

            if (!herramientaRota)
            {
                float roll = UnityEngine.Random.Range(0f, 1f);
                if (roll <= ObtenerNivelActual().probabilidadRotura)
                {
                    herramientaRota = true;
                }
            }

            if (herramientaRota && onToolBroken != null)
            {
                onToolBroken.Invoke();
            }
        }
    }
}
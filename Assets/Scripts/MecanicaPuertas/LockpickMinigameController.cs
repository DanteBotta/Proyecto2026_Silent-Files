using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Controla el minijuego completo de ganzúa.
/// Se inicia desde una SimpleDoor mediante StartMinigameForDoor().
/// </summary>
public class LockpickMinigameController : MonoBehaviour
{
    [Serializable]
    public class NivelDificultad
    {
        public string nombre = "Nivel";
        public float velocidad = 90f;

        [Range(5f, 360f)]
        public float anchoZonaVerde = 70f;

        [Range(0f, 1f)]
        public float probabilidadRotura = 0.1f;
    }

    [Header("Referencias de escena")]
    public GameObject circlePrefab;
    public RectTransform circleSpawnPoint;
    public GameObject indicatorsPanel;
    public GameObject inventoryPanel;
    public Image[] topIndicators = new Image[5];
    public TextMeshProUGUI objectiveText;

    [Header("Colores")]
    public Color colorNeutral = Color.gray;
    public Color colorAcierto = Color.green;
    public Color colorFallo = Color.red;

    [Header("Reglas")]
    public int totalCirculos = 5;
    public int aciertosNecesarios = 3;
    public float tiempoMostrarResultado = 1f;

    [Header("Niveles de dificultad")]
    public NivelDificultad[] niveles = new NivelDificultad[]
    {
        new NivelDificultad
        {
            nombre = "Fácil",
            velocidad = 80f,
            anchoZonaVerde = 80f,
            probabilidadRotura = 0.05f
        },

        new NivelDificultad
        {
            nombre = "Medio",
            velocidad = 110f,
            anchoZonaVerde = 60f,
            probabilidadRotura = 0.15f
        },

        new NivelDificultad
        {
            nombre = "Difícil",
            velocidad = 140f,
            anchoZonaVerde = 45f,
            probabilidadRotura = 0.30f
        }
    };

    [Header("Dificultad seleccionada")]
    [Range(1, 10)]
    public int nivelSeleccionado = 1;

    [Header("Eventos opcionales")]
    public UnityEvent onDoorUnlocked;
    public UnityEvent onAttemptFailed;
    public UnityEvent onToolBroken;

    private int aciertosActuales;
    private bool herramientaRota;
    private bool minijuegoEnCurso;

    private SimpleDoor puertaActual;

    private void Awake()
    {
        if (indicatorsPanel != null)
            indicatorsPanel.SetActive(false);
    }

    // =========================================================
    // INICIAR MINIJUEGO PARA UNA PUERTA
    // =========================================================

    public void StartMinigameForDoor(SimpleDoor door)
    {
        if (door == null)
        {
            Debug.LogError("LockpickMinigameController: puerta null.");
            return;
        }

        if (minijuegoEnCurso)
            return;

        if (herramientaRota)
        {
            Debug.Log("La ganzúa está rota.");
            return;
        }

        puertaActual = door;

        StartMinigame();
    }

    // =========================================================
    // INICIAR
    // =========================================================

    public void StartMinigame()
    {
        if (minijuegoEnCurso)
            return;

        if (herramientaRota)
            return;

        if (puertaActual == null)
        {
            Debug.LogWarning(
                "LockpickMinigameController: no hay puerta seleccionada."
            );

            return;
        }

        if (indicatorsPanel != null)
            indicatorsPanel.SetActive(true);
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        aciertosActuales = 0;

        ResetearIndicadores();
        ActualizarTextoObjetivo();

        StartCoroutine(RunMinigameRoutine());
    }

    // =========================================================
    // INDICADORES
    // =========================================================

    private void ResetearIndicadores()
    {
        for (int i = 0; i < topIndicators.Length; i++)
        {
            if (topIndicators[i] != null)
                topIndicators[i].color = colorNeutral;
        }
    }

    private void ActualizarTextoObjetivo()
    {
        if (objectiveText != null)
        {
            objectiveText.text =
                "Conseguí " +
                aciertosNecesarios +
                " para acceder";
        }
    }

    // =========================================================
    // DIFICULTAD
    // =========================================================

    private NivelDificultad ObtenerNivelActual()
    {
        if (niveles == null || niveles.Length == 0)
        {
            return new NivelDificultad
            {
                nombre = "Predeterminado",
                velocidad = 90f,
                anchoZonaVerde = 60f,
                probabilidadRotura = 0.1f
            };
        }

        int index = Mathf.Clamp(
            nivelSeleccionado - 1,
            0,
            niveles.Length - 1
        );

        return niveles[index];
    }

    // =========================================================
    // MINIJUEGO
    // =========================================================

    private IEnumerator RunMinigameRoutine()
    {
        minijuegoEnCurso = true;

        NivelDificultad nivel = ObtenerNivelActual();

        int fallosActuales = 0;

        int fallosMaximosAntesDeSalirTemprano =
            totalCirculos - aciertosNecesarios + 1;

        for (int i = 0; i < totalCirculos; i++)
        {
            bool resultadoListo = false;
            bool acierto = false;

            GameObject circuloObj =
                Instantiate(circlePrefab, circleSpawnPoint);

            LockpickCircleUI circuloUI =
                circuloObj.GetComponent<LockpickCircleUI>();

            if (circuloUI == null)
            {
                Debug.LogError(
                    "El Circle Prefab no tiene LockpickCircleUI."
                );

                Destroy(circuloObj);

                break;
            }

            circuloUI.Setup(
                nivel.velocidad,
                nivel.anchoZonaVerde,
                resultado =>
                {
                    acierto = resultado;
                    resultadoListo = true;
                }
            );

            yield return new WaitUntil(
                () => resultadoListo
            );

            if (acierto)
            {
                aciertosActuales++;

                if (topIndicators.Length > i &&
                    topIndicators[i] != null)
                {
                    topIndicators[i].color = colorAcierto;
                }
            }
            else
            {
                fallosActuales++;

                if (topIndicators.Length > i &&
                    topIndicators[i] != null)
                {
                    topIndicators[i].color = colorFallo;
                }
            }

            yield return new WaitForSeconds(0.1f);

            bool yaGano =
                aciertosActuales >= aciertosNecesarios;

            bool yaEsImposibleGanar =
                fallosActuales >= fallosMaximosAntesDeSalirTemprano;

            if (yaGano || yaEsImposibleGanar)
                break;
        }

        FinalizarIntento(nivel);

        yield return new WaitForSeconds(
            tiempoMostrarResultado
        );

        if (indicatorsPanel != null)
            indicatorsPanel.SetActive(false);
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);

        minijuegoEnCurso = false;
    }

    // =========================================================
    // RESULTADO
    // =========================================================

    private void FinalizarIntento(NivelDificultad nivel)
    {
        bool exito =
            aciertosActuales >= aciertosNecesarios;

        if (exito)
        {
            if (puertaActual != null)
                puertaActual.LockpickSucceeded();

            onDoorUnlocked?.Invoke();

            puertaActual = null;

            return;
        }

        if (puertaActual != null)
            puertaActual.LockpickFailed();

        onAttemptFailed?.Invoke();

        if (!herramientaRota)
        {
            float roll =
                UnityEngine.Random.Range(0f, 1f);

            if (roll <= nivel.probabilidadRotura)
                herramientaRota = true;
        }

        if (herramientaRota)
        {
            if (puertaActual != null)
                puertaActual.LockpickBroken();

            onToolBroken?.Invoke();
        }

        puertaActual = null;
    }
}
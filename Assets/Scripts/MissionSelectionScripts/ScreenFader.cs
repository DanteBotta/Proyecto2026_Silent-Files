using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Fade con un panel negro SOLO para esta escena (no persiste a otras escenas).
/// Lo crea el MissionSelectionManager solo, no hay que ponerlo a mano.
///
/// - Al empezar la escena arranca en negro y se aclara (entrada).
/// - LoadScene(...) oscurece la pantalla y recién después carga la otra escena (salida).
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private float _duration = 0.5f;
    private CanvasGroup _group;
    private Coroutine _fadeRoutine;
    private bool _leaving;

    /// <summary>True cuando ya se está yendo a otra escena.</summary>
    public bool IsLeaving => _leaving;

    /// <summary>Crea el panel negro en la escena actual (arranca totalmente negro).</summary>
    public static ScreenFader Create(float duration = 0.5f)
    {
        var go = new GameObject("ScreenFader");
        var fader = go.AddComponent<ScreenFader>();
        fader._duration = Mathf.Max(0.01f, duration);
        return fader;
    }

    private void Awake()
    {
        BuildOverlay();
        _group.alpha = 1f;            // arranca en negro para que no haya un parpadeo
        _group.blocksRaycasts = true;
    }

    private void Start()
    {
        // Entrada: del negro a la escena
        _fadeRoutine = StartCoroutine(FadeRoutine(0f, true));
    }

    /// <summary>Fade a negro y carga la escena. Devuelve false si no pudo empezar.</summary>
    public bool LoadScene(string sceneName)
    {
        if (_leaving) return false;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[ScreenFader] La escena '{sceneName}' no existe o no está agregada en Build Settings / Build Profiles.");
            return false;
        }

        _leaving = true;
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        StartCoroutine(LeaveRoutine(sceneName));
        return true;
    }

    private IEnumerator LeaveRoutine(string sceneName)
    {
        yield return FadeRoutine(1f, false);   // salida: a negro
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeRoutine(float target, bool waitOneFrame)
    {
        // Un frame de espera para que los Start() de la escena ocurran bajo el negro
        if (waitOneFrame) yield return null;

        float start = _group.alpha;
        _group.blocksRaycasts = true; // mientras hay fade no se puede clickear nada

        for (float t = 0f; t < 1f; t += Mathf.Min(Time.unscaledDeltaTime, 0.05f) / _duration)
        {
            _group.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }

        _group.alpha = target;
        _group.blocksRaycasts = target > 0.001f;
        _fadeRoutine = null;
    }

    private void BuildOverlay()
    {
        var canvasGO = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // por encima de cualquier otro canvas
        _group = canvasGO.GetComponent<CanvasGroup>();

        var panelGO = new GameObject("BlackPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasGO.transform, false);
        var rt = (RectTransform)panelGO.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        panelGO.GetComponent<Image>().color = Color.black;
    }
}
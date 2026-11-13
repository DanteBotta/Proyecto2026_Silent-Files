using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// Para usar el Input System NUEVO en el teclado: agregar el scripting define MISSION_USE_NEW_INPUT
// (Project Settings > Player > Scripting Define Symbols) y asegurarse de que el paquete
// "Input System" esté instalado y referenciado por el asmdef de esta carpeta (si tiene uno).
#if MISSION_USE_NEW_INPUT
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Gestiona las misiones del mapa.
/// - Detecta automáticamente los paneles (misiones) que están dentro de la carpeta.
/// - Genera los botones del menú inferior (siempre visibles, con nombre + imagen).
///   Solo el botón de la misión elegida muestra el marcador "Selected".
/// - Al elegir una misión, la hoja:
///     1) se acerca a la cámara (se "levanta"),
///     2) se desplaza hasta la mesa,
///     3) baja (se aleja de la cámara) y queda apoyada.
///   La hoja NUNCA rota: mantiene su rotación original (mirando a la cámara).
/// - Si elegís otra, la hoja actual hace el recorrido inverso y recién después sale la nueva.
/// - Next() / Previous(): botones en pantalla (mouse/touch) y flechas izquierda/derecha del teclado.
/// </summary>
public class MissionSelectionManager : MonoBehaviour
{
    public enum DiscoveryMode
    {
        /// <summary>Cada Canvas (World Space) dentro de la carpeta; sus hijos directos son las misiones.</summary>
        ChildrenOfCanvases,
        /// <summary>Los hijos directos de "folderRoot" son las misiones.</summary>
        DirectChildrenOfRoot,
        /// <summary>Todo objeto que tenga el componente MissionPanelData dentro de la carpeta.</summary>
        MarkedPanels
    }

    [Serializable]
    private class Mission
    {
        public Transform panel;
        public string displayName;
        public Sprite thumbnail;
        public int difficulty;

        // Posición "guardada en la carpeta" (la rotación no se toca nunca)
        public Transform homeParent;
        public Vector3 homeLocalPos;
        public Vector3 homeLocalScale;

        public MissionPanelData data;   // puede ser null (sin datos = desbloqueada)
        public Button button;
        public GameObject menuButton;
        public GameObject selectedMarker;
        public GameObject lockedMarker;
        public Transform difficultyPanel;

        /// <summary>Bloqueada = solo visual (panel con candado, sin estrellas).</summary>
        public bool Locked => data != null && data.isLocked;

        /// <summary>Si se puede elegir. Si no, no se toca, se salta con flechas y la hoja nunca sale.</summary>
        public bool Selectable => data == null || data.isSelectable;

        /// <summary>Las estrellas solo se ven si no está bloqueada y se puede seleccionar.</summary>
        public bool ShowDifficulty => !Locked && Selectable;

        public Vector3 HomeWorldPos => homeParent ? homeParent.TransformPoint(homeLocalPos) : homeLocalPos;
    }

    // ───────────────────────── Escena ─────────────────────────
    [Header("Escena")]
    [Tooltip("Modelo 3D de la carpeta (contiene los canvas con los paneles).")]
    [SerializeField] private Transform folderRoot;

    [Tooltip("Empty ubicado sobre la mesa, al lado de la carpeta. Solo importa su POSICIÓN: ahí queda apoyada la hoja. Su rotación se ignora.")]
    [SerializeField] private Transform displayAnchor;

    [Tooltip("Cámara que mira la escena desde arriba. Si queda vacío usa Camera.main.")]
    [SerializeField] private Camera viewCamera;

    [SerializeField] private DiscoveryMode discoveryMode = DiscoveryMode.ChildrenOfCanvases;

    [Tooltip("Los paneles no seleccionados quedan desactivados (evita solapamientos).")]
    [SerializeField] private bool hideInactivePanels = true;

    [Tooltip("Al iniciar, abre la primera misión automáticamente.")]
    [SerializeField] private bool openFirstOnStart = false;

    [Tooltip("Si se toca la misión ya abierta, se guarda de nuevo en la carpeta.")]
    [SerializeField] private bool allowToggleOff = true;

    [Tooltip("Next/Previous dan la vuelta al llegar al final.")]
    [SerializeField] private bool wrapAround = true;

    // ───────────────────────── Animación ─────────────────────────
    [Header("Animación")]
    [Tooltip("Cuánto se acerca la hoja a la cámara al levantarse (en metros).")]
    [SerializeField] private float liftDistance = 0.15f;

    [Tooltip("Tiempo de levantarse (acercarse a la cámara).")]
    [SerializeField] private float liftDuration = 0.3f;

    [Tooltip("Tiempo de desplazarse de la carpeta a la mesa (y de vuelta).")]
    [SerializeField] private float travelDuration = 0.5f;

    [Tooltip("Tiempo de bajar (alejarse de la cámara) hasta apoyarse.")]
    [SerializeField] private float dropDuration = 0.25f;

    [Tooltip("Escala de la hoja cuando está en la mesa, para poder leerla. 1 = mismo tamaño.")]
    [SerializeField] private float displayScaleMultiplier = 1.6f;

    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ───────────────────────── UI ─────────────────────────
    [Header("Menú inferior")]
    [Tooltip("Contenedor del menú (idealmente con HorizontalLayoutGroup).")]
    [SerializeField] private RectTransform menuContainer;

    [Tooltip("Prefab con Button. Opcional: TMP_Text (nombre), hijo 'Icon' (Image), hijo 'Selected' (marca de selección), hijo 'Dificultad' (panel con 5 Images de estrellas) e hijo 'PanelBloqueado' (panel transparente con un candado grande).")]
    [SerializeField] private Button menuButtonPrefab;

    [Header("Dificultad (estrellas)")]
    [Tooltip("Imagen de estrella completa (se usa para cada punto de dificultad).")]
    [SerializeField] private Sprite starFull;

    [Tooltip("Imagen de estrella vacía (se usa para los puntos que faltan hasta 5).")]
    [SerializeField] private Sprite starEmpty;

    [Header("Menú: separación y scroll")]
    [Tooltip("Separación mínima entre botones (px).")]
    [SerializeField] private float menuSpacing = 12f;

    [Tooltip("Margen a los costados del menú (px).")]
    [SerializeField] private float menuPadding = 12f;

    [Tooltip("Ancho de cada botón (px). 0 = usa el ancho del prefab. Los botones nunca se achican.")]
    [SerializeField] private float buttonWidth = 0f;

    [Tooltip("Muestra una scrollbar abajo cuando los botones no entran en pantalla.")]
    [SerializeField] private bool showScrollbar = true;

    [SerializeField] private float scrollbarHeight = 10f;

    [Header("Flechas")]
    [Tooltip("Botones en pantalla (mouse o touch). Opcionales.")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Tooltip("Usar el teclado: flechas izquierda/derecha para cambiar y Enter para confirmar.")]
    [SerializeField] private bool keyboardNavigation = true;

    [Header("Confirmar misión")]
    [Tooltip("Nombre exacto de la escena de juego. Tiene que estar agregada en Build Settings / Build Profiles.")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Tooltip("Botón en pantalla para confirmar (opcional, útil en celular). Enter en el teclado hace lo mismo.")]
    [SerializeField] private Button confirmButton;

    [Tooltip("Duración (segundos) del fade al entrar y al salir de esta escena.")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Ayuda")]
    [Tooltip("Texto de ayuda para quien no sabe cómo elegir. Aparece después de 'helpDelay' segundos de iniciada la escena y desaparece al apretar Enter.")]
    public TMP_Text helpText;

    [Tooltip("Segundos desde que arranca la escena hasta que aparece el texto de ayuda.")]
    [SerializeField] private float helpDelay = 10f;

    [Header("Debug")]
    [Tooltip("Activa o desactiva TODOS los mensajes de debug de este script (selección, confirmación, avisos y advertencias).")]
    public bool DebugsConsola = false;

    // ───────────────────────── Eventos ─────────────────────────
    /// <summary>Se dispara cuando cambia la misión objetivo (-1 = ninguna).</summary>
    public event Action<int> OnMissionChanged;
    /// <summary>Se dispara cuando la hoja terminó de apoyarse en la mesa.</summary>
    public event Action<int> OnMissionShown;
    /// <summary>Se dispara cuando la hoja terminó de guardarse en la carpeta.</summary>
    public event Action<int> OnMissionHidden;

    private readonly List<Mission> _missions = new List<Mission>();
    private int _shownIndex = -1;   // lo que está realmente en la mesa
    private int _targetIndex = -1;  // lo que el jugador pidió
    private Coroutine _process;

    public int MissionCount => _missions.Count;
    public int CurrentIndex => _targetIndex;
    public bool IsAnimating => _process != null;

    // ───────────────────────── Ciclo de vida ─────────────────────────
    private void Awake()
    {
        if (folderRoot == null) folderRoot = transform;

        // Fade solo de esta escena: entra desde negro y sale a negro al ir a Gameplay
        _fader = ScreenFader.Create(fadeDuration);
    }

    private void Start()
    {
        if (DebugsConsola && displayAnchor == null)
            Debug.LogWarning("[MissionSelectionManager] Falta asignar 'displayAnchor'. Se usará una posición por defecto junto a la carpeta.", this);

#if !MISSION_USE_NEW_INPUT && !ENABLE_LEGACY_INPUT_MANAGER
        if (DebugsConsola && keyboardNavigation)
            Debug.LogWarning("[MissionSelectionManager] Teclado desactivado: el proyecto usa solo el Input System nuevo. Agregá el define MISSION_USE_NEW_INPUT para activarlo.", this);
#endif

        SetupMenuLayout();
        Rescan();

        if (previousButton) previousButton.onClick.AddListener(Previous);
        if (nextButton) nextButton.onClick.AddListener(Next);
        if (confirmButton) confirmButton.onClick.AddListener(ConfirmSelection);

        // Ayuda: arranca oculta y aparece a los 'helpDelay' segundos (se reinicia cada vez que se carga la escena)
        if (helpText)
        {
            helpText.gameObject.SetActive(false);
            _helpRoutine = StartCoroutine(HelpRoutine());
        }

        if (openFirstOnStart && _missions.Count > 0)
            Next(); // la primera misión desbloqueada
    }

    private void OnDestroy()
    {
        if (previousButton) previousButton.onClick.RemoveListener(Previous);
        if (nextButton) nextButton.onClick.RemoveListener(Next);
        if (confirmButton) confirmButton.onClick.RemoveListener(ConfirmSelection);
    }

    private void Update()
    {
        if (!keyboardNavigation) return;

#if MISSION_USE_NEW_INPUT
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.rightArrowKey.wasPressedThisFrame) Next();
        else if (kb.leftArrowKey.wasPressedThisFrame) Previous();
        else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) ConfirmSelection();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.RightArrow)) Next();
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) Previous();
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ConfirmSelection();
#endif
    }

    // ───────────────────────── Descubrimiento automático ─────────────────────────
    [ContextMenu("Rescan Missions")]
    public void Rescan()
    {
        // Si había algo abierto, lo devolvemos a su lugar sin animar
        if (_process != null) { StopCoroutine(_process); _process = null; }
        foreach (var m in _missions)
        {
            if (m.panel == null) continue;
            SnapHome(m);
        }
        ClearMenu();
        _missions.Clear();
        _shownIndex = -1;
        _targetIndex = -1;

        var found = new List<Transform>();
        Transform root = folderRoot ? folderRoot : transform;

        switch (discoveryMode)
        {
            case DiscoveryMode.ChildrenOfCanvases:
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                    foreach (Transform child in canvas.transform)
                        found.Add(child);
                break;

            case DiscoveryMode.DirectChildrenOfRoot:
                foreach (Transform child in root)
                    found.Add(child);
                break;

            case DiscoveryMode.MarkedPanels:
                foreach (var data in root.GetComponentsInChildren<MissionPanelData>(true))
                    found.Add(data.transform);
                break;
        }

        foreach (var t in found)
        {
            var data = t.GetComponent<MissionPanelData>();
            _missions.Add(new Mission
            {
                panel = t,
                displayName = data && !string.IsNullOrEmpty(data.displayName) ? data.displayName : t.name,
                thumbnail = data ? data.thumbnail : null,
                difficulty = data ? data.Difficulty : MissionPanelData.MinDifficulty,
                data = data,
                homeParent = t.parent,
                homeLocalPos = t.localPosition,
                homeLocalScale = t.localScale
            });
        }

        // Las hojas bloqueadas nunca se muestran, aunque hideInactivePanels esté apagado
        foreach (var m in _missions)
            if (hideInactivePanels || !m.Selectable) m.panel.gameObject.SetActive(false);

        BuildMenu();
        RefreshMenuHighlight();
        if (DebugsConsola)
            Debug.Log($"[MissionSelectionManager] {_missions.Count} misiones encontradas.", this);
    }

    // ───────────────────────── Menú ─────────────────────────
    private void ClearMenu()
    {
        foreach (var m in _missions)
            if (m.menuButton) Destroy(m.menuButton);
    }

    /// <summary>Crea un botón por misión. Todos quedan visibles todo el tiempo.</summary>
    private void BuildMenu()
    {
        if (menuContainer == null || menuButtonPrefab == null) return;

        for (int i = 0; i < _missions.Count; i++)
        {
            int index = i; // captura local para el closure
            var m = _missions[i];

            Button btn = Instantiate(menuButtonPrefab, menuContainer);
            btn.gameObject.name = $"MissionButton_{m.displayName}";
            btn.onClick.AddListener(() => SelectMission(index));

            var label = btn.GetComponentInChildren<TMP_Text>(true);
            if (label) label.text = m.displayName;

            var iconT = btn.transform.Find("Icon");
            if (iconT && iconT.TryGetComponent(out Image icon))
            {
                icon.sprite = m.thumbnail;
                icon.enabled = m.thumbnail != null;
            }

            // Estrellas de dificultad: panel hijo llamado "Dificultad" con 5 Images
            m.difficultyPanel = ApplyDifficultyStars(btn.transform, m.difficulty);

            // El marcador "Selected" solo se ve en la misión elegida
            var selected = btn.transform.Find("Selected");
            if (selected) { m.selectedMarker = selected.gameObject; selected.gameObject.SetActive(false); }

            // Ancho fijo: si hay muchas misiones los botones NO se aprietan, el menú se hace scrolleable
            float w = buttonWidth > 0f ? buttonWidth : ((RectTransform)menuButtonPrefab.transform).rect.width;
            if (w <= 0f) w = 140f;
            if (!btn.TryGetComponent(out LayoutElement le)) le = btn.gameObject.AddComponent<LayoutElement>();
            le.minWidth = w;
            le.preferredWidth = w;
            le.flexibleWidth = 0f;

            // Panel bloqueado: hijo del botón llamado "PanelBloqueado", solo se ve si la misión está bloqueada
            m.button = btn;
            var lockedT = FindDeep(btn.transform, "PanelBloqueado");
            if (lockedT == null) lockedT = FindDeep(btn.transform, "Panel Bloqueado");
            if (lockedT == null) lockedT = FindDeep(btn.transform, "Locked");
            if (lockedT)
            {
                m.lockedMarker = lockedT.gameObject;
                lockedT.SetAsLastSibling(); // queda por delante del nombre y la imagen
            }
            ApplyLockVisual(m);

            m.menuButton = btn.gameObject;
        }

        // Recalcular el layout y arrancar el scroll desde la izquierda
        if (_scroll != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(menuContainer);
            _scroll.horizontalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// Busca el panel "Dificultad" dentro del botón y pinta sus estrellas:
    /// las primeras "difficulty" con la estrella completa y el resto con la vacía.
    /// </summary>
    private Transform ApplyDifficultyStars(Transform buttonRoot, int difficulty)
    {
        // Busca "Dificultad" en cualquier nivel del botón (también acepta "Difficulty")
        Transform panel = FindDeep(buttonRoot, "Dificultad");
        if (panel == null) panel = FindDeep(buttonRoot, "Difficulty");
        if (panel == null) return null;

        difficulty = Mathf.Clamp(difficulty, MissionPanelData.MinDifficulty, MissionPanelData.MaxDifficulty);

        // Las estrellas son las Images hijas directas del panel, en orden de jerarquía
        int star = 0;
        foreach (Transform child in panel)
        {
            if (!child.TryGetComponent(out Image img)) continue;

            Sprite sprite = star < difficulty ? starFull : starEmpty;
            if (sprite != null) img.sprite = sprite;
            star++;
        }

        if (DebugsConsola && star != MissionPanelData.MaxDifficulty)
            Debug.LogWarning($"[MissionSelectionManager] El panel 'Dificultad' tiene {star} imágenes hijas; se esperaban {MissionPanelData.MaxDifficulty}.", panel);

        if (DebugsConsola && (starFull == null || starEmpty == null))
            Debug.LogWarning("[MissionSelectionManager] Faltan asignar 'starFull' y/o 'starEmpty' en el inspector.", this);

        return panel;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void RefreshMenuHighlight()
    {
        for (int i = 0; i < _missions.Count; i++)
            if (_missions[i].selectedMarker)
                _missions[i].selectedMarker.SetActive(i == _targetIndex);
    }

    // ───────────────────────── Layout y scroll del menú ─────────────────────────
    private ScrollRect _scroll;
    private Coroutine _scrollRoutine;

    /// <summary>
    /// Configura el menú: separación mínima entre botones, contenido que crece según la cantidad
    /// y un ScrollRect horizontal (se crea solo si el menú no está ya dentro de uno).
    /// </summary>
    private void SetupMenuLayout()
    {
        if (menuContainer == null) return;

        // 1) Fila horizontal con separación mínima
        if (!menuContainer.TryGetComponent(out HorizontalLayoutGroup layout))
            layout = menuContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = menuSpacing;
        layout.padding = new RectOffset(Mathf.RoundToInt(menuPadding), Mathf.RoundToInt(menuPadding), 0, 0);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // 2) El contenido crece a lo ancho según la cantidad de botones
        if (!menuContainer.TryGetComponent(out ContentSizeFitter fitter))
            fitter = menuContainer.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // 3) Scroll: usar el existente o crear uno
        _scroll = menuContainer.GetComponentInParent<ScrollRect>();
        if (_scroll == null)
        {
            _scroll = BuildScrollRect();
        }
        else
        {
            _scroll.content = menuContainer;
            _scroll.horizontal = true;
            _scroll.vertical = false;
        }
    }

    private ScrollRect BuildScrollRect()
    {
        RectTransform content = menuContainer;
        var parent = content.parent as RectTransform;
        if (parent == null) return null;

        // Raíz del scroll: ocupa el mismo lugar que ocupaba el menú
        var rootGO = new GameObject("MissionMenu_Scroll", typeof(RectTransform));
        var root = (RectTransform)rootGO.transform;
        root.SetParent(parent, false);
        root.SetSiblingIndex(content.GetSiblingIndex());
        root.anchorMin = content.anchorMin;
        root.anchorMax = content.anchorMax;
        root.pivot = content.pivot;
        root.anchoredPosition = content.anchoredPosition;
        root.sizeDelta = content.sizeDelta;

        // Viewport: recorta lo que se sale y permite arrastrar también en los huecos
        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        var viewport = (RectTransform)viewportGO.transform;
        viewport.SetParent(root, false);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(0f, showScrollbar ? scrollbarHeight + 2f : 0f);
        viewport.offsetMax = Vector2.zero;
        viewportGO.GetComponent<Image>().color = Color.clear;

        // El menú pasa a ser el "Content" del scroll
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0.5f, 0f);
        content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 0.5f);   // si todo entra, queda centrado
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        var scroll = rootGO.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.1f;
        scroll.inertia = true;
        scroll.scrollSensitivity = 20f;

        if (showScrollbar)
        {
            scroll.horizontalScrollbar = BuildScrollbar(root);
            scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        return scroll;
    }

    private Scrollbar BuildScrollbar(RectTransform parent)
    {
        var sbGO = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        var sbRT = (RectTransform)sbGO.transform;
        sbRT.SetParent(parent, false);
        sbRT.anchorMin = new Vector2(0f, 0f);
        sbRT.anchorMax = new Vector2(1f, 0f);
        sbRT.pivot = new Vector2(0.5f, 0f);
        sbRT.sizeDelta = new Vector2(0f, scrollbarHeight);
        sbRT.anchoredPosition = Vector2.zero;
        sbGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

        var areaGO = new GameObject("Sliding Area", typeof(RectTransform));
        var area = (RectTransform)areaGO.transform;
        area.SetParent(sbRT, false);
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = Vector2.zero;
        area.offsetMax = Vector2.zero;

        var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        var handle = (RectTransform)handleGO.transform;
        handle.SetParent(area, false);
        handle.sizeDelta = Vector2.zero;
        var handleImg = handleGO.GetComponent<Image>();
        handleImg.color = new Color(1f, 1f, 1f, 0.8f);

        var sb = sbGO.GetComponent<Scrollbar>();
        sb.handleRect = handle;
        sb.targetGraphic = handleImg;
        sb.direction = Scrollbar.Direction.LeftToRight;
        return sb;
    }

    /// <summary>Si la misión elegida quedó fuera de la pantalla, desliza el menú hasta mostrarla.</summary>
    private void ScrollToSelected()
    {
        if (_scroll == null || _targetIndex < 0 || _targetIndex >= _missions.Count) return;
        var btn = _missions[_targetIndex].menuButton;
        if (btn == null) return;

        if (_scrollRoutine != null) StopCoroutine(_scrollRoutine);
        _scrollRoutine = StartCoroutine(ScrollToRoutine((RectTransform)btn.transform));
    }

    private IEnumerator ScrollToRoutine(RectTransform target)
    {
        Canvas.ForceUpdateCanvases();

        RectTransform content = _scroll.content;
        RectTransform viewport = _scroll.viewport ? _scroll.viewport : (RectTransform)_scroll.transform;

        float contentW = content.rect.width;
        float overflow = contentW - viewport.rect.width;
        if (overflow <= 1f) { _scrollRoutine = null; yield break; } // todo entra: no hay nada que scrollear

        // Centro del botón dentro del contenido (0..contentW) y posición normalizada para centrarlo
        float centerX = content.InverseTransformPoint(target.TransformPoint(target.rect.center)).x
                        + contentW * content.pivot.x;
        float desired = Mathf.Clamp01((centerX - viewport.rect.width * 0.5f) / overflow);

        _scroll.StopMovement();
        float start = _scroll.horizontalNormalizedPosition;
        const float duration = 0.25f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _scroll.horizontalNormalizedPosition =
                Mathf.LerpUnclamped(start, desired, ease.Evaluate(Mathf.Clamp01(t / duration)));
            yield return null;
        }
        _scroll.horizontalNormalizedPosition = desired;
        _scrollRoutine = null;
    }

    // ───────────────────────── API pública ─────────────────────────
    public void SelectMission(int index)
    {
        if (index < 0 || index >= _missions.Count) return;
        if (!_missions[index].Selectable) return; // no seleccionable: no hace nada

        // Tocar la misma misión => guardarla (si está permitido)
        if (index == _targetIndex)
        {
            if (allowToggleOff) SetTarget(-1);
            return;
        }
        SetTarget(index);
    }

    public void Deselect() => SetTarget(-1);

    public void Next()
    {
        int next = FindSelectable(_targetIndex, +1);
        if (next >= 0) SetTarget(next);
    }

    public void Previous()
    {
        int prev = FindSelectable(_targetIndex, -1);
        if (prev >= 0) SetTarget(prev);
    }

    /// <summary>Busca la próxima misión seleccionable en la dirección dada (salta las no seleccionables). -1 si no hay.</summary>
    private int FindSelectable(int from, int dir)
    {
        int n = _missions.Count;
        if (n == 0) return -1;

        // Sin misión elegida: ir hacia adelante empieza en la primera, hacia atrás en la última
        if (from < 0) from = dir > 0 ? -1 : n;

        for (int step = 1; step <= n; step++)
        {
            int i = from + dir * step;
            if (wrapAround) i = ((i % n) + n) % n;
            else if (i < 0 || i >= n) return -1;

            if (_missions[i].Selectable) return i;
        }
        return -1;
    }

    public bool IsMissionLocked(int index) =>
        index >= 0 && index < _missions.Count && _missions[index].Locked;

    public bool IsMissionSelectable(int index) =>
        index >= 0 && index < _missions.Count && _missions[index].Selectable;

    /// <summary>Marca una misión como bloqueada o no (solo visual: candado y sin estrellas). No cambia si se puede seleccionar.</summary>
    public void SetMissionLocked(int index, bool locked)
    {
        if (index < 0 || index >= _missions.Count) return;
        var m = _missions[index];

        if (m.data == null) m.data = m.panel.gameObject.AddComponent<MissionPanelData>();
        m.data.isLocked = locked;
        ApplyLockVisual(m);
    }

    /// <summary>Permite o impide seleccionar una misión en pleno juego (por ejemplo según el progreso del jugador).</summary>
    public void SetMissionSelectable(int index, bool selectable)
    {
        if (index < 0 || index >= _missions.Count) return;
        var m = _missions[index];

        if (m.data == null) m.data = m.panel.gameObject.AddComponent<MissionPanelData>();
        m.data.isSelectable = selectable;
        ApplyLockVisual(m);

        if (!selectable)
        {
            if (_targetIndex == index) SetTarget(-1);                      // si estaba abierta, se guarda
            else if (index != _shownIndex) m.panel.gameObject.SetActive(false);
        }
        else if (!hideInactivePanels && index != _shownIndex)
        {
            m.panel.gameObject.SetActive(true);
        }
    }

    /// <summary>Actualiza lo visual del botón: panel del candado y estrellas.</summary>
    private void ApplyLockVisual(Mission m)
    {
        if (m.lockedMarker) m.lockedMarker.SetActive(m.Locked);
        if (m.difficultyPanel) m.difficultyPanel.gameObject.SetActive(m.ShowDifficulty);
    }

    // ───────────────────────── Confirmar ─────────────────────────
    private bool _confirmed;
    private ScreenFader _fader;

    // ───────────────────────── Texto de ayuda ─────────────────────────
    private Coroutine _helpRoutine;
    private bool _helpDismissed;

    private IEnumerator HelpRoutine()
    {
        yield return new WaitForSecondsRealtime(helpDelay);
        _helpRoutine = null;
        if (!_helpDismissed && helpText) helpText.gameObject.SetActive(true);
    }

    /// <summary>Oculta el texto de ayuda y evita que vuelva a aparecer en esta escena.</summary>
    private void DismissHelp()
    {
        _helpDismissed = true;
        if (_helpRoutine != null) { StopCoroutine(_helpRoutine); _helpRoutine = null; }
        if (helpText) helpText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Guarda la misión elegida (como número, en SelectedMission) y carga la escena de juego con fade.
    /// Se llama con Enter, con el botón "confirmButton" o desde cualquier otro script.
    /// </summary>
    public void ConfirmSelection()
    {
        if (_confirmed) return;

        if (_targetIndex < 0)
        {
            if (DebugsConsola)
                Debug.Log("[MissionSelectionManager] No hay ninguna misión elegida, no se confirma nada.", this);
            return;
        }

        // Solo se puede confirmar cuando la hoja está quieta y fija sobre la mesa:
        // no durante la entrada ni la salida (ni mientras otra hoja se está guardando).
        if (_process != null || _shownIndex != _targetIndex)
        {
            if (DebugsConsola)
                Debug.Log("[MissionSelectionManager] Confirmar ignorado: la hoja todavía está en transición.", this);
            return;
        }

        int chosen = _targetIndex;
        if (_fader == null || !_fader.LoadScene(gameplaySceneName)) return;

        SelectedMission.Index = chosen;
        _confirmed = true;

        // La ayuda desaparece solo con el Enter que realmente elige la misión (no con cualquier Enter)
        DismissHelp();

        if (DebugsConsola)
            Debug.Log($"[MissionSelectionManager] Misión CONFIRMADA: índice {chosen} ({_missions[chosen].displayName}).", this);
    }

    // ───────────────────────── Núcleo ─────────────────────────
    private void SetTarget(int index)
    {
        if (_confirmed) return; // ya se confirmó: no se puede cambiar mientras se hace el fade
        if (index == _targetIndex) return;
        _targetIndex = index;

        if (DebugsConsola)
        {
            if (index >= 0) Debug.Log($"[MissionSelectionManager] Misión seleccionada: índice {index} ({_missions[index].displayName}).", this);
            else Debug.Log("[MissionSelectionManager] Ninguna misión seleccionada.", this);
        }

        RefreshMenuHighlight();
        ScrollToSelected();
        OnMissionChanged?.Invoke(_targetIndex);

        // Una sola corrutina procesa todo: si el jugador toca rápido varias veces,
        // simplemente se actualiza el objetivo y se respeta la animación en curso.
        if (_process == null)
            _process = StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        while (_shownIndex != _targetIndex)
        {
            // 1) Guardar la hoja actual (si hay una afuera)
            if (_shownIndex >= 0)
            {
                int hiding = _shownIndex;
                yield return HideRoutine(_missions[hiding]);
                _shownIndex = -1;
                OnMissionHidden?.Invoke(hiding);
            }

            // 2) Sacar la nueva (si el objetivo sigue siendo válido)
            if (_targetIndex >= 0)
            {
                int showing = _targetIndex;
                yield return ShowRoutine(_missions[showing]);
                _shownIndex = showing;
                OnMissionShown?.Invoke(showing);
            }
        }
        _process = null;
    }

    /// <summary>Dirección hacia la cámara (para "levantar" la hoja acercándola a ella).</summary>
    private Vector3 LiftOffset()
    {
        Camera cam = viewCamera ? viewCamera : Camera.main;
        Vector3 towardCamera = cam ? -cam.transform.forward : Vector3.up;
        return towardCamera * liftDistance;
    }

    private Vector3 GetDisplayPosition()
    {
        if (displayAnchor != null) return displayAnchor.position;

        Transform r = folderRoot ? folderRoot : transform;
        return r.position + r.right * 0.5f;
    }

    /// <summary>Carpeta → arriba (hacia la cámara) → sobre la mesa → baja y se apoya.</summary>
    private IEnumerator ShowRoutine(Mission m)
    {
        if (m.panel == null) yield break;

        m.panel.gameObject.SetActive(true);
        SnapHome(m);

        Vector3 lift = LiftOffset();
        Vector3 homePos = m.HomeWorldPos;
        Vector3 deskPos = GetDisplayPosition();
        Vector3 deskScale = m.homeLocalScale * displayScaleMultiplier;

        // 1) Se levanta: se acerca a la cámara
        yield return MoveTo(m.panel, homePos + lift, m.homeLocalScale, liftDuration);

        // 2) Se desplaza (ya levantada) hasta la posición de la mesa y crece para leerse
        yield return MoveTo(m.panel, deskPos + lift, deskScale, travelDuration);

        // 3) Baja a la altura original y queda apoyada
        yield return MoveTo(m.panel, deskPos, deskScale, dropDuration);
    }

    /// <summary>Mesa → se levanta → vuelve sobre la carpeta → baja y se guarda.</summary>
    private IEnumerator HideRoutine(Mission m)
    {
        if (m.panel == null) yield break;

        Vector3 lift = LiftOffset();
        Vector3 currentPos = m.panel.position;
        Vector3 homePos = m.HomeWorldPos;

        // 1) Se levanta de la mesa
        yield return MoveTo(m.panel, currentPos + lift, m.panel.localScale, liftDuration);

        // 2) Vuelve sobre la carpeta y recupera su tamaño
        yield return MoveTo(m.panel, homePos + lift, m.homeLocalScale, travelDuration);

        // 3) Baja y se guarda
        yield return MoveTo(m.panel, homePos, m.homeLocalScale, dropDuration);

        SnapHome(m);
        if (hideInactivePanels || !m.Selectable) m.panel.gameObject.SetActive(false);
    }

    private void SnapHome(Mission m)
    {
        m.panel.position = m.HomeWorldPos;
        m.panel.localScale = m.homeLocalScale;
    }

    /// <summary>Mueve y escala el panel. NO modifica la rotación.</summary>
    private IEnumerator MoveTo(Transform t, Vector3 toPos, Vector3 toScale, float duration)
    {
        Vector3 fromPos = t.position;
        Vector3 fromScale = t.localScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float k = ease.Evaluate(Mathf.Clamp01(elapsed / duration));

            t.position = Vector3.LerpUnclamped(fromPos, toPos, k);
            t.localScale = Vector3.LerpUnclamped(fromScale, toScale, k);
            yield return null;
        }

        t.position = toPos;
        t.localScale = toScale;
    }
}
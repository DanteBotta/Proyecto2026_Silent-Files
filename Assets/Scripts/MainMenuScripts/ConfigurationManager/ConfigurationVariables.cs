using UnityEngine;

public class ConfigurationVariables : MonoBehaviour
{
    public static ConfigurationVariables Instance { get; private set; }

    public int Idioma = 0;
    public float MasterVolume = 1f;
    public float MusicVolume = 1f;
    public float EffectVolume = 1f;
    public int Resolucion = 2;
    public bool FullScreen = true;
    public bool SpeedrunMode = false;

    // Mismo orden que las opciones del dropdown
    private static readonly Vector2Int[] resoluciones =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160),

        new Vector2Int(1440, 900),
        new Vector2Int(1680, 1050),
        new Vector2Int(1920, 1200),
        new Vector2Int(2560, 1600),
    };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        AplicarPantalla(); // aplica los valores al abrir el juego
    }

    public void AplicarPantalla()
    {
        Vector2Int res = resoluciones[Mathf.Clamp(Resolucion, 0, resoluciones.Length - 1)];
        FullScreenMode modo = FullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        Screen.SetResolution(res.x, res.y, modo);

        Debug.Log("Pantalla aplicada: " + res.x + "x" + res.y + " | " + modo);
    }
}
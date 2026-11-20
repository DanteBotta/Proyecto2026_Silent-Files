using UnityEngine;

public class ConfigurationVariables : MonoBehaviour
{
    public static ConfigurationVariables Instance { get; private set; }

    // Valores por defecto (acá, no en Start, para que no se reseteen)
    public int Idioma = 0;            // 0 = Español ; 1 = Inglés
    public float MasterVolume = 1f;   // 0 a 1
    public float MusicVolume = 1f;
    public float EffectVolume = 1f;
    public int Resolucion = 3;        // 0=1280x720 ; 1=1366x768 ; 2=1600x900 ; 3=1920x1080
    public bool FullScreen = false;
    public bool SpeedrunMode = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // evita duplicados al volver a la escena de origen
            return;
        }

        Instance = this;
        transform.SetParent(null); // DontDestroyOnLoad solo funciona en objetos raíz
        DontDestroyOnLoad(gameObject);
    }
}
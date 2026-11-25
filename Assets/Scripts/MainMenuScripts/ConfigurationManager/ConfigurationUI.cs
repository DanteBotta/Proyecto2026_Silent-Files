using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfigurationUI : MonoBehaviour
{
    [Header("Toggles")]
    public Toggle modoSpeedrun;
    public Toggle pantallaCompleta;

    [Header("Dropdowns")]
    public TMP_Dropdown idiomaDropdown;
    public TMP_Dropdown resolucionDropdown;

    [Header("Audio")]
    public Slider Slider_MasterVolume;
    public TextMeshProUGUI Text_MasterVolume;
    public Slider Slider_Music;
    public TextMeshProUGUI Text_Music;
    public Slider Slider_EffectVolume;
    public TextMeshProUGUI Text_EffectVolume;

    ConfigurationVariables cfg;

    void Start()
    {
        cfg = ConfigurationVariables.Instance;

        // Cargo en la UI los valores guardados (no los reseteo)
        idiomaDropdown.value = cfg.Idioma;
        resolucionDropdown.value = cfg.Resolucion;
        Slider_MasterVolume.value = cfg.MasterVolume * 100;
        Slider_Music.value = cfg.MusicVolume * 100;
        Slider_EffectVolume.value = cfg.EffectVolume * 100;
        pantallaCompleta.isOn = cfg.FullScreen;
        modoSpeedrun.isOn = cfg.SpeedrunMode;
    }

    void Update()
    {
        // Detecto si cambió la resolución o la pantalla completa antes de pisar los valores
        bool cambioPantalla = cfg.Resolucion != resolucionDropdown.value
                           || cfg.FullScreen != pantallaCompleta.isOn;

        // Paso los valores de la UI al script persistente
        cfg.Idioma = idiomaDropdown.value;
        cfg.Resolucion = resolucionDropdown.value;
        cfg.MasterVolume = Slider_MasterVolume.value / 100;
        cfg.MusicVolume = Slider_Music.value / 100;
        cfg.EffectVolume = Slider_EffectVolume.value / 100;
        cfg.FullScreen = pantallaCompleta.isOn;
        cfg.SpeedrunMode = modoSpeedrun.isOn;

        if (cambioPantalla)
        {
            cfg.AplicarPantalla();
        }

        bool esp = LanguageManager.Instance.idiomaActual == 0;
        Text_MasterVolume.text = (esp ? "Volumen Maestro: " : "Master Volume: ") + ObtenerValor(Slider_MasterVolume.value);
        Text_Music.text = (esp ? "Música: " : "Music: ") + ObtenerValor(Slider_Music.value);
        Text_EffectVolume.text = (esp ? "Efectos: " : "Effects: ") + ObtenerValor(Slider_EffectVolume.value);
    }

    string ObtenerValor(float valor) => valor == 0 ? "OFF" : valor + "%";
}
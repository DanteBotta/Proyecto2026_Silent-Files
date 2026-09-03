using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfigurationVariables : MonoBehaviour
{
    //Dropdown de idioma
    [Header("Dropdown de idioma")]
    public TMP_Dropdown idiomaDropdown;

    [Space(5)]

    //Slider and TextMeshProUGUI for each volume control
    [Header("Slider y txt de Audio")]
    public Slider Slider_MasterVolume;
    public TextMeshProUGUI Text_MasterVolume;
    [Space(2)]
    public Slider Slider_Music;
    public TextMeshProUGUI Text_Music;
    [Space(2)]
    public Slider Slider_EffectVolume;
    public TextMeshProUGUI Text_EffectVolume;

    [Space(5)]

    // Dropdown de calidad
    [Header("Dropdown de calidad")]
    public TMP_Dropdown calidadDropdown;

    // Dropdown de calidad
    [Header("Dropdown de resolución")]
    public TMP_Dropdown resolucionDropdown;

    // Toggle de pantalla completa
    [Header("Dropdown de pantallaCompleta")]
    public Toggle pantallaCompleta;

    // Dropdown de saturación
    [Header("Dropdown de saturación")]
    public Slider Slider_Saturacion;



    int Idioma = 0;

    float MasterVolume = 0;
    float MusicVolume = 0;
    float EffectVolume = 0;

    int Calidad = 0;
    int Resolucion = 0;

    bool FullScreen = false;

    float Saturacion = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Valor inicial de idioma
        // 0 = Español ; 1 = Ingles
        idiomaDropdown.value = 0;

        //Valor inicial de slider de volumen
        Slider_MasterVolume.value = 67;
        Slider_Music.value = 100;
        Slider_EffectVolume.value = 100;

        //Valor inicial de calidad
        // 0 = BAJA ; 1 = MEDIA ; 2 = ALTA
        calidadDropdown.value = 1;

        //Valor inicial de calidad
        // 0 = 1280 x 720 ; 1 = 1366 x 768 ; 2 = 1600 x 900 ; 3 = 1920 x 1080
        resolucionDropdown.value = 3;

        Slider_Saturacion.value = 50;
    }

    // Update is called once per frame
    void Update()
    {
        Idioma = idiomaDropdown.value; // We assign the value of the dropdown to the variable 'Idioma'


        // Variables for the sounds
        Text_MasterVolume.text = "Volumen Maestro:" + ObtenerValor(Slider_MasterVolume.value);
        MasterVolume = Slider_MasterVolume.value / 100;

        Text_Music.text = "Musica:" + ObtenerValor(Slider_Music.value);
        MusicVolume = Slider_Music.value / 100;

        Text_EffectVolume.text = "Efectos:" + ObtenerValor(Slider_EffectVolume.value);
        EffectVolume = Slider_EffectVolume.value / 100;

        // Variables for the quality and resolution
        Calidad = calidadDropdown.value;
        Resolucion = resolucionDropdown.value;

        if (pantallaCompleta.isOn)
        {
            FullScreen = true;
        }
        else
        {
            FullScreen = false;

        }

        Saturacion = Slider_Saturacion.value / 100;
    }

    public string ObtenerValor(float valor)
    {
        if (valor == 0)
        {
            return "OFF";
        }

        return valor.ToString() + "%";    
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VolumeConfiguration : MonoBehaviour
{

    public Slider Slider_MasterVolume;
    public TextMeshProUGUI Text_MasterVolume;

    public Slider Slider_Music;
    public TextMeshProUGUI Text_Music;

    public Slider Slider_EffectVolume;
    public TextMeshProUGUI Text_EffectVolume;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Slider_MasterVolume.value = 100;
        Slider_Music.value = 100;
        Slider_EffectVolume.value = 100;
    }

    // Update is called once per frame
    void Update()
    {
        Text_MasterVolume.text = "Volumen Maestro:" + ObtenerValor(Slider_MasterVolume.value);
        Text_Music.text = "Musica:" + ObtenerValor(Slider_Music.value);
        Text_EffectVolume.text = "Efectos:" + ObtenerValor(Slider_EffectVolume.value    );
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

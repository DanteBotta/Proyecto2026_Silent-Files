using TMPro;
using UnityEngine;

public class LocalizationText : MonoBehaviour
{
    public string textoEspañol;
    public string textoIngles;

    private TextMeshProUGUI texto;

    private void Awake()
    {
        texto = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        // Al aparecer en una escena, usa el idioma actual
        if (LanguageManager.Instance != null)
        {
            ActualizarTexto(LanguageManager.Instance.idiomaActual);
        }
    }

    public void ActualizarTexto(int idioma)
    {
        if (idioma == 0)
        {
            texto.text = textoEspañol;
        }
        else if (idioma == 1)
        {
            texto.text = textoIngles;
        }
    }
}

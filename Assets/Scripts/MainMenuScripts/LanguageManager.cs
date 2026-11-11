using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance;

    // 0 = Español
    // 1 = Inglés
    public int idiomaActual = 0;

    public TMP_Dropdown dropdownIdioma;
    public TMP_Dropdown dropdownCalidad;

    private void Awake()
    {
        // Evita que haya más de un LanguageManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CambiarIdioma(int idioma)
    {
        idiomaActual = idioma;

        ActualizarTodosLosTextos();
        ActualizarDropdowns();
    }

    public void ActualizarTodosLosTextos()
    {
        LocalizationText[] textos = FindObjectsByType<LocalizationText>(
            FindObjectsInactive.Include
        );

        foreach (LocalizationText texto in textos)
        {
            texto.ActualizarTexto(idiomaActual);
        }
    }

    public void ActualizarDropdowns()
    {
        // Dropdown de idioma
        if (dropdownIdioma != null)
        {
            dropdownIdioma.options[0].text =
                idiomaActual == 0 ? "ESPAÑOL" : "SPANISH";

            dropdownIdioma.options[1].text =
                idiomaActual == 0 ? "INGLÉS" : "ENGLISH";

            dropdownIdioma.RefreshShownValue();
        }

        // Dropdown de calidad
        if (dropdownCalidad != null)
        {
            dropdownCalidad.options[0].text =
                idiomaActual == 0 ? "BAJO" : "LOW";

            dropdownCalidad.options[1].text =
                idiomaActual == 0 ? "MEDIO" : "MEDIUM";

            dropdownCalidad.options[2].text =
                idiomaActual == 0 ? "ALTO" : "HIGH";

            dropdownCalidad.RefreshShownValue();
        }
    }
}

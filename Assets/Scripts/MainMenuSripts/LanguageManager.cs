using UnityEngine;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance;

    // 0 = Español
    // 1 = Inglés
    public int idiomaActual = 0;

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

        Debug.Log("Idioma cambiado a: " + idiomaActual);

        ActualizarTodosLosTextos();
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
}

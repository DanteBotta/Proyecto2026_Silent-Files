
using System.Collections;
using UnityEngine;

public class IntroJuego : MonoBehaviour
{
    [Header("Configuracion")]
    public bool activarIntro = true;

    [Header("Elementos")]
    public CanvasGroup pantallaNegra;
    public CanvasGroup imagenIntro;

    [Header("Tiempos")]
    public float esperaInicial = 0.5f;
    public float duracionFadeAparicion = 1f;
    public float esperaImagenVisible = 2f;
    public float duracionFadeDesaparicion = 1f;
    public float esperaPantallaNegra = 0.5f;
    public float duracionFadeNegro = 1f;

    [Header("Estado del juego")]
    public bool juegoEmpezo = false;

    private void Start()
    {
        Time.timeScale = 1f; // Avanza el tiempo del juego.
        pantallaNegra.gameObject.SetActive(true);
        imagenIntro.gameObject.SetActive(true);

        pantallaNegra.alpha = 1f;
        imagenIntro.alpha = 0f;

        juegoEmpezo = false;

        if (!activarIntro)
        {
            pantallaNegra.alpha = 0f;
            imagenIntro.alpha = 0f;
            pantallaNegra.gameObject.SetActive(false);
            imagenIntro.gameObject.SetActive(false);
            juegoEmpezo = true;
            return;
        }

        StartCoroutine(ReproducirIntro());
    }           

    private IEnumerator ReproducirIntro()
    {
        yield return new WaitForSeconds(esperaInicial);

        // Aparece la imagen con fade.
        yield return Fade(imagenIntro, 0f, 1f,
            duracionFadeAparicion);

        // Mantener la imagen completamente visible.
        yield return new WaitForSeconds(esperaImagenVisible);

        // Desaparece la imagen.
        yield return Fade(imagenIntro, 1f, 0f,
            duracionFadeDesaparicion);

        // Esperar antes de quitar el fondo negro.
        yield return new WaitForSeconds(esperaPantallaNegra);

        yield return Fade(pantallaNegra, 1f, 0f,
            duracionFadeNegro);

        juegoEmpezo = true;

        pantallaNegra.gameObject.SetActive(false);
        imagenIntro.gameObject.SetActive(false);
    }

    private IEnumerator Fade(
        CanvasGroup grupo,
        float inicio,
        float fin,
        float duracion)
    {
        if (duracion <= 0f)
        {
            grupo.alpha = fin;
            yield break;
        }

        float tiempo = 0f;
        grupo.alpha = inicio;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;

            grupo.alpha = Mathf.Lerp(
                inicio,
                fin,
                tiempo / duracion
            );

            yield return null;
        }

        grupo.alpha = fin;
    }
}


using System.Collections;
using UnityEngine;

public class IntroJuego : MonoBehaviour
{
    [Header("Configuracion")]
    public bool activarIntro = true;

    [Header("Elementos")]
    public CanvasGroup pantallaNegra;
    public CanvasGroup imagenIntro;
    public CanvasGroup textoIntro;

    [Header("Tiempos")]
    public float esperaInicial = 0.5f;
    public float duracionFadeAparicion = 1f;
    public float esperaTexto = 0.5f;
    public float duracionFadeDesaparicion = 1f;
    public float esperaPantallaNegra = 0.5f;
    public float duracionFadeNegro = 1f;

    [Header("Estado del juego")]
    public bool juegoEmpezo = false;

    private void Start()
    {
        // Activar los objetos aunque esten ocultos en el Inspector.
        pantallaNegra.gameObject.SetActive(true);
        imagenIntro.gameObject.SetActive(true);
        textoIntro.gameObject.SetActive(true);

        if (!activarIntro)
        {
            pantallaNegra.alpha = 0f;
            imagenIntro.alpha = 0f;
            textoIntro.alpha = 0f;
            juegoEmpezo = true;
            return;
        }

        pantallaNegra.alpha = 1f;
        imagenIntro.alpha = 0f;
        textoIntro.alpha = 0f;
        juegoEmpezo = false;

        StartCoroutine(ReproducirIntro());
    }

    private IEnumerator ReproducirIntro()
    {
        yield return new WaitForSeconds(esperaInicial);

        // Aparece la imagen con fade.
        yield return Fade(imagenIntro, 0f, 1f,
            duracionFadeAparicion);

        // El texto comienza a aparecer 0.5 segundos despues.
        yield return new WaitForSeconds(esperaTexto);

        yield return Fade(textoIntro, 0f, 1f,
            duracionFadeAparicion);

        // Desaparecen juntos la imagen y el texto.
        Coroutine fadeImagen = StartCoroutine(
            Fade(imagenIntro, 1f, 0f,
                duracionFadeDesaparicion));

        Coroutine fadeTexto = StartCoroutine(
            Fade(textoIntro, 1f, 0f,
                duracionFadeDesaparicion));

        yield return fadeImagen;
        yield return fadeTexto;

        // Esperar antes de quitar el fondo negro.
        yield return new WaitForSeconds(esperaPantallaNegra);

        yield return Fade(pantallaNegra, 1f, 0f,
            duracionFadeNegro);

        juegoEmpezo = true;
        pantallaNegra.gameObject.SetActive(false);
        imagenIntro.gameObject.SetActive(false);
        textoIntro.gameObject.SetActive(false);
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

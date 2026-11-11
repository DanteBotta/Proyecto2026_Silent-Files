using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class AnimationImages : MonoBehaviour
{
    public Image imagen;
    public float FPS = 24f;

    private Coroutine animacion;

    public void MostrarImagen(string ruta)
    {
        Detener();

        Sprite sprite = Resources.Load<Sprite>(ruta);

        if (sprite == null)
        {
            Debug.LogError("NO SE ENCONTRO LA IMAGEN");
            return;
        }

        imagen.enabled = true;
        imagen.sprite = sprite;
    }

    public Coroutine Reproducir(string carpeta, bool repetir)
    {
        Detener();

        Sprite[] frames = Resources.LoadAll<Sprite>(carpeta);

        Array.Sort(frames, (a, b) =>
            string.Compare(a.name, b.name, StringComparison.Ordinal));

        if (frames.Length == 0)
        {
            Debug.LogError("No hay imágenes en: " + carpeta);
            return null;
        }

        imagen.enabled = true;

        animacion = StartCoroutine(Animar(frames, repetir));

        return animacion;
    }

    IEnumerator Animar(Sprite[] frames, bool repetir)
    {
        int frame = 0;

        while (true)
        {
            imagen.sprite = frames[frame];

            yield return new WaitForSecondsRealtime(1f / FPS);

            frame++;

            if (frame >= frames.Length)
            {
                if (!repetir)
                {
                    break;
                }

                frame = 0;
            }
        }

        animacion = null;
    }

    public void Detener()
    {
        if (animacion != null)
        {
            StopCoroutine(animacion);
            animacion = null;
        }
    }
}
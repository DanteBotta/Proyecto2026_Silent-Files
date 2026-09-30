using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using System;
using System.Collections;

public class WindowMinigameManager : MonoBehaviour
{
    [Header("Referenicas a scripts (se asignan automaticamente")]
    public ShowCanvasNearPlayer ShowCanvasNearPlayer; // Para saber si el jugador esta cerca para que el canvas funcione
    public AnimationImages AnimationImages; // Script para reproducir las imagenes

    [Header("Referencias a UIs de la ventana")]
    public TextMeshProUGUI TextoEstado; // Texto del estado de ventana
    public Image SpriteImage; // Imagen de la ventana
    public Image SpriteBoton; // Imagen del boton
    public Image ImageF;

    [Header("Cámara que muestra la layer UIWorld")]
    public Camera UICamera; // Cámara que muestra los UI en world space

    // La referencia al post proscessing
    public Volume volume; 
    private DepthOfField depthOfField;

    [Header("Configuración del Minijuego")]
    public float TiempoMinimoEspera = 1f;
    public float TiempoMaximoEspera = 3f;
    public float MargenReaccion = 0.5f;
    public float TiempoMostrarResultado = 1.5f;


    bool JuegoIniciado = false;
    bool AnimacionTerminada = false;

    int EvitarRepeticion = 0;

    float TiempoEspera;
    float TiempoInicio;
    float TiempoTranscurrido;
    bool DebeSoltar = false;
    bool MostrandoResultado = false;
    float TiempoInicioResultado;
    bool VentanaAbierta = false;
    bool ResultadoExitoso = false;

    void Awake()
    {
        ShowCanvasNearPlayer = GetComponent<ShowCanvasNearPlayer>();
        AnimationImages = GetComponent<AnimationImages>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        volume.profile.TryGet(out depthOfField); // Consigue el efecto de depthOfField del volume
        TextoEstado.text = "Ventana (cerrado)";

        SpriteBoton.enabled = false;
        SpriteImage.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (ShowCanvasNearPlayer.FuncionCanvas && Input.GetKeyDown(KeyCode.F) && !JuegoIniciado && !VentanaAbierta)
        {
            StartCoroutine(ActivarMinijuego());
        }

        if (JuegoIniciado && AnimacionTerminada)
        {
            // Si estamos mostrando un resultado, esperamos antes de continuar
            if (MostrandoResultado)
            {
                if (Time.time - TiempoInicioResultado >= TiempoMostrarResultado)
                {
                    Debug.Log("Termino el resultado");

                    MostrandoResultado = false;
                    DebeSoltar = false;
                    EvitarRepeticion = 0;

                    if (ResultadoExitoso)
                    {
                        DesactivarMinijuego();
                    }
                }

                return;
            }

            if (Input.GetKey(KeyCode.F))
            {
                if (EvitarRepeticion == 0)
                {
                    AnimationImages.Reproducir("MinijuegoVentana/InteraccionVentana/StruglingWindow", true);
                    MostrarBoton("MinijuegoVentana/Botones/Apretado");
                    EvitarRepeticion++;

                    TiempoEspera = UnityEngine.Random.Range(TiempoMinimoEspera, TiempoMaximoEspera);
                    TiempoInicio = Time.time;
                }

                TiempoTranscurrido = Time.time - TiempoInicio;

                if (TiempoTranscurrido >= TiempoEspera)
                {
                    MostrarBoton("MinijuegoVentana/Botones/Soltar");
                    DebeSoltar = true;
                }

                // Se pasó el margen y todavía mantiene F
                if (DebeSoltar && (TiempoTranscurrido - TiempoEspera) > MargenReaccion)
                {
                    MostrarBoton("MinijuegoVentana/Botones/Fracaso");
                    Debug.Log("No soltaste a tiempo");
                    AnimationImages.Detener();

                    MostrandoResultado = true;
                    TiempoInicioResultado = Time.time;
                    ResultadoExitoso = false;

                    EvitarRepeticion = 0;
                }
            }

            if (Input.GetKeyUp(KeyCode.F))
            {
                if (!DebeSoltar)
                {
                    // SOLTÓ ANTES
                    MostrarBoton("MinijuegoVentana/Botones/Gris");
                    Debug.Log("Soltaste antes de tiempo");
                    AnimationImages.Detener();

                    MostrandoResultado = true;
                    TiempoInicioResultado = Time.time;
                    ResultadoExitoso = false;

                    EvitarRepeticion = 0;
                }
                else if ((TiempoTranscurrido - TiempoEspera) <= MargenReaccion)
                {
                    // ÉXITO
                    MostrarBoton("MinijuegoVentana/Botones/Exito");
                    Debug.Log("Soltaste perfecto");

                    AnimationImages.Detener();

                    VentanaAbierta = true;
                    MostrandoResultado = true;
                    TiempoInicioResultado = Time.time;
                    ResultadoExitoso = true;

                    AnimationImages.Reproducir("MinijuegoVentana/InteraccionVentana/OpeningWindow", false );

                    TextoEstado.text = "Ventana (abierta)";
                }
            }
        }
    }

    IEnumerator ActivarMinijuego()
    {
        SpriteImage.enabled = true;
        SpriteBoton.enabled = true;
        UICamera.enabled = false;
        ConfigurarDesenfoque(true);
        MostrarBoton("MinijuegoVentana/Botones/Gris");

        JuegoIniciado = true;
        AnimacionTerminada = false;

        yield return AnimationImages.Reproducir("MinijuegoVentana/InteraccionVentana/StartInteraction", false);
        AnimacionTerminada = true;
    }

    void DesactivarMinijuego()
    {
        SpriteImage.enabled = false;
        SpriteBoton.enabled = false;
        UICamera.enabled = true;
        ConfigurarDesenfoque(false);

        JuegoIniciado = false;

        if (VentanaAbierta)
        {
            Destroy(ImageF.gameObject);
        }
    }

    public void MostrarBoton(string ruta) // Con una ruta específica muestra una imagen específica del boton
    {
        Sprite sprite = Resources.Load<Sprite>(ruta); // Carga la imagen

        if (sprite == null) // Si no la encontró
        {
            Debug.LogError("NO SE ENCONTRO LA IMAGEN");
            return;
        }
        SpriteBoton.sprite = sprite; // La establece al boton
    }
    void ConfigurarDesenfoque(bool estado) // Recibe true o false y activa o desactiva el post proscessing
    {
        depthOfField.active = estado;
    }
}

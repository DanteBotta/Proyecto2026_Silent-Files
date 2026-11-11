using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using System;
using System.Collections;
using GamePlay;

public class WindowMinigameManager : MonoBehaviour
{
    [Header("Referenicas a scripts (se asignan automaticamente")]
    public ShowCanvasNearPlayer ShowCanvasNearPlayer; // Para saber si el jugador esta cerca para que el canvas funcione
    public AnimationImages AnimationImages; // Script para reproducir las imagenes

    [Header("Referencias a UIs de la ventana")]
    public TextMeshProUGUI TextoEstado; // Texto del estado de ventana
    public Image SpriteImage; // Imagen de la ventana
    public Image SpriteBoton; // Imagen del boton
    public Image ImageF; // Imagen de F del canvas de world space (ahora se mantiene para cruzar la ventana)
    public Canvas CanvasInventario;

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

    [Header("Animator de la ventana")]
    public Animator AnimatorVentana;

    [Header("Ruido")]
    [SerializeField, Min(0f)] private float radioRuidoFallo = 10f;
    [Tooltip("Punto de interacción cerca del suelo. Sin asignar, usa el sitio donde el jugador comenzó la interacción.")]
    [SerializeField] private Transform origenRuido;

    [Header("Cruzar la ventana")]
    [Tooltip("Imagen negra a pantalla completa (en un Canvas Screen Space - Overlay) con un CanvasGroup.")]
    [SerializeField] private CanvasGroup pantallaNegra;
    [Tooltip("Primer punto de teletransporte (un lado de la ventana).")]
    [SerializeField] private Transform puntoTeletransporteA;
    [Tooltip("Segundo punto de teletransporte (el otro lado de la ventana).")]
    [SerializeField] private Transform puntoTeletransporteB;
    [SerializeField, Min(0.01f)] private float duracionFadeEntrada = 0.6f;
    [SerializeField, Min(0.01f)] private float duracionFadeSalida = 0.6f;
    [SerializeField, Min(0f)] private float tiempoPantallaNegra = 0.3f;
    [Tooltip("Copiar también la rotación del punto de destino al jugador.")]
    [SerializeField] private bool copiarRotacionDestino = true;
    [Tooltip("Scripts del jugador (movimiento, cámara, etc.) que se desactivan mientras cruza.")]
    [SerializeField] private Behaviour[] scriptsJugadorADesactivar;

    [Header("Distancias tras completar el minijuego")]
    [SerializeField] private float distanciaTrasCompletar = 4f;

    // Frame en el que se cerró un minijuego con ESC (para que el menú de pausa lo ignore)
    public static int FrameCierrePorEsc = -1;

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
    bool Cruzando = false;
    bool DistanciasReducidas = false;
    Vector3 posicionInteraccion;

    bool PrimeraVezF = false;
    bool FalloRojo = false;

    public bool MinijuegoActivo => JuegoIniciado;
    // Se puede salir con ESC solo antes de empezar a apretar F, o tras un fallo
    bool PuedeSalirConEsc =>
        JuegoIniciado &&
        !(MostrandoResultado && ResultadoExitoso) &&   // en éxito no se puede salir
        (MostrandoResultado || EvitarRepeticion == 0); // fallo, o todavía sin apretar F

    void Awake()
    {
        ShowCanvasNearPlayer = GetComponent<ShowCanvasNearPlayer>();
        AnimationImages = GetComponent<AnimationImages>();

        // Obtener el Animator del padre
        AnimatorVentana = GetComponentInParent<Animator>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        volume.profile.TryGet(out depthOfField); // Consigue el efecto de depthOfField del volume
        TextoEstado.text = "Ventana (cerrado)";

        SpriteBoton.enabled = false;
        SpriteImage.enabled = false;

        if (pantallaNegra != null)
        {
            pantallaNegra.alpha = 0f;
            pantallaNegra.blocksRaycasts = false;
            pantallaNegra.interactable = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // ESC para salir del minijuego (solo si está activo y en un estado permitido)
        if (Input.GetKeyDown(KeyCode.Escape) && PuedeSalirConEsc)
        {
            FrameCierrePorEsc = Time.frameCount;
            AnimationImages.Detener();
            DesactivarMinijuego();
            return;
        }

        if (ShowCanvasNearPlayer.FuncionCanvas && Input.GetKeyDown(KeyCode.F) && !JuegoIniciado)
        {
            if (!VentanaAbierta)
            {
                StartCoroutine(ActivarMinijuego());
            }
            else if (!Cruzando && puntoTeletransporteA != null && puntoTeletransporteB != null)
            {
                // Ventana ya abierta: F para pasar al otro lado
                StartCoroutine(CruzarVentana());
            }
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
                    PrimeraVezF = true;
                    FalloRojo = false;

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
                    RegistrarFallo("MinijuegoVentana/Botones/Fracaso", "No soltaste a tiempo");
                    return;
                }
            }

            if (Input.GetKeyUp(KeyCode.F) && EvitarRepeticion > 0)
            {
                // Al soltar, GetKey ya es falso: calcular el tiempo de este frame.
                TiempoTranscurrido = Time.time - TiempoInicio;

                if (TiempoTranscurrido < TiempoEspera)
                {
                    // SOLTÓ ANTES
                    RegistrarFallo("MinijuegoVentana/Botones/Gris", "Soltaste antes de tiempo");
                }
                else if ((TiempoTranscurrido - TiempoEspera) > MargenReaccion)
                {
                    RegistrarFallo("MinijuegoVentana/Botones/Fracaso", "No soltaste a tiempo");
                }
                else
                {
                    // ÉXITO
                    MostrarBoton("MinijuegoVentana/Botones/Exito");
                    Debug.Log("Soltaste perfecto");

                    AnimationImages.Detener();

                    VentanaAbierta = true;
                    MostrandoResultado = true;
                    TiempoInicioResultado = Time.time;
                    ResultadoExitoso = true;
                    AnimatorVentana.SetBool("Abrir", true);

                    AnimationImages.Reproducir("MinijuegoVentana/InteraccionVentana/OpeningWindow", false);

                    TextoEstado.text = "Ventana (abierta)";
                }
            }
        }
    }

    void RegistrarFallo(string boton, string mensaje)
    {
        if (MostrandoResultado)
            return;

        FalloRojo = boton.Contains("Fracaso");

        MostrarBoton(boton);
        Debug.Log(mensaje);
        AnimationImages.Detener();

        MostrandoResultado = true;
        TiempoInicioResultado = Time.time;
        ResultadoExitoso = false;
        DebeSoltar = false;
        EvitarRepeticion = 0;

        Vector3 posicionRuido = origenRuido != null ? origenRuido.position : posicionInteraccion;
        Sounds.MakeSound(new Sound(posicionRuido, radioRuidoFallo, Sound.SoundType.Interesting));
    }

    IEnumerator ActivarMinijuego()
    {
        // El canvas está elevado: conservar el punto desde el que se inició la interacción.
        posicionInteraccion = ShowCanvasNearPlayer != null && ShowCanvasNearPlayer.player != null
            ? ShowCanvasNearPlayer.player.position
            : transform.position;

        SpriteImage.enabled = true;
        SpriteBoton.enabled = true;
        UICamera.enabled = false;
        CanvasInventario.enabled = false;
        ConfigurarDesenfoque(true);
        MostrarBoton("MinijuegoVentana/Botones/Gris");

        JuegoIniciado = true;
        AnimacionTerminada = false;

        yield return AnimationImages.Reproducir("MinijuegoVentana/InteraccionVentana/StartInteraction", false);
        AnimacionTerminada = true;
    }

    void DesactivarMinijuego()
    {
        StopAllCoroutines(); // por si estaba en la animación inicial

        SpriteImage.enabled = false;
        SpriteBoton.enabled = false;
        UICamera.enabled = true;
        CanvasInventario.enabled = true;
        ConfigurarDesenfoque(false);

        JuegoIniciado = false;
        AnimacionTerminada = false;
        MostrandoResultado = false;
        DebeSoltar = false;
        EvitarRepeticion = 0;

        // Minijuego completado: la F se mantiene (ahora sirve para cruzar) y se reducen las distancias
        if (VentanaAbierta)
        {
            ReducirDistancias();
        }
    }

    void ReducirDistancias()
    {
        if (DistanciasReducidas || ShowCanvasNearPlayer == null)
            return;

        DistanciasReducidas = true;
        ShowCanvasNearPlayer.distanciaActivacion = distanciaTrasCompletar;
        ShowCanvasNearPlayer.distanciaFuncion = distanciaTrasCompletar;
    }

    // Fade a negro -> teletransporte al otro lado -> fade desde negro
    IEnumerator CruzarVentana()
    {
        Cruzando = true;

        Transform jugador = ShowCanvasNearPlayer != null ? ShowCanvasNearPlayer.player : null;

        // Bloquear control del jugador mientras dura la transición
        if (scriptsJugadorADesactivar != null)
        {
            foreach (Behaviour b in scriptsJugadorADesactivar)
                if (b != null) b.enabled = false;
        }

        // Fade de entrada (pantalla pasa a negro)
        yield return Fade(0f, 1f, duracionFadeEntrada);

        // Mover al jugador al punto más lejano (el opuesto al más cercano)
        if (jugador != null && puntoTeletransporteA != null && puntoTeletransporteB != null)
        {
            float distA = (jugador.position - puntoTeletransporteA.position).sqrMagnitude;
            float distB = (jugador.position - puntoTeletransporteB.position).sqrMagnitude;
            Transform destino = distA <= distB ? puntoTeletransporteB : puntoTeletransporteA;

            CharacterController cc = jugador.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false; // si no, el CharacterController pisa la posición

            jugador.position = destino.position;
            if (copiarRotacionDestino)
                jugador.rotation = destino.rotation;

            if (cc != null) cc.enabled = true;
        }

        if (tiempoPantallaNegra > 0f)
            yield return new WaitForSecondsRealtime(tiempoPantallaNegra);

        // Fade de salida (pantalla vuelve a verse)
        yield return Fade(1f, 0f, duracionFadeSalida);

        if (scriptsJugadorADesactivar != null)
        {
            foreach (Behaviour b in scriptsJugadorADesactivar)
                if (b != null) b.enabled = true;
        }

        Cruzando = false;
    }

    IEnumerator Fade(float desde, float hasta, float duracion)
    {
        if (pantallaNegra == null)
            yield break;

        pantallaNegra.blocksRaycasts = true;
        float t = 0f;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            pantallaNegra.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }

        pantallaNegra.alpha = hasta;
        pantallaNegra.blocksRaycasts = hasta > 0f;
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
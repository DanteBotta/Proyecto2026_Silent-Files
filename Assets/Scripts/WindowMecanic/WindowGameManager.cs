using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WindowGameManager : MonoBehaviour
{
    public ShowCanvasNearPlayer CanvasWindow; // Referencia al script que controla la activación del canvas según la distancia del jugador

    public TextMeshProUGUI WindowStateText; // Referencia al texto que muestra el estado de la ventana (abierta, cerrada, bloqueada)
    public TextMeshProUGUI WindowTitleText; // Referencia al texto que muestra el título de la ventana (instrucciones para el jugador)

    public Image Image; // Referencia a la imagen que cambia de color según el estado del minijuego


    // Estado de la ventana: abierta, cerrada o bloqueada
    public enum EstadoVentana
    {
        Bloqueada,
        Cerrada,
        Abierta
    }

    // Decidir como esta la ventana desde inspector
    public EstadoVentana estado = EstadoVentana.Cerrada;


    private Coroutine coroutineSenal; // Guarda una referencia a la Coroutine que está esperando para mostrar la señal

    private float tiempoSenal; // Guarda el momento exacto en el que apareció la señal

    private bool minijuegoActivo = false; // Indica si el minijuego está activo (el jugador está manteniendo presionada la tecla E)

    private bool senalMostrada = false; // Indica si la señal ya fue mostrada al jugador


    void Start()
    {
        Image.gameObject.SetActive(false); // Desactivar la imagen al inicio

        // Se actualiza la UI dependiendo de el estado inicial de la ventana
        ActualizarUI();
    }


    void Update()
    {
        // Si el jugador no está cerca
        if (!CanvasWindow.FuncionCanvas)
        {
            Image.gameObject.SetActive(false);
            return;
        }

        // Mostrar imagen
        Image.gameObject.SetActive(true);


        // =========================
        // VENTANA CERRADA
        // =========================

        if (estado == EstadoVentana.Cerrada)
        {
            // Empezar a mantener E
            if (Input.GetKeyDown(KeyCode.F) && !minijuegoActivo)
            {
                IniciarMinijuego();
            }


            // Soltar E
            if (Input.GetKeyUp(KeyCode.F) && minijuegoActivo)
            {
                SoltarTecla();
            }
        }


        // =========================
        // VENTANA ABIERTA
        // =========================

        else if (estado == EstadoVentana.Abierta)
        {
            WindowStateText.text = "Ventana (abierta)";
            WindowTitleText.text = "Presiona [F] para entrar";

            // Acá después ponemos la animación de entrar
        }


        // =========================
        // VENTANA BLOQUEADA
        // =========================

        else if (estado == EstadoVentana.Bloqueada)
        {
            WindowStateText.text = "Ventana (bloqueada)";
            WindowTitleText.text = "No puedes abrirla";
        }
    }


    // =========================
    // INICIAR MINIJUEGO
    // =========================

    void IniciarMinijuego()
    {
        minijuegoActivo = true;
        senalMostrada = false;

        WindowStateText.text = "Abriendo...";
        WindowTitleText.text = "ESPERA...";

        Image.color = Color.gray;

        coroutineSenal = StartCoroutine(EsperarSenal());
    }


    // =========================
    // ESPERAR SEÑAL
    // =========================

    IEnumerator EsperarSenal()
    {
        // Tiempo aleatorio entre 1 y 3 segundos
        float tiempoEspera = Random.Range(1f, 3f);

        yield return new WaitForSeconds(tiempoEspera);


        // Si el jugador ya soltó E, no hacemos nada
        if (!minijuegoActivo)
            yield break;


        // Guardamos el momento EXACTO de la señal
        tiempoSenal = Time.time;

        senalMostrada = true;


        // Cambiar UI
        WindowStateText.text = "¡SUELTA!";
        WindowTitleText.text = "¡AHORA!";

        Image.color = Color.green;


        // =========================
        // ESPERAR MÁXIMO 0.5 SEGUNDOS
        // =========================

        yield return new WaitForSeconds(0.5f);


        // Si todavía está activo significa
        // que el jugador NO soltó E a tiempo
        if (minijuegoActivo)
        {
            FalloPorTardanza();
        }
    }


    // =========================
    // SOLTAR E
    // =========================

    void SoltarTecla()
    {
        // Si soltó antes de la señal
        if (!senalMostrada)
        {
            StopCoroutine(coroutineSenal);

            minijuegoActivo = false;

            WindowStateText.text = "Ventana (cerrada)";
            WindowTitleText.text = "La soltaste demasiado pronto";

            Image.color = Color.white;

            Debug.Log("Fallo: soltaste antes de la señal.");

            // Ruido bajo
            HacerRuido("Bajo");

            return;
        }


        // Tiempo exacto en el que soltó
        float tiempoSoltar = Time.time;


        // Tiempo de reacción
        float reaccion = tiempoSoltar - tiempoSenal;


        Debug.Log("Tiempo de reacción: " + reaccion.ToString("F3") + " segundos");


        // Ya terminó el intento
        minijuegoActivo = false;


        // Detener coroutine
        if (coroutineSenal != null)
        {
            StopCoroutine(coroutineSenal);
            coroutineSenal = null;
        }


        // =========================
        // PERFECTO
        // =========================

        if (reaccion >= 0f && reaccion <= 0.2f)
        {
            estado = EstadoVentana.Abierta;

            WindowStateText.text = "Ventana (abierta)";
            WindowTitleText.text = "Presiona [F] para entrar";

            Image.color = Color.green;

            HacerRuido("Ninguno");

            Debug.Log("¡PERFECTO!");
        }


        // =========================
        // TARDE
        // =========================

        else if (reaccion > 0.2f && reaccion <= 0.5f)
        {
            estado = EstadoVentana.Abierta;

            WindowStateText.text = "Ventana (abierta)";
            WindowTitleText.text = "Presiona [F] para entrar";

            Image.color = Color.yellow;

            HacerRuido("Medio");

            Debug.Log("Abierta, pero hiciste ruido.");
        }


        // =========================
        // DEMASIADO TARDE
        // =========================

        else
        {
            FalloPorTardanza();
        }
    }


    // =========================
    // FALLO POR TARDANZA
    // =========================

    void FalloPorTardanza()
    {
        minijuegoActivo = false;

        estado = EstadoVentana.Cerrada;

        WindowStateText.text = "Ventana (cerrada)";
        WindowTitleText.text = "¡Demasiado tarde!";

        Image.color = Color.red;

        HacerRuido("Maximo");

        Debug.Log("Fallo: demasiado tarde.");
    }

    // Función para actualizar la UI según el estado de la ventana
    void ActualizarUI()
    {
        if (estado == EstadoVentana.Bloqueada) // Si la ventana está bloqueada, mostrar el texto correspondiente
        {
            WindowStateText.text = "Ventana (bloqueada)";
            WindowTitleText.text = "No puedes abrirla";
        }

        else if (estado == EstadoVentana.Cerrada) // Si la ventana está cerrada, mostrar el texto correspondiente
        {
            WindowStateText.text = "Ventana (cerrada)";
            WindowTitleText.text = "Mantén [F]";
        }

        else if (estado == EstadoVentana.Abierta) // Si la ventana está abierta, mostrar el texto correspondiente
        {
            WindowStateText.text = "Ventana (abierta)";
            WindowTitleText.text = "Presiona [F] para entrar";
        }
    }


    // Función para simular el ruido que hace el jugador al abrir la ventana, dependiendo de su tiempo de reacción
    void HacerRuido(string nivel)
    {
        Debug.Log("Ruido: " + nivel);
    }
}
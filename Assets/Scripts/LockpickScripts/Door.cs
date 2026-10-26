using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Door : MonoBehaviour
{
    public InventoryManager InventoryManager;
    public LockpickMinigameController LockpickMinigameController;
    public ShowCanvasNearPlayer ShowCanvasNearPlayer;

    public enum EstadoPuerta
    {
        Abierta,
        Cerrada,
        Trabada,
        Bloqueada
    }
    public EstadoPuerta estadoActual;

    bool PuertaAbierta = false;

    void Start()
    {
        InventoryManager = FindAnyObjectByType<InventoryManager>();
        LockpickMinigameController = FindAnyObjectByType<LockpickMinigameController>();
    }

    void Update()
    {
        if (ShowCanvasNearPlayer.FuncionCanvas && Input.GetKeyDown(KeyCode.F) && estadoActual != EstadoPuerta.Bloqueada)
        {
            InventoryManager.Herramienta herramientaActual = InventoryManager.ObjetoEnMano;

            if ((herramientaActual == InventoryManager.Herramienta.Mano)) // Se interactua con la mano
            {
                if (estadoActual == EstadoPuerta.Cerrada) // Puerta Cerrada
                {
                    Debug.Log("Abriste puerta con la mano");
                    estadoActual = EstadoPuerta.Abierta;
                    PuertaAbierta = true;
                }
                else if (estadoActual == EstadoPuerta.Abierta) // Puerta Abierta
                {
                    Debug.Log("Cerraste puerta con la mano");
                    estadoActual = EstadoPuerta.Cerrada;
                    PuertaAbierta = false;
                }
                else if (estadoActual == EstadoPuerta.Trabada) // Puerta Trabada
                {
                    Debug.Log("La puerta esta bloqueada, no se puede abrir con la mano");
                }
            }
            

            else if (herramientaActual == InventoryManager.Herramienta.Ganzua) // Se interactua con la ganzua
            {
                if (estadoActual == EstadoPuerta.Cerrada) // Puerta Cerrada
                {
                    Debug.Log("La puerta esta cerrada, no es necesario usar ganzua");
                }
                else if (estadoActual == EstadoPuerta.Abierta) // Puerta Abierta
                {
                    Debug.Log("Puerta Abierta");
                }
                else if (estadoActual == EstadoPuerta.Trabada) // Puerta Trabada, se puede intentar abrir con la ganzúa
                {
                    if (!LockpickMinigameController.MinigameRunning)
                    {
                        LockpickMinigameController.StartLockpickMinigame();
                        StartCoroutine(EsperarResultadoLockpickMinigame());
                    }
                }
                
            }
            else if (herramientaActual == InventoryManager.Herramienta.Palanca)
            {
                if (estadoActual == EstadoPuerta.Cerrada) // Puerta Cerrada
                {
                    Debug.Log("La puerta esta cerrada, no es necesario usar palanca");
                }
                else if (estadoActual == EstadoPuerta.Abierta) // Puerta Abierta
                {
                    Debug.Log("Puerta Abierta");
                }
                else if (estadoActual == EstadoPuerta.Trabada) // Puerta Trabada, se puede abrir con la palanca
                {
                    estadoActual = EstadoPuerta.Cerrada;
                    PuertaAbierta = true;
                }
            }
        }
        else
        {
            // Lo que pasa si la puerta esta bloqueada (no se puede abrir)
            Debug.Log("La puerta no se puede abrir, esta bloqueada");
        }
    }

    IEnumerator EsperarResultadoLockpickMinigame()
    {
        // Espera hasta que el minijuego termine
        yield return new WaitUntil(() => LockpickMinigameController.MinigameFinished);

        if (LockpickMinigameController.MinigameSuccess)
        {
            // Salio bien
            estadoActual = EstadoPuerta.Cerrada; // desbloqueada, o Abierta si queres abrirla directo
            PuertaAbierta = true;
            Debug.Log("Puerta desbloqueada con la ganzua");
        }
        else
        {
            // Salio mal
            estadoActual = EstadoPuerta.Trabada;
            PuertaAbierta = false;
            Debug.Log("No pudiste abrir la puerta con la ganzua");
        }
    }
}
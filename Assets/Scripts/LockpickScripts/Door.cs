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

            if (estadoActual == EstadoPuerta.Abierta)
            {
                // La puerta ya esta abierta
            }
            else if (herramientaActual == InventoryManager.Herramienta.Mano && estadoActual == EstadoPuerta.Cerrada)
            {
                // Lo que pasa si está equipada la mano
            }
            else if (herramientaActual == InventoryManager.Herramienta.Ganzua && estadoActual == EstadoPuerta.Trabada)
            {
                // Lo que pasa si está equipada la ganzúa
                if (!LockpickMinigameController.MinigameRunning)
                {
                    LockpickMinigameController.StartLockpickMinigame();
                    StartCoroutine(EsperarResultadoLockpickMinigame());
                }
            }
            else if (herramientaActual == InventoryManager.Herramienta.Palanca && estadoActual == EstadoPuerta.Trabada)
            {
                // Lo que pasa si está equipada la palanca
            }
        }
        else
        {
            // Lo que pasa si la puerta esta bloqueada (no se puede abrir)
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
            Debug.Log("Puerta desbloqueada");
        }
        else
        {
            // Salio mal
            estadoActual = EstadoPuerta.Trabada;
            Debug.Log("No pudiste abrir la puerta");
        }
    }
}
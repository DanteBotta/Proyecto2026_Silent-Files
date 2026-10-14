using System;
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
        if (ShowCanvasNearPlayer.FuncionCanvas && Input.GetKeyDown(KeyCode.F))
        {
            InventoryManager.Herramienta herramientaActual = InventoryManager.ObjetoEnMano;

            if (herramientaActual == InventoryManager.Herramienta.Mano)
            {
                // Lo que pasa si está equipada la mano
            }
            else if (herramientaActual == InventoryManager.Herramienta.Ganzua)
            {
                // Lo que pasa si está equipada la ganzúa
                LockpickMinigameController.StartLockpickMinigame();
            }
            else if (herramientaActual == InventoryManager.Herramienta.Palanca)
            {
                // Lo que pasa si está equipada la palanca
            }
        }
    }
}
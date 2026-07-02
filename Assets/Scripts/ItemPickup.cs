using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemPickup : MonoBehaviour
{
    public ItemData data;

    [Header("UI")]
    public GameObject interactUI;
    public Image      progressRing;
    public TMP_Text       itemNameLabel; // NUEVO

    private bool  playerNearby   = false;
    private float pickupProgress = 0f;

    void Start()
    {
        if (interactUI != null)
            interactUI.SetActive(false);

        // Pone el nombre al iniciar, así no hay que cambiarlo a mano
        if (itemNameLabel != null)
            itemNameLabel.text = data.itemName; // NUEVO
    }

    void Update()
    {
        // El UI mira siempre a la camara
        if (interactUI != null && interactUI.activeSelf)
            interactUI.transform.LookAt(Camera.main.transform);

        if (!playerNearby) return;

        if (Input.GetKey(KeyCode.E))
        {
            pickupProgress += Time.deltaTime;

            // Actualiza el aro: valor entre 0 y 1
            if (progressRing != null)
                progressRing.fillAmount = pickupProgress / data.pickupTime;

            if (pickupProgress >= data.pickupTime)
            {
                PlayerInventory inventory = FindObjectOfType<PlayerInventory>();
                if (inventory != null)
                    inventory.AddItem(data);

                gameObject.SetActive(false);
            }
        }
        else
        {
            pickupProgress = 0f;

            if (progressRing != null)
                progressRing.fillAmount = 0f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            if (interactUI != null)
                interactUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby   = false;
            pickupProgress = 0f;

            if (interactUI != null)
                interactUI.SetActive(false);

            if (progressRing != null)
                progressRing.fillAmount = 0f;
        }
    }
}

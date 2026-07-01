using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemPickup : MonoBehaviour
{
    public ItemData data;

    [Header("UI")]
    public GameObject interactUI;  // el canvas que vas a crear
    public Image      progressRing; // el aro de progreso

    private bool  playerNearby   = false;
    private float pickupProgress = 0f;

    void Start()
    {
        if (interactUI != null)
            interactUI.SetActive(false);
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

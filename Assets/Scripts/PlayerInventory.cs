using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private List<ItemData> items        = new List<ItemData>();
    private float          totalWeight  = 0f;
    private int            totalValue   = 0;

    private PlayerMovement playerMovement;
    private float          baseWalkSpeed;
    private float          baseRunSpeed;
    private float          baseSneakSpeed;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        baseWalkSpeed  = playerMovement.walkSpeed;
        baseRunSpeed   = playerMovement.runSpeed;
        baseSneakSpeed = playerMovement.sneakSpeed;
    }

    public void AddItem(ItemData item)
    {
        items.Add(item);

        totalWeight += item.weight;
        totalValue  += item.value;

        ApplyWeight();
    }

    void ApplyWeight()
    {
        playerMovement.walkSpeed  = Mathf.Max(1f, baseWalkSpeed  - totalWeight);
        playerMovement.runSpeed   = Mathf.Max(2f, baseRunSpeed   - totalWeight);
        playerMovement.sneakSpeed = Mathf.Max(0.5f, baseSneakSpeed - totalWeight);
    }

    // Por si otra parte del juego necesita estos datos
    public float GetTotalWeight() { return totalWeight; }
    public int   GetTotalValue()  { return totalValue;  }
    public List<ItemData> GetItems() { return items; }
}

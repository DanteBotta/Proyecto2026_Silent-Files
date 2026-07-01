using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Heist/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public float  weight;
    public float  pickupTime;
    public int    value;
}

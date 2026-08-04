using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleDoor : MonoBehaviour
{
    public void Open()
    {
        Debug.Log("¡PUERTA DESBLOQUEADA!");
    }

    public void LockpickFailed()
    {
        Debug.Log("La ganzúa no consiguió abrir la puerta.");
    }

    public void LockpickBroken()
    {
        Debug.Log("¡La ganzúa se rompió!");
    }
}

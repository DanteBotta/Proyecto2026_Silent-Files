using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLockpick : MonoBehaviour
{
    public LockpickMinigameController lockpickManager;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            lockpickManager.StartMinigame();
        }
    }
}

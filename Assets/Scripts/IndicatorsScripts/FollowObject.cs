
using UnityEngine;

public class FollowObject : MonoBehaviour
{
    public Transform objeto;
    public MinimapaManager manager;

    void LateUpdate()
    {
        if (objeto == null)
        {
            Destroy(gameObject);
            return;
        }

        if (manager == null)
        {
            return;
        }

        transform.position = objeto.position
            + Vector3.up * manager.AlturaIndicadores;

        transform.rotation = Quaternion.Euler(90f, objeto.eulerAngles.z, 0f);
    }
}

using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform camara;

    void LateUpdate()
    {
        transform.position = camara.position;
        transform.rotation = camara.rotation;
    }
}

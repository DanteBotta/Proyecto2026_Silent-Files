using UnityEngine;

public class ShowCanvasNearPlayer : MonoBehaviour
{
    [Header("Configuración")]
    public Transform player; // The object from which the distance is calculated
    public float distanciaActivacion = 5f; // The distance at which the canvas will activate

    private Canvas canvas; // Reference to the Canvas component

    void Start()
    {
        // Get the Canvas component attached to this GameObject
        canvas = GetComponent<Canvas>();

        // Start with the canvas disabled
        canvas.enabled = false;
    }

    void Update()
    {
        // Vector3.Distance calculates the distance between two points in 3D space
        float distancia = Vector3.Distance(player.position, transform.position);

        // Check if the distance is less than or equal to the activation distance
        if (distancia <= distanciaActivacion)
        {
            canvas.enabled = true;
        }
        else
        {
            canvas.enabled = false;
        }
    }
}
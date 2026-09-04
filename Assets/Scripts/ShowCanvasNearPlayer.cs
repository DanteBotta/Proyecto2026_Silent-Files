using UnityEngine;

public class ShowCanvasNearPlayer : MonoBehaviour
{
    [Header("Configuración")]
    public Transform player; // The object from which the distance is calculated
    public float distanciaActivacion = 5f; // The distance at which the canvas will activate
    public float distanciaFuncion = 2f; // The distance at which the canvas will function (not used in this script)
    public bool FuncionCanvas = false; // A boolean to control the canvas functionality (not used in this script)

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

        // Activa/desactiva el Canvas según la distancia
        canvas.enabled = distancia <= distanciaActivacion;

        // Activa/desactiva su funcionamiento según la distancia
        FuncionCanvas = distancia <= distanciaFuncion;
    }
}
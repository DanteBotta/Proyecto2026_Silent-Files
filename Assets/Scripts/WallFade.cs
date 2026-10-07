using System.Collections.Generic;
using UnityEngine;

public class WallFade : MonoBehaviour
{
    [Header("Referencias")]
    public Transform Objetivo; // Objetivo del spherecast (normalmente el jugador)

    [Header("Raycast")]
    public float AlcanceRayCast = 10f; // Distancia máxima del SphereCast
    public float RadioRayCast = 0.5f; // Radio del SphereCast
    public float AlturaObjetivo = 1f; // Altura extra del objetivo

    [Header("Transparencia")]
    [Range(0f, 1f)] public float Transparencia = 0.3f; // Valor de alpha cuando el objeto está tapando al objetivo
    public float VelocidadFade = 2f; // Velocidad a la que el objeto se vuelve transparente u opaco

    [Header("Debug")]
    public bool MostrarDebug = true; // Muestra el SphereCast en la escena

    private HashSet<Renderer> renderersControlados = new HashSet<Renderer>(); // Renderers que están siendo controlados actualmente por este script
    // HashSet es una colección que guarda varios Renderer, pero sin repetir elementos

    private List<Renderer> paraQuitar = new List<Renderer>(); // Renderers que ya no están tapando al objetivo y que deben dejar de ser controlados
    // List<Renderer> paraQuitar es una lista temporal de renderers que hay que dejar de controlar

    private void Start()
    {
        Objetivo = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        Vector3 destino = Objetivo.position + Vector3.up * AlturaObjetivo; // Se obtiene el destino del spherecast
            // Objetivo.position = posición del objetivo (jugador) en el mundo
            // Vector3.up * AlturaObjetivo = Es una valor que se suma en el eje Y para que el spherecast no apunte al suelo

        Vector3 direccion = (destino - transform.position).normalized; // Se obtiene la dirección del spherecast
            // destino - transform.position = calcula el vector que va desde el objeto que tiene este script hasta el destino
            // .normalized = Se normaliza el vector para que tenga longitud 1

        // Physics.SphereCastAll() hace un SphereCast, lanza una especie de esfera a lo largo de una dirección y devuelve todos los objetos que encuentra
        // Se guarda en RaycastHit[] impactos que es una array de todos los objetos que impacta el SphereCast
        RaycastHit[] impactos = Physics.SphereCastAll( 
            transform.position, // Desde donde empieza
            RadioRayCast, // El tamaño de la espera
            direccion, // La dirección
            AlcanceRayCast // Su alcanze
        );

        // Renderers detectados durante ESTE frame, cada frame se genera una nuevo
        HashSet<Renderer> renderersActuales = new HashSet<Renderer>();

        // Recorre todos los impactos del SphereCast
        foreach (RaycastHit impacto in impactos)
        {
            if (!impacto.collider.CompareTag("Pared")) // Comprueba si el collider que choco tiene el tag "Pared"
                continue; // Si no tiene el tag "Pared", pasa al siguiente impacto, no lo considera

            // Obtiene el renderer del objeto que choco y todos sus hijos
            Renderer[] renderers = impacto.collider.GetComponentsInChildren<Renderer>();

            foreach (Renderer r in renderers) // Recorre todos y cada uno de los que encontró
            {
                renderersActuales.Add(r); // Este esta sindo controlado ESTE frame
                renderersControlados.Add(r); // Este esta siendo controlado
            }
        }

        paraQuitar.Clear(); // Limpia la lista de renderers que ya no están tapando al objetivo

        foreach (Renderer r in renderersControlados) // Recorre todos los renders controlados
        {
            // Por si el objeto fue destruido
            if (r == null)
            {
                paraQuitar.Add(r); // Se agrega en quitar
                continue;
            }

            bool tapando = renderersActuales.Contains(r); // Verifica si la pared esta tapando actualmente o no en ESTE 
            float alphaMeta = tapando ? Transparencia : 1f; // Si esta tapando, el alpha meta es la transparencia, si no, es 1 (opaco)

            bool terminado = true;

            // Recorre todos los materiales, ya que pueden tener varios
            foreach (Material mat in r.materials) 
            {
                if (!mat.HasProperty("_Color")) // Verifica si tiene color
                    continue; // Si no pasa al siguiente

                Color color = mat.color; // Se obtiene el color del material

                color.a = Mathf.MoveTowards( // Hace que un valor se acerque a otro valor a una velocidad determinada
                    color.a, // Color actual
                    alphaMeta, // Color objetivo
                    VelocidadFade * Time.deltaTime // Tiempo de duración del cambio
                );

                mat.color = color; // Le aplica el color al material

                if (!Mathf.Approximately(color.a, alphaMeta))
                    terminado = false;
            }

            // Si ya volvió a ser opaco y no está tapando, dejamos de controlarlo
            if (!tapando && terminado) // Verifica si ya no esta tapando y si ya termino de hacer el fade
                paraQuitar.Add(r);
        }

        foreach (Renderer r in paraQuitar) // Se recorrecon todos los renderer en paraQuitar
        {
            renderersControlados.Remove(r); // Lo saca de los renderers controlados
        }
    }

    // Permite visualizar el SphereCast en la escena
    private void OnDrawGizmos()
    {
        if (!MostrarDebug || Objetivo == null) // Solamente si esta debug en true y existe un objetivo
            return;

        Vector3 destino = Objetivo.position + Vector3.up * AlturaObjetivo; 
        Vector3 direccion = (destino - transform.position).normalized; 

        Vector3 inicio = transform.position; // Guarda la posición del objeto del script
        Vector3 final = transform.position + direccion * AlcanceRayCast; // Calcula la posición final del spherecast

        Gizmos.DrawLine(inicio, final); // Dibuja una línea entre el inicio y el final del spherecast
        Gizmos.DrawWireSphere(inicio, RadioRayCast); // Dibuja una esfera en el inicio del spherecast con el radio del spherecast
        Gizmos.DrawWireSphere(final, RadioRayCast); // Dibuja una esfera en el final del spherecast con el radio del spherecast
        Gizmos.DrawWireSphere(destino, RadioRayCast); // Dibuja una esfera en el destino del spherecast con el radio del spherecast
    }
}
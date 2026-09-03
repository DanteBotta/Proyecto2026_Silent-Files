using System.Collections.Generic;
using UnityEngine;

public class WallFade : MonoBehaviour
{
    [Header("Referencias")]
    public Transform Objetivo; // The target object that the raycast will be directed

    [Header("Raycast")]
    public float AlcanceRayCast = 10f; // Distance at which the wall starts to fade

    [Header("Transparencia")]
    [Range(0f, 1f)]
    public float Transparencia = 0.3f; // The transparency value to apply to the wall when it is hit by the raycast
    public float VelocidadFade = 2f; // The speed at which the wall fades in and out

    // create a list which can contain 'MeshRenderer' type objects that were affected during the previous frame
    // = new List<MeshRenderer>() : creates a new empty list of type 'MeshRenderer' and assigns it to the variable 'paredesControladas'
    private List<MeshRenderer> paredesControladas = new List<MeshRenderer>();
    // Update is called once per frame
    void Update()
    {
        // Calculate the direction from the current object to the target object
        Vector3 direccion = (Objetivo.position - transform.position).normalized; // Target position + current object position = displacement from camera to target


        // Raycast Annotation

        // RaycastHit: variable type to store information about ONE impact of the raycast
        // RaycastHit[]: An array of RaycastHit objects that store information about ALL the objects hit by the raycast
        // impactos: variable called impactos capable of storing multiple RaycastHit

        // Physics.Raycast(...): The function that performs a raycast in the Unity scene and returns information about the first object hit by the raycast
        // Physics.RaycastAll(...): perform a raycast in the Unity, but returns information about ALL objects hit by
        // {
        //     transform.position: The starting point of the raycast (the position of the current object)
        //     direccion: the physical direction to which the raycast is directed
        //     AlcanceRayCast: The maximum distance of the raycast
        // }


        // It casts a raycast from the object position, in a certain direction, up to a maximum distance, and stores information about ALL objects it hits.
        RaycastHit[] impactos = Physics.RaycastAll(
            transform.position,
            direccion,
            AlcanceRayCast
        );
        
        // List of walls that are being hit by the raycast NOW
        List<MeshRenderer> paredesActuales = new List<MeshRenderer>();


        // Check every object hit by the raycast
        //      RaycastHit impacto: For each object hit by the raycast, 'impacto' of type RaycastHit stores information about that specific impact
        foreach (RaycastHit impacto in impactos)
        {
            if (impacto.collider.CompareTag("Pared")) // Check if the object hit by the raycast has the tag "Pared"
            {
                // Get the MeshRenderer component of the object hit by the raycast
                //      MeshRenderer pared: variable of type MeshRenderer that will store the MeshRenderer component of the object hit by the raycast
                //      impacto.collider: The collider of the object hit by the raycast
                //      .GetComponent<MeshRenderer>(): Get the MeshRenderer component of the object hit by the raycast
                MeshRenderer pared = impacto.collider.GetComponent<MeshRenderer>();

                if (pared != null) // Check if the MeshRenderer component was found, if not, it means that the object cannot change its color
                {
                    // Add the wall to the list of currently affected walls
                    paredesActuales.Add(pared);

                    // Add the wall to the list of walls that were affected during the previous frame, if it is not already in the list
                    if (!paredesControladas.Contains(pared))
                    {
                        paredesControladas.Add(pared);
                    }
                }
            }
        }


        // Check the walls that were affected during the PREVIOUS frame
        //      MeshRenderer pared: For each object in paredesControladas of type MeshRenderer stores information about that specific MeshRenderer
        foreach (MeshRenderer pared in paredesControladas)
        {
            if (paredesActuales.Contains(pared)) // Check if the wall is still being hit by the raycast in the current frame
            {
                // Fade IN

                // Keep the RGB color of the wall and only change its Alpha (transparency)
                //  {
                //      .material.color: Change the color of the wall material
                //      color.a: Change the Alpha (transparency) of the wall color
                //  }

                Color color = pared.material.color;

                // Use Mathf.MoveTowards to gradually change the alpha value of the wall color towards the target transparency value
                color.a = Mathf.MoveTowards(
                    color.a,
                    Transparencia, // The target alpha value to reach (the transparency value)
                    VelocidadFade * Time.deltaTime // The speed at which the wall fades in
                );

                pared.material.color = color; // Apply the new color to the wall material
            }
            else
            {
                // Fade OUT
                Color color = pared.material.color;

                // Use Mathf.MoveTowards to gradually change the alpha value of the wall color towards 1 (fully opaque)
                color.a = Mathf.MoveTowards(
                    color.a,
                    1f, // The target alpha value to reach (fully opaque)
                    VelocidadFade * Time.deltaTime // The speed at which the wall fades out
                );

                pared.material.color = color; // Apply the new color to the wall material
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

public class WallFade : MonoBehaviour
{
    [Header("Referencias")]
    public Transform Objetivo;

    [Header("Raycast")]
    public float AlcanceRayCast = 10f;

    // Radio del área que detectará el SphereCast
    public float RadioRayCast = 0.5f;

    // Altura adicional que se suma al objetivo
    public float AlturaObjetivo = 1f;

    [Header("Transparencia")]
    [Range(0f, 1f)]
    public float Transparencia = 0.3f;

    public float VelocidadFade = 2f;

    [Header("Debug")]
    public bool MostrarDebug = true;

    // Paredes afectadas durante el frame anterior
    private List<MeshRenderer> paredesControladas = new List<MeshRenderer>();

    void Update()
    {
        // Sumamos altura al punto al que apunta el Raycast
        Vector3 destino = Objetivo.position + Vector3.up * AlturaObjetivo;

        // Calculamos la dirección desde la cámara hasta el destino
        Vector3 direccion = (destino - transform.position).normalized;

        RaycastHit[] impactos = Physics.SphereCastAll(
            transform.position,
            RadioRayCast,
            direccion,
            AlcanceRayCast
        );

        // Paredes detectadas durante ESTE frame
        List<MeshRenderer> paredesActuales = new List<MeshRenderer>();

        // Revisamos todos los objetos detectados
        foreach (RaycastHit impacto in impactos)
        {
            if (impacto.collider.CompareTag("Pared"))
            {
                MeshRenderer pared = impacto.collider.GetComponent<MeshRenderer>();

                if (pared != null)
                {
                    // Agregamos la pared a las paredes actuales
                    if (!paredesActuales.Contains(pared))
                    {
                        paredesActuales.Add(pared);
                    }

                    // Si todavía no estaba controlada, la agregamos
                    if (!paredesControladas.Contains(pared))
                    {
                        paredesControladas.Add(pared);
                    }
                }
            }
        }

        // Revisamos las paredes que estaban siendo controladas
        foreach (MeshRenderer pared in paredesControladas)
        {
            if (paredesActuales.Contains(pared))
            {
                // FADE IN
                Color color = pared.material.color;

                color.a = Mathf.MoveTowards(
                    color.a,
                    Transparencia,
                    VelocidadFade * Time.deltaTime
                );

                pared.material.color = color;
            }
            else
            {
                // FADE OUT
                Color color = pared.material.color;

                color.a = Mathf.MoveTowards(
                    color.a,
                    1f,
                    VelocidadFade * Time.deltaTime
                );

                pared.material.color = color;
            }
        }
    }

    // Permite visualizar el SphereCast en la escena
    private void OnDrawGizmos()
    {
        if (!MostrarDebug || Objetivo == null)
            return;

        Vector3 destino = Objetivo.position + Vector3.up * AlturaObjetivo;

        Vector3 direccion = (destino - transform.position).normalized;

        Vector3 inicio = transform.position;
        Vector3 final = transform.position + direccion * AlcanceRayCast;

        Gizmos.DrawLine(inicio, final);

        Gizmos.DrawWireSphere(inicio, RadioRayCast);
        Gizmos.DrawWireSphere(final, RadioRayCast);

        // Esfera en el verdadero punto de destino
        Gizmos.DrawWireSphere(destino, RadioRayCast);
    }
}
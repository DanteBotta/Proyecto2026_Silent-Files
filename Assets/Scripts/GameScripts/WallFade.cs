using System.Collections.Generic;
using UnityEngine;

public class WallFade : MonoBehaviour
{
    [Header("Referencias")]
    public Transform Objetivo; // Objetivo del spherecast (normalmente el jugador)
    public Collider[] ZonasInterior; // Colliders (Is Trigger) que juntos cubren el interior de la casa

    [Header("Techos")]
    public List<GameObject> Techos = new List<GameObject>(); // Techos que se desactivan al entrar a la casa (se asignan a mano)

    [Header("Tags")]
    public string TagPared = "Pared";

    [Header("Raycast")]
    public float AlcanceRayCast = 10f; // Distancia máxima del SphereCast
    public float RadioRayCast = 0.5f; // Radio del SphereCast
    public float AlturaObjetivo = 1f; // Altura extra del objetivo

    [Header("Transparencia")]
    [Range(0f, 1f)] public float Transparencia = 0.3f; // Valor de alpha cuando la pared está tapando al objetivo
    public float VelocidadFade = 2f; // Velocidad a la que la pared se vuelve transparente u opaca

    [Header("Debug")]
    public bool MostrarDebug = true; // Muestra el SphereCast en la escena

    private HashSet<Renderer> renderersControlados = new HashSet<Renderer>(); // Renderers controlados actualmente por este script
    private List<Renderer> paraQuitar = new List<Renderer>(); // Renderers que ya no hay que controlar
    private bool techosActivos = true; // Estado actual de los techos

    private void Start()
    {
        Objetivo = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // Devuelve true si el objetivo (jugador) está dentro de alguna de las zonas interiores
    private bool EstaDentroDeLaCasa()
    {
        if (ZonasInterior == null || Objetivo == null)
            return false;

        Vector3 pos = Objetivo.position + Vector3.up * AlturaObjetivo;

        foreach (Collider zona in ZonasInterior)
        {
            // ClosestPoint devuelve el mismo punto si este está dentro del collider
            // (funciona con BoxCollider, SphereCollider, CapsuleCollider y MeshCollider convexo)
            if (zona != null && zona.ClosestPoint(pos) == pos)
                return true;
        }

        return false;
    }

    void Update()
    {
        bool dentro = EstaDentroDeLaCasa();

        // Techos: se desactivan al entrar y se activan al salir (solo cuando cambia el estado)
        if (techosActivos == dentro)
        {
            techosActivos = !dentro;
            foreach (GameObject techo in Techos)
            {
                if (techo != null)
                    techo.SetActive(techosActivos);
            }
        }

        // Paredes que deben ser transparentes durante ESTE frame
        HashSet<Renderer> renderersActuales = new HashSet<Renderer>();

        if (dentro)
        {
            // Solo se hace el SphereCast si el jugador está dentro de la casa
            Vector3 destino = Objetivo.position + Vector3.up * AlturaObjetivo;
            Vector3 direccion = (destino - transform.position).normalized;

            RaycastHit[] impactos = Physics.SphereCastAll(
                transform.position,
                RadioRayCast,
                direccion,
                AlcanceRayCast
            );

            foreach (RaycastHit impacto in impactos)
            {
                if (!impacto.collider.CompareTag(TagPared))
                    continue;

                Renderer[] renderers = impacto.collider.GetComponentsInChildren<Renderer>();

                foreach (Renderer r in renderers)
                {
                    renderersActuales.Add(r);
                    renderersControlados.Add(r);
                }
            }
        }
        // Si está afuera, renderersActuales queda vacío: todo lo controlado vuelve a ser opaco solo

        paraQuitar.Clear();

        foreach (Renderer r in renderersControlados)
        {
            if (r == null)
            {
                paraQuitar.Add(r);
                continue;
            }

            bool tapando = renderersActuales.Contains(r);
            float alphaMeta = tapando ? Transparencia : 1f;

            bool terminado = true;

            foreach (Material mat in r.materials)
            {
                if (!mat.HasProperty("_Color"))
                    continue;

                Color color = mat.color;

                color.a = Mathf.MoveTowards(
                    color.a,
                    alphaMeta,
                    VelocidadFade * Time.deltaTime
                );

                mat.color = color;

                if (!Mathf.Approximately(color.a, alphaMeta))
                    terminado = false;
            }

            // Si ya volvió a ser opaca y no está tapando, dejamos de controlarla
            if (!tapando && terminado)
                paraQuitar.Add(r);
        }

        foreach (Renderer r in paraQuitar)
        {
            renderersControlados.Remove(r);
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

        // Verde si está dentro de la casa (efecto activo), rojo si está afuera
        Gizmos.color = EstaDentroDeLaCasa() ? Color.green : Color.red;

        Gizmos.DrawLine(inicio, final);
        Gizmos.DrawWireSphere(inicio, RadioRayCast);
        Gizmos.DrawWireSphere(final, RadioRayCast);
        Gizmos.DrawWireSphere(destino, RadioRayCast);
    }
}
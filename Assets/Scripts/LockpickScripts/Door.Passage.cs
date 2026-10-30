using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public partial class Door
{
    [Header("Paso de guardas")]
    [Tooltip("Colliders de la hoja que bloquean el paso. Vacío: busca los colliders sólidos de esta puerta.")]
    [SerializeField] private Collider[] collidersBloqueo;
    [Tooltip("Volumen del hueco cerrado. Sin asignar, usa el primer BoxCollider de la hoja. Su posición se guarda al iniciar.")]
    [SerializeField] private BoxCollider volumenPaso;

    [Header("Animación (opcional)")]
    [Tooltip("Para Animation Events directos, el Animator debe estar en el mismo objeto que Door. También se puede controlar desde los eventos de solicitud.")]
    [SerializeField] private Animator animatorPuerta;
    [SerializeField] private string parametroAbierta = "Abierta";
    [Tooltip("Activar cuando los clips llamen a NotificarPuertaAbierta y NotificarPuertaCerrada. Sin animación, dejar desactivado.")]
    [SerializeField] private bool esperarEventosAnimacion;
    [SerializeField] private UnityEvent alSolicitarApertura = new UnityEvent();
    [SerializeField] private UnityEvent alSolicitarCierre = new UnityEvent();

    private enum FaseMovimiento { Cerrada, Abriendo, Abierta, Cerrando }

    private static readonly List<Door> activeDoors = new List<Door>();
    public static IReadOnlyList<Door> ActiveDoors => activeDoors;
    public bool IsPassable => isActiveAndEnabled && fase == FaseMovimiento.Abierta;

    private readonly HashSet<GuardDoorPassage> guardasEnPaso = new HashSet<GuardDoorPassage>();
    private EstadoPuerta estadoAntesDelPaso;
    private FaseMovimiento fase;
    private bool[] collidersHabilitados;
    private Bounds limitesPaso;
    private Matrix4x4 mundoAPaso;
    private int revisionEstado;

    private void Awake()
    {
        if (collidersBloqueo == null || collidersBloqueo.Length == 0)
        {
            var solidos = new List<Collider>();
            foreach (Collider candidato in GetComponentsInChildren<Collider>(true))
            {
                if (!candidato.isTrigger && candidato.GetComponentInParent<Door>() == this)
                    solidos.Add(candidato);
            }
            collidersBloqueo = solidos.ToArray();
        }

        collidersHabilitados = new bool[collidersBloqueo.Length];
        for (int index = 0; index < collidersBloqueo.Length; index++)
        {
            Collider bloqueo = collidersBloqueo[index];
            if (bloqueo == null)
                continue;

            collidersHabilitados[index] = bloqueo.enabled;
            if (volumenPaso == null && bloqueo is BoxCollider caja)
                volumenPaso = caja;
        }

        // Animar/deshabilitar la hoja no debe mover el hueco que el guarda debe despejar.
        if (volumenPaso != null)
        {
            Transform caja = volumenPaso.transform;
            mundoAPaso = Matrix4x4.TRS(caja.TransformPoint(volumenPaso.center), caja.rotation, Vector3.one).inverse;
            limitesPaso = new Bounds(Vector3.zero, Vector3.Scale(volumenPaso.size, Abs(caja.lossyScale)));
        }
        else
        {
            mundoAPaso = Matrix4x4.identity;
            limitesPaso = new Bounds(transform.position, new Vector3(1f, 2f, 0.2f));
            bool primerCollider = true;
            foreach (Collider bloqueo in collidersBloqueo)
            {
                if (bloqueo == null || !bloqueo.enabled || !bloqueo.gameObject.activeInHierarchy)
                    continue;
                if (primerCollider)
                    limitesPaso = bloqueo.bounds;
                else
                    limitesPaso.Encapsulate(bloqueo.bounds);
                primerCollider = false;
            }
        }

        if (animatorPuerta == null)
            animatorPuerta = GetComponent<Animator>();
        AplicarEstadoInmediato();
    }

    private void OnEnable()
    {
        if (!activeDoors.Contains(this))
            activeDoors.Add(this);
    }

    private void OnDisable()
    {
        activeDoors.Remove(this);
        if (guardasEnPaso.Count > 0)
        {
            estadoActual = estadoAntesDelPaso;
            guardasEnPaso.Clear();
            revisionEstado++;
        }
        AplicarEstadoInmediato();
    }

    public bool RequestGuardPassage(GuardDoorPassage guarda)
    {
        if (!isActiveAndEnabled || guarda == null)
            return false;
        if (guardasEnPaso.Contains(guarda))
            return true;

        if (guardasEnPaso.Count == 0)
            estadoAntesDelPaso = estadoActual;
        guardasEnPaso.Add(guarda);
        CambiarEstado(EstadoPuerta.Abierta);
        return true;
    }

    public void ReleaseGuardPassage(GuardDoorPassage guarda)
    {
        if (!guardasEnPaso.Remove(guarda) || guardasEnPaso.Count > 0)
            return;
        CambiarEstado(estadoAntesDelPaso);
    }

    // Las posiciones recibidas son los pies del agente, no el centro del modelo.
    public bool TryGetPathIntersection(Vector3 start, Vector3 end, float radius, float height, out float distance)
    {
        Vector3 centro = Vector3.up * (height * 0.5f);
        Vector3 inicio = mundoAPaso.MultiplyPoint3x4(start + centro);
        Vector3 desplazamiento = mundoAPaso.MultiplyVector(end - start);
        Bounds limites = LimitesParaGuarda(radius, height, 0f);
        distance = 0f;
        if (limites.Contains(inicio))
            return true;
        float longitud = desplazamiento.magnitude;
        return longitud > 0.001f &&
            limites.IntersectRay(new Ray(inicio, desplazamiento / longitud), out distance) &&
            distance <= longitud;
    }

    public bool IsGuardClear(Vector3 position, float radius, float height, float margin)
    {
        Vector3 centro = mundoAPaso.MultiplyPoint3x4(position + Vector3.up * (height * 0.5f));
        return !LimitesParaGuarda(radius, height, margin).Contains(centro);
    }

    private Bounds LimitesParaGuarda(float radius, float height, float margin)
    {
        Bounds limites = limitesPaso;
        Vector3 extension = Abs(mundoAPaso.MultiplyVector(Vector3.right * (radius + margin))) +
            Abs(mundoAPaso.MultiplyVector(Vector3.forward * (radius + margin))) +
            Abs(mundoAPaso.MultiplyVector(Vector3.up * (height * 0.5f)));
        limites.Expand(extension * 2f);
        return limites;
    }

    private static Vector3 Abs(Vector3 value) => new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

    private void CambiarEstado(EstadoPuerta nuevoEstado)
    {
        estadoActual = nuevoEstado;
        revisionEstado++;
        bool abrir = nuevoEstado == EstadoPuerta.Abierta;
        if ((abrir && (fase == FaseMovimiento.Abierta || fase == FaseMovimiento.Abriendo)) ||
            (!abrir && (fase == FaseMovimiento.Cerrada || fase == FaseMovimiento.Cerrando)))
            return;

        fase = abrir ? FaseMovimiento.Abriendo : FaseMovimiento.Cerrando;
        ActualizarAnimator(abrir);
        if (abrir)
        {
            alSolicitarApertura.Invoke();
            if (!esperarEventosAnimacion)
                NotificarPuertaAbierta();
        }
        else
        {
            alSolicitarCierre.Invoke();
            if (!esperarEventosAnimacion)
                NotificarPuertaCerrada();
        }
    }

    // Animation Events: apertura cuando la hoja ya deja pasar; cierre al final del clip.
    public void NotificarPuertaAbierta()
    {
        if (fase != FaseMovimiento.Abriendo)
            return;
        fase = FaseMovimiento.Abierta;
        ActualizarColliders(false);
    }

    public void NotificarPuertaCerrada()
    {
        if (fase != FaseMovimiento.Cerrando)
            return;
        fase = FaseMovimiento.Cerrada;
        ActualizarColliders(true);
    }

    private void AplicarEstadoInmediato()
    {
        bool abierta = estadoActual == EstadoPuerta.Abierta;
        fase = abierta ? FaseMovimiento.Abierta : FaseMovimiento.Cerrada;
        ActualizarColliders(!abierta);
        ActualizarAnimator(abierta);
    }

    private void ActualizarColliders(bool bloquear)
    {
        if (collidersHabilitados == null)
            return;
        for (int index = 0; index < collidersBloqueo.Length; index++)
        {
            if (collidersBloqueo[index] != null)
                collidersBloqueo[index].enabled = bloquear && collidersHabilitados[index];
        }
    }

    private void ActualizarAnimator(bool abierta)
    {
        if (animatorPuerta == null || animatorPuerta.runtimeAnimatorController == null ||
            string.IsNullOrEmpty(parametroAbierta))
            return;

        // El controller actual todavía no tiene parámetros para las puertas.
        foreach (AnimatorControllerParameter parametro in animatorPuerta.parameters)
        {
            if (parametro.name == parametroAbierta && parametro.type == AnimatorControllerParameterType.Bool)
            {
                animatorPuerta.SetBool(parametroAbierta, abierta);
                break;
            }
        }
    }
}

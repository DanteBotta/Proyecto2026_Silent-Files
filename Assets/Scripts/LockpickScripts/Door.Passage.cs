using System.Collections.Generic;
using UnityEngine;

// Paso de guardas por puertas, estilo aldeano de Minecraft:
// el guarda pide pasar -> la puerta se abre sola (rotando la bisagra por código),
// el guarda cruza y, cuando termina de pasar, la puerta se cierra sola.
// No usa animaciones ni apaga colliders: el collider de la hoja es hijo de la bisagra
// y se mueve junto con el modelo.
public partial class Door
{
    [Header("Paso de guardas")]
    [Tooltip("Colliders de la hoja. Sólo se usan para calcular el hueco de la puerta. Vacío: busca los colliders sólidos de esta puerta.")]
    [SerializeField] private Collider[] collidersBloqueo;
    [Tooltip("Volumen del hueco cerrado. Sin asignar, usa el primer BoxCollider de la hoja. Su posición se guarda al iniciar.")]
    [SerializeField] private BoxCollider volumenPaso;
    [Tooltip("Velocidad (grados por segundo) con la que la puerta se abre y se cierra sola cuando pasa un guarda.")]
    [SerializeField, Min(1f)] private float velocidadGuardaGrados = 240f;

    private enum FaseMovimiento { Cerrada, Abriendo, Abierta, Cerrando }

    // Qué tan cerca (en grados) tiene que estar la bisagra de su objetivo para considerarla llegada.
    private const float ToleranciaGrados = 1f;

    private static readonly List<Door> activeDoors = new List<Door>();
    public static IReadOnlyList<Door> ActiveDoors => activeDoors;

    // Pasable sólo cuando la hoja realmente llegó a la posición abierta (también si la abrió el jugador).
    public bool IsPassable => isActiveAndEnabled && fase == FaseMovimiento.Abierta && (!BisagraLista || EstaEn(RotacionAbierta));

    private readonly HashSet<GuardDoorPassage> guardasEnPaso = new HashSet<GuardDoorPassage>();
    private EstadoPuerta estadoAntesDelPaso;
    private FaseMovimiento fase;
    private Bounds limitesPaso;
    private Matrix4x4 mundoAPaso;
    private int revisionEstado;

    // Door.cs calcula las rotaciones en Start(); hasta entonces no se puede mover la bisagra.
    private bool BisagraLista =>
        Bisagra != null &&
        (RotacionCerrada.x != 0f || RotacionCerrada.y != 0f || RotacionCerrada.z != 0f || RotacionCerrada.w != 0f);

    private bool EstaEn(Quaternion objetivo) =>
        BisagraLista && Quaternion.Angle(Bisagra.localRotation, objetivo) < ToleranciaGrados;

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

        if (volumenPaso == null)
        {
            foreach (Collider bloqueo in collidersBloqueo)
            {
                if (bloqueo is BoxCollider caja)
                {
                    volumenPaso = caja;
                    break;
                }
            }
        }

        // La hoja rota con la bisagra, pero el hueco que el guarda debe despejar queda fijo:
        // se calcula una sola vez, con la puerta en su posición inicial.
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

        fase = estadoActual == EstadoPuerta.Abierta ? FaseMovimiento.Abierta : FaseMovimiento.Cerrada;
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

        // Si se desactiva en medio de un movimiento, la puerta queda directamente en su estado final.
        bool abierta = estadoActual == EstadoPuerta.Abierta;
        fase = abierta ? FaseMovimiento.Abierta : FaseMovimiento.Cerrada;
        if (BisagraLista)
        {
            RotacionObjetivo = abierta ? RotacionAbierta : RotacionCerrada;
            Bisagra.localRotation = RotacionObjetivo;
        }
    }

    // Mientras la puerta se mueve por un guarda (fase Abriendo/Cerrando o con guardas en paso),
    // Door.Update() no toca la bisagra, así que la mueve este método.
    private void LateUpdate()
    {
        ActualizarTextoEstado();

        if (!BisagraLista)
            return;

        if (fase == FaseMovimiento.Abriendo)
        {
            GirarHacia(RotacionAbierta);
            if (EstaEn(RotacionAbierta))
            {
                Bisagra.localRotation = RotacionAbierta;
                fase = FaseMovimiento.Abierta;
            }
        }
        else if (fase == FaseMovimiento.Cerrando)
        {
            GirarHacia(RotacionCerrada);
            if (EstaEn(RotacionCerrada))
            {
                Bisagra.localRotation = RotacionCerrada;
                fase = FaseMovimiento.Cerrada;
            }
        }
    }

    private void GirarHacia(Quaternion objetivo)
    {
        Bisagra.localRotation = Quaternion.RotateTowards(
            Bisagra.localRotation, objetivo, velocidadGuardaGrados * Time.deltaTime);
    }

    // El guarda pide pasar: la puerta empieza a abrirse. Hay que esperar a IsPassable antes de cruzar.
    public bool RequestGuardPassage(GuardDoorPassage guarda)
    {
        if (!isActiveAndEnabled || guarda == null)
            return false;
        if (guardasEnPaso.Contains(guarda))
            return true;

        if (guardasEnPaso.Count == 0)
            estadoAntesDelPaso = estadoActual;
        guardasEnPaso.Add(guarda);

        estadoActual = EstadoPuerta.Abierta;
        revisionEstado++;
        RotacionObjetivo = RotacionAbierta;
        fase = EstaEn(RotacionAbierta) ? FaseMovimiento.Abierta : FaseMovimiento.Abriendo;
        return true;
    }

    // El guarda ya pasó. Cuando se va el último, la puerta se cierra sola
    // (también si el jugador la había dejado abierta).
    public void ReleaseGuardPassage(GuardDoorPassage guarda)
    {
        if (!guardasEnPaso.Remove(guarda) || guardasEnPaso.Count > 0)
            return;

        // La puerta siempre se cierra al terminar de pasar, incluso si ya estaba abierta.
        // Si estaba trabada o bloqueada, recupera ese estado.
        estadoActual = estadoAntesDelPaso == EstadoPuerta.Abierta ? EstadoPuerta.Cerrada : estadoAntesDelPaso;
        revisionEstado++;

        RotacionObjetivo = RotacionCerrada;
        fase = EstaEn(RotacionCerrada) ? FaseMovimiento.Cerrada : FaseMovimiento.Cerrando;
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

    // Completan al instante el movimiento de apertura/cierre iniciado por un guarda.
    // El juego no las necesita (LateUpdate llega solo), pero los tests las usan para simular que la puerta terminó de moverse.
    public void NotificarPuertaAbierta()
    {
        if (fase != FaseMovimiento.Abriendo)
            return;
        if (BisagraLista)
            Bisagra.localRotation = RotacionAbierta;
        fase = FaseMovimiento.Abierta;
    }

    public void NotificarPuertaCerrada()
    {
        if (fase != FaseMovimiento.Cerrando)
            return;
        if (BisagraLista)
            Bisagra.localRotation = RotacionCerrada;
        fase = FaseMovimiento.Cerrada;
    }

    // Lo usa Door.cs cuando el jugador abre, cierra, destraba o traba la puerta.
    // La rotación la sigue haciendo Door.Update() con el Slerp de siempre.
    private void CambiarEstado(EstadoPuerta nuevoEstado)
    {
        estadoActual = nuevoEstado;
        revisionEstado++;
        fase = nuevoEstado == EstadoPuerta.Abierta ? FaseMovimiento.Abierta : FaseMovimiento.Cerrada;
    }
}
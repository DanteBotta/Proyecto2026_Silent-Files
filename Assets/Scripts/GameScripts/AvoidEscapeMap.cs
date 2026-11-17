
using UnityEngine;
using System.Collections;

public class AviodEscapeMap : MonoBehaviour
{
    [Header("Configuración")]
    public float distanciaRetroceso = 2f;
    public float velocidadRetroceso = 3f;
    public float velocidadRotacion = 360f;

    private CharacterController controller;
    private MonoBehaviour movimientoJugador;
    private bool retrocediendo = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Buscar el script de movimiento del jugador.
        // Reemplazar PersonajeController por el nombre real
        // de tu script de movimiento.
        movimientoJugador = GetComponent<PlayerController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MapLimiters") && !retrocediendo)
        {
            StartCoroutine(Retroceder());
        }
    }

    private IEnumerator Retroceder()
    {
        retrocediendo = true;

        // Desactivar el control del jugador.
        if (movimientoJugador != null)
            movimientoJugador.enabled = false;

        // Girar 180 grados respecto a la dirección actual.
        Quaternion rotacionObjetivo =
            Quaternion.Euler(0, transform.eulerAngles.y + 180f, 0);

        while (Quaternion.Angle(transform.rotation, rotacionObjetivo) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                rotacionObjetivo,
                velocidadRotacion * Time.deltaTime
            );

            yield return null;
        }

        // Retroceder utilizando CharacterController.
        float distanciaRecorrida = 0f;

        while (distanciaRecorrida < distanciaRetroceso)
        {
            float movimiento = velocidadRetroceso * Time.deltaTime;

            controller.Move(transform.forward * movimiento);

            distanciaRecorrida += movimiento;

            yield return null;
        }

        // Reactivar el control.
        if (movimientoJugador != null)
            movimientoJugador.enabled = true;

        retrocediendo = false;
    }
}

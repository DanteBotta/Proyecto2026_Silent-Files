using UnityEngine;

/// <summary>
/// Hace que este objeto (típicamente un Canvas en modo World Space, pero sirve
/// para cualquier GameObject) siempre esté mirando hacia la cámara. Útil para
/// barras de vida, indicadores de sospecha, nombres flotantes, iconos de
/// interacción, etc.
///
/// Se puede asignar a cualquier objeto: solo agregalo como componente.
/// No requiere ninguna referencia manual si hay una cámara con el tag
/// "MainCamera" en la escena, pero también se puede asignar a mano.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Referencia")]
    [Tooltip("Cámara a la que va a mirar. Si se deja vacío, usa Camera.main automáticamente.")]
    public Camera targetCamera;
 
    [Header("Modo de rotación")]
    [Tooltip("Si está activo, el objeto copia exactamente la rotación de la cámara (recomendado para Canvas de UI, texto, íconos). Si está desactivado, en cambio 'mira hacia' la posición de la cámara, lo cual puede verse espejado o inclinado según el ángulo.")]
    public bool matchCameraRotation = true;
 
    [Tooltip("Si es true, se ignora la inclinación vertical de la cámara y el objeto solo rota en el eje Y (útil si no querés que el Canvas se incline hacia arriba/abajo cuando la cámara mira desde arriba, como en una cámara top-down).")]
    public bool onlyYAxis = false;
 
    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }
 
    private void LateUpdate()
    {
        if (targetCamera == null) return;
 
        if (matchCameraRotation)
        {
            if (onlyYAxis)
            {
                Vector3 camEuler = targetCamera.transform.eulerAngles;
                transform.rotation = Quaternion.Euler(0f, camEuler.y, 0f);
            }
            else
            {
                transform.rotation = targetCamera.transform.rotation;
            }
        }
        else
        {
            // "Mira hacia" la cámara desde la posición del objeto.
            // Se invierte la dirección (LookRotation apunta el eje Z hacia adelante,
            // pero un Canvas normalmente necesita que su "frente" mire a la cámara,
            // no que la cámara quede detrás).
            Vector3 directionToCamera = transform.position - targetCamera.transform.position;
 
            if (onlyYAxis)
            {
                directionToCamera.y = 0f;
            }
 
            if (directionToCamera.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(directionToCamera);
            }
        }
    }
}

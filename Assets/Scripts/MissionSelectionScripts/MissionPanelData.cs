using UnityEngine;

/// <summary>
/// Ponelo en cada panel de misión para definir su nombre, su imagen y si está bloqueada.
/// Es opcional: si no lo ponés, el manager usa el nombre del GameObject, no muestra imagen
/// y la misión está desbloqueada.
/// </summary>
public class MissionPanelData : MonoBehaviour
{
    [Tooltip("Nombre que se muestra en el botón del menú. Si queda vacío, usa el nombre del GameObject.")]
    public string displayName;

    [Tooltip("Imagen del botón del menú.")]
    public Sprite thumbnail;

    [Tooltip("Si está bloqueada: el botón no se puede usar, se salta con las flechas y la hoja nunca se muestra.")]
    public bool isLocked;
}
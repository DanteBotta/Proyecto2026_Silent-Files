using UnityEngine;

/// <summary>
/// Ponelo en cada panel de misión para definir su nombre, su imagen, su dificultad y su estado.
/// Es opcional: si no lo ponés, el manager usa el nombre del GameObject, no muestra imagen,
/// la dificultad es 1, no está bloqueada y se puede seleccionar.
/// </summary>
public class MissionPanelData : MonoBehaviour
{
    public const int MinDifficulty = 1;
    public const int MaxDifficulty = 5;

    [Tooltip("Nombre que se muestra en el botón del menú. Si queda vacío, usa el nombre del GameObject.")]
    public string displayName;

    [Tooltip("Imagen del botón del menú.")]
    public Sprite thumbnail;

    [Tooltip("Dificultad de la misión, de 1 a 5 (solo números enteros). Se muestra como estrellas en el botón del menú.")]
    [Range(MinDifficulty, MaxDifficulty)]
    public int difficulty = MinDifficulty;

    [Tooltip("Si está bloqueada: se muestra el panel del candado por encima del botón (nombre e imagen quedan debajo) y se ocultan las estrellas. Esto es solo visual; si además no querés que se pueda elegir, desmarcá 'Is Selectable'.")]
    public bool isLocked;

    [Tooltip("Si se puede seleccionar. Si está desmarcado: el botón no hace nada, se salta con las flechas y la hoja nunca se muestra. Las estrellas de dificultad tampoco se muestran.")]
    public bool isSelectable = true;

    /// <summary>Dificultad siempre dentro del rango válido (1 a 5).</summary>
    public int Difficulty => Mathf.Clamp(difficulty, MinDifficulty, MaxDifficulty);

    private void OnValidate()
    {
        difficulty = Mathf.Clamp(difficulty, MinDifficulty, MaxDifficulty);
    }
}
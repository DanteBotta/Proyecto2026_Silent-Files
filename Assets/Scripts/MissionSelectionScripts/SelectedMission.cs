using UnityEngine;

/// <summary>
/// Guarda cuál misión se eligió, como NÚMERO, para leerla desde cualquier escena.
///
/// Uso desde otro script (por ejemplo en la escena "Gameplay"):
///     int indice = SelectedMission.Index;    // 0 = primera misión, 1 = segunda, ...  (-1 = no se eligió ninguna)
///     int numero = SelectedMission.Number;   // 1 = primera misión, 2 = segunda, ...  (0 = no se eligió ninguna)
///     if (SelectedMission.HasSelection) { ... }
///
/// El orden es el de los paneles dentro de la carpeta (el orden de la jerarquía en Unity).
/// Se guarda en PlayerPrefs, así que también sobrevive si cerrás y abrís el juego.
/// </summary>
public static class SelectedMission
{
    private const string Key = "SelectedMissionIndex";

    public const int None = -1;

    /// <summary>Posición de la misión elegida, empezando en 0. -1 si no hay ninguna.</summary>
    public static int Index
    {
        get => PlayerPrefs.GetInt(Key, None);
        set
        {
            PlayerPrefs.SetInt(Key, value);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Número de la misión elegida, empezando en 1. 0 si no hay ninguna.</summary>
    public static int Number => Index + 1;

    public static bool HasSelection => Index >= 0;

    /// <summary>Borra la selección guardada.</summary>
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
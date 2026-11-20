using TMPro;
using UnityEngine;

public class SpeedrunManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject ContenedorTimer;      // objeto que contiene el texto (se oculta si no hay speedrun)
    public TextMeshProUGUI TextoTimer;

    [Header("Estado (no tocar)")]
    public bool TimerCorriendo = false;
    public float TiempoActual = 0f;

    private bool speedrunActivo = false;

    private void Start()
    {
        speedrunActivo = ConfigurationVariables.Instance.SpeedrunMode;

        if (ContenedorTimer != null)
        {
            ContenedorTimer.SetActive(speedrunActivo);
        }

        ActualizarTexto();
    }

    private void Update()
    {
        if (!speedrunActivo || !TimerCorriendo)
        {
            return;
        }

        TiempoActual += Time.deltaTime;
        ActualizarTexto();
    }

    // Se llama al presionar F para empezar la misión
    public void IniciarTimer()
    {
        if (!speedrunActivo)
        {
            return;
        }

        TimerCorriendo = true;
    }

    // Se llama cuando aparece el panel de victoria
    public void PausarTimer()
    {
        TimerCorriendo = false;
        ActualizarTexto(); // deja el valor final en pantalla
    }

    public void ReiniciarTimer()
    {
        TimerCorriendo = false;
        TiempoActual = 0f;
        ActualizarTexto();
    }

    private void ActualizarTexto()
    {
        if (TextoTimer == null)
        {
            return;
        }

        int totalMs = Mathf.FloorToInt(TiempoActual * 1000f);
        int minutos = totalMs / 60000;
        int segundos = (totalMs / 1000) % 60;
        int milis = totalMs % 1000;

        TextoTimer.text = string.Format("{0:00}:{1:00}.{2:000}", minutos, segundos, milis);
    }
}
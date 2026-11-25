using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpeedrunManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject ContenedorTimer;      // objeto que contiene el texto (se oculta si no hay speedrun)
    public TextMeshProUGUI TextoTimer;

    [Header("Controles")]
    public KeyCode TeclaReinicio = KeyCode.R;

    [Header("Estado (no tocar)")]
    public bool TimerCorriendo = false;
    public float TiempoActual = 0f;

    private bool speedrunActivo = false;
    private MissionBriefing missionBriefing;   // referencia automática

    private void Start()
    {
        speedrunActivo = ConfigurationVariables.Instance.SpeedrunMode;
        missionBriefing = FindAnyObjectByType<MissionBriefing>();

        if (ContenedorTimer != null)
        {
            ContenedorTimer.SetActive(speedrunActivo);
        }

        ActualizarTexto();
    }

    private void Update()
    {
        if (!speedrunActivo)
        {
            return;
        }

        // Reinicio rápido: solo si el briefing ya terminó y la misión empezó (ya se tocó F)
        if (Input.GetKeyDown(TeclaReinicio)
            && missionBriefing != null
            && missionBriefing.MisionIniciada)
        {
            ReiniciarNivel();
            return;
        }

        if (!TimerCorriendo)
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

    // Recarga la escena actual (el timer arranca de cero porque se recrea el manager)
    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
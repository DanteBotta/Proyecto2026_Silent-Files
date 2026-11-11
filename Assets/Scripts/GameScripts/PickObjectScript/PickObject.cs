using UnityEngine;
using UnityEngine.UI;

public class PickObject : MonoBehaviour
{
    public ShowCanvasNearPlayer ShowCanvasNearPlayer;
    public ObjetivoManager ObjetivoManager;

    // Imagen que se va a completar
    public Image ImageFilled;

    float fillAmount = 0;
    bool completado = false;

    private void Start()
    {
        ShowCanvasNearPlayer = GetComponentInChildren<ShowCanvasNearPlayer>(true);
        ObjetivoManager = FindAnyObjectByType<ObjetivoManager>();
        ImageFilled = transform.Find("CanvasObjetivo1/Fill-Image").GetComponent<Image>();
        ImageFilled.fillAmount = 0;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.F) && ShowCanvasNearPlayer.FuncionCanvas)
        {
            fillAmount = Mathf.Clamp01(
                fillAmount + Time.deltaTime
            );

            ImageFilled.fillAmount = fillAmount;
        }
        else
        {
            fillAmount = 0f;
            ImageFilled.fillAmount = 0f;
        }

        if (fillAmount >= 1f && !completado)
        {
            completado = true;
            ObjetivoManager.CantidadObjetivosCompletados++;
            ObjetivoManager.StartCoroutine(ObjetivoManager.MostrarCantObjetivos());
            Destroy(gameObject);
        }
    }
}
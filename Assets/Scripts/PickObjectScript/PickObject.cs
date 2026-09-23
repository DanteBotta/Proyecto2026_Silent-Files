using UnityEngine;
using UnityEngine.UI;

public class PickObject : MonoBehaviour
{
    // Imagen que se va a completar
    public Image ImageFilled;

    // Objeto que corresponde la función de agarrar
    public GameObject ObjetoSeleccionado;

    float fillAmount = 0;
    bool completado = false;

    private void Start()
    {
        ImageFilled.fillAmount = 0;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            fillAmount = Mathf.Clamp01(fillAmount + Time.deltaTime);
            ImageFilled.fillAmount = fillAmount;
        }

        if (fillAmount >= 1f && !completado)
        {
            completado = true;
            Destroy(gameObject);
            Destroy(ObjetoSeleccionado);
        }
    }
}
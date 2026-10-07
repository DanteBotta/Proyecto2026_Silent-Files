using UnityEngine;
using UnityEngine.UI;

public class ExitMisionCar : MonoBehaviour
{
    // Imagen que se va a completar
    public Image ImageFilled;

    public bool MisionTerminada = false;

    float fillAmount = 0;
    bool completado = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ImageFilled.fillAmount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.F) && MisionTerminada)
        {
            fillAmount = Mathf.Clamp01(fillAmount + Time.deltaTime);
            ImageFilled.fillAmount = fillAmount;
        }

        if (fillAmount >= 1f && !completado)
        {
            completado = true;
            Destroy(gameObject);
        }
    }
}

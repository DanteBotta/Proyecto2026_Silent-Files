using System.Collections;
using TMPro;
using UnityEngine;

public class ObjetivoManager : MonoBehaviour
{
    public ExitMisionCar ExitMisionCar;

    [Header("Objetivos (se autocompleta)")]
    public PickObject[] PickObjects;

    [Header("Textos")]
    public TextMeshProUGUI TextoObjetivos;
    public float TiempoMostrarTexto = 1.5f;


    int CantidadTotalObjetivos => PickObjects.Length;
    public int CantidadObjetivosCompletados;

    private bool mostrandoTexto = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ExitMisionCar = FindAnyObjectByType<ExitMisionCar>();
        PickObjects = FindObjectsByType<PickObject>();
        TextoObjetivos.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (CantidadObjetivosCompletados == CantidadTotalObjetivos)
        {
            ExitMisionCar.MisionTerminada = true;
        }
    }

    public IEnumerator MostrarCantObjetivos()
    {
        mostrandoTexto = true;

        TextoObjetivos.enabled = true;
        TextoObjetivos.text = CantidadObjetivosCompletados + " / " + CantidadTotalObjetivos + " documentos recuperados";

        yield return new WaitForSeconds(TiempoMostrarTexto);

        TextoObjetivos.enabled = false;
        mostrandoTexto = false;
    }
}

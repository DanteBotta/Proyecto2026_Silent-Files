using UnityEngine;

public class MinimapaManager : MonoBehaviour
{
    [Header("Prefabs de los indicadores")]
    public GameObject IndicadorPlayer;
    public GameObject IndicadorEnemy;
    public GameObject IndicadorObjetivo;
    public GameObject IndicadorAuto;

    [Header("Altura de los indicadores")]
    public float AlturaIndicadores = 10f;

    void Start()
    {
        CrearIndicadores("Player", IndicadorPlayer);
        CrearIndicadores("Enemy", IndicadorEnemy);
        CrearIndicadores("Objectives", IndicadorObjetivo);
        CrearIndicadores("Car", IndicadorAuto);

        AsignarSeguimiento();
    }

    void CrearIndicadores(string tagObjeto, GameObject prefab)
    {
        GameObject[] objetos = GameObject.FindGameObjectsWithTag(tagObjeto);

        foreach (GameObject objeto in objetos)
        {
            GameObject indicador = Instantiate(prefab);

            indicador.tag = "Indicator";

            indicador.transform.position = objeto.transform.position;

            FollowObject seguimiento = indicador.AddComponent<FollowObject>();
            seguimiento.objeto = objeto.transform;
            seguimiento.manager = this;
        }
    }

    void AsignarSeguimiento()
    {
        GameObject[] indicadores = GameObject.FindGameObjectsWithTag("Indicator");

        foreach (GameObject indicador in indicadores)
        {
            FollowObject seguimiento = indicador.GetComponent<FollowObject>();

            if (seguimiento == null)
            {
                seguimiento = indicador.AddComponent<FollowObject>();
            }

            seguimiento.manager = this;
        }
    }
}
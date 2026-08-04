using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detecta las paredes/objetos que quedan entre la cámara y el jugador y las vuelve
/// semi-transparentes mientras bloqueen la vista. Cuando dejan de bloquear, vuelven
/// a ser opacas de forma gradual.
///
/// IMPORTANTE - Requisito de materiales:
/// Para que el efecto de transparencia se vea, los materiales de las paredes deben
/// tener su "Rendering Mode" configurado en "Fade" o "Transparent" (no "Opaque").
/// Esto se cambia en el Inspector del material, con el shader Standard.
/// Si el material queda en "Opaque", Unity va a ignorar el valor de alpha y la
/// pared no se va a ver transparente aunque el script funcione correctamente.
///
/// Requisito de capas (Layers):
/// Las paredes que quieras que se vuelvan transparentes deben estar en una Layer
/// específica (por ejemplo, crear una layer llamada "Wall") y asignar esa layer
/// en el campo "Obstacle Layers" de este script en el Inspector.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraOcclusionFade : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform del jugador (el objetivo que la cámara no debe perder de vista).")]
    public Transform target;
 
    [Tooltip("Offset en altura sobre el jugador, para apuntar a la altura del pecho/cabeza en vez de los pies.")]
    public Vector3 targetOffset = new Vector3(0f, 1f, 0f);
 
    [Header("Detección de obstáculos")]
    [Tooltip("Capas que se consideran 'pared' y pueden volverse transparentes. Configurar en Project Settings > Tags and Layers.")]
    public LayerMask obstacleLayers;
 
    [Tooltip("Radio del chequeo (SphereCast). Ayuda a detectar paredes finas que un Raycast simple podría atravesar sin tocar.")]
    public float checkRadius = 0.3f;
 
    [Header("Transparencia")]
    [Tooltip("Alpha (transparencia) que tendrá la pared cuando bloquee la vista. 0 = invisible, 1 = opaco.")]
    [Range(0f, 1f)]
    public float fadedAlpha = 0.25f;
 
    [Tooltip("Velocidad de la transición entre opaco y transparente.")]
    public float fadeSpeed = 6f;
 
    // Guarda, para cada Renderer afectado, su alpha actual y su MaterialPropertyBlock
    private class FadeInfo
    {
        public float currentAlpha = 1f;
        public MaterialPropertyBlock block;
    }
 
    private Dictionary<Renderer, FadeInfo> fadeData = new Dictionary<Renderer, FadeInfo>();
    private List<Renderer> currentlyBlocking = new List<Renderer>();
    private List<Renderer> keysToRemove = new List<Renderer>();
 
    private void LateUpdate()
    {
        if (target == null) return;
 
        DetectObstacles();
        UpdateFades();
    }
 
    private void DetectObstacles()
    {
        currentlyBlocking.Clear();
 
        Vector3 origin = transform.position;
        Vector3 destination = target.position + targetOffset;
        Vector3 direction = destination - origin;
        float distance = direction.magnitude;
 
        if (distance <= 0.01f) return;
        direction.Normalize();
 
        RaycastHit[] hits = Physics.SphereCastAll(origin, checkRadius, direction, distance, obstacleLayers);
 
        for (int i = 0; i < hits.Length; i++)
        {
            Renderer rend = hits[i].collider.GetComponent<Renderer>();
            if (rend == null) continue;
 
            currentlyBlocking.Add(rend);
 
            if (!fadeData.ContainsKey(rend))
            {
                FadeInfo info = new FadeInfo();
                info.currentAlpha = 1f;
                info.block = new MaterialPropertyBlock();
                fadeData.Add(rend, info);
            }
        }
    }
 
    private void UpdateFades()
    {
        keysToRemove.Clear();
 
        foreach (KeyValuePair<Renderer, FadeInfo> pair in fadeData)
        {
            Renderer rend = pair.Key;
            FadeInfo info = pair.Value;
 
            if (rend == null)
            {
                keysToRemove.Add(rend);
                continue;
            }
 
            bool isBlocking = currentlyBlocking.Contains(rend);
            float targetAlpha = isBlocking ? fadedAlpha : 1f;
 
            info.currentAlpha = Mathf.MoveTowards(info.currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
 
            ApplyAlpha(rend, info);
 
            // Si ya volvió a estar completamente opaco y no está bloqueando, se deja de trackear
            if (!isBlocking && info.currentAlpha >= 0.99f)
            {
                keysToRemove.Add(rend);
            }
        }
 
        for (int i = 0; i < keysToRemove.Count; i++)
        {
            if (fadeData.ContainsKey(keysToRemove[i]))
            {
                fadeData.Remove(keysToRemove[i]);
            }
        }
    }
 
    private void ApplyAlpha(Renderer rend, FadeInfo info)
    {
        rend.GetPropertyBlock(info.block);
 
        Color color = rend.sharedMaterial.HasProperty("_Color") ? rend.sharedMaterial.color : Color.white;
        color.a = info.currentAlpha;
 
        info.block.SetColor("_Color", color);
        rend.SetPropertyBlock(info.block);
    }
}

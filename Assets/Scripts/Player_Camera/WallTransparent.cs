using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallTransparent : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Transparencia")]
    [Range(0f, 1f)]
    public float hiddenAlpha = 0.15f;
    public float fadeSpeed   = 8f;

    [Header("Layer de paredes")]
    public LayerMask wallLayer;

    // CAMBIO: new() → new Dictionary<>() / new HashSet<>() explícito
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private HashSet<Renderer> currentlyHidden = new HashSet<Renderer>();
    private HashSet<Renderer> hitThisFrame    = new HashSet<Renderer>();

    void Update()
    {
        CheckWalls();
    }

    void CheckWalls()
    {
        hitThisFrame.Clear();

        Vector3 direction = player.position - transform.position;
        float   distance  = direction.magnitude;

        RaycastHit[] hits = Physics.RaycastAll(
            transform.position,
            direction.normalized,
            distance,
            wallLayer
        );

        foreach (RaycastHit hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend == null) continue;

            hitThisFrame.Add(rend);

            if (!originalMaterials.ContainsKey(rend))
                SaveMaterials(rend);

            SetTransparent(rend, true);
            currentlyHidden.Add(rend);
        }

        // CAMBIO: new() → new List<Renderer>() explícito
        List<Renderer> toRestore = new List<Renderer>();
        foreach (Renderer rend in currentlyHidden)
        {
            if (!hitThisFrame.Contains(rend))
                toRestore.Add(rend);
        }

        foreach (Renderer rend in toRestore)
        {
            SetTransparent(rend, false);
            currentlyHidden.Remove(rend);
        }
    }

    void SaveMaterials(Renderer rend)
    {
        Material[] originals = new Material[rend.materials.Length];
        for (int i = 0; i < rend.materials.Length; i++)
        {
            originals[i] = new Material(rend.materials[i]);
        }
        originalMaterials[rend] = originals;
    }

    void SetTransparent(Renderer rend, bool transparent)
    {
        foreach (Material mat in rend.materials)
        {
            if (transparent)
            {
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;

                Color c = mat.color;
                c.a = Mathf.Lerp(c.a, hiddenAlpha, fadeSpeed * Time.deltaTime);
                mat.color = c;
            }
            else
            {
                if (originalMaterials.ContainsKey(rend))
                {
                    rend.materials = originalMaterials[rend];
                    originalMaterials.Remove(rend);
                }
            }
        }
    }
}

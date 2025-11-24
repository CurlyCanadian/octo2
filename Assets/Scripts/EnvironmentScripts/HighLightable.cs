using UnityEngine;

//
// Highlightable.cs
// slap this on anything that wants to glow when hovered
//
// no custom shaders, just uses emission on whatever material it already has
// if emission isn't enabled on your material, script will try anyway
// (URP Lit / Standard both support emission)
//

[DisallowMultipleComponent]
public class Highlightable : MonoBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] private Color glowColor = Color.white;
    [SerializeField] [Range(0f, 5f)] private float glowIntensity = 1.5f;

    private Renderer[] renderers;
    private MaterialPropertyBlock mpb;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    public void SetHighlight(bool on)
    {
        Color final = on ? glowColor * glowIntensity : Color.black;

        foreach (var r in renderers)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionColorID, final);
            r.SetPropertyBlock(mpb);
        }
    }
}

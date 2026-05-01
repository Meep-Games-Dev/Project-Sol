using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FogDecalController : MonoBehaviour
{
    public Material fogMaterial;
    public float blendSpeed = 1f;
    public int textureScale = 1;
    public RenderTexture fogSource;

    private RenderTexture prevTexture;
    private RenderTexture currTexture;
    private DecalProjector decalProjector;
    private Material instantiatedMaterial;
    private float blendAmount;

    private void Awake()
    {
        decalProjector = GetComponent<DecalProjector>();

        prevTexture = GenerateTexture();
        currTexture = GenerateTexture();

        instantiatedMaterial = new Material(fogMaterial);
        decalProjector.material = instantiatedMaterial;

        instantiatedMaterial.SetTexture("_PrevTexture", prevTexture);
        instantiatedMaterial.SetTexture("_CurrTexture", currTexture);

        StartNewBlend();
    }
    private void Update()
    {
        if (blendAmount >= 1f)
        {
            StartNewBlend();
        }
    }
    RenderTexture GenerateTexture()
    {
        RenderTexture rt = new RenderTexture(
            fogSource.width * textureScale,
            fogSource.height * textureScale,
            0,
            fogSource.format)
        { filterMode = FilterMode.Bilinear };
        return rt;
    }

    public void StartNewBlend()
    {
        blendAmount = 0;
        Graphics.Blit(currTexture, prevTexture);
        Graphics.Blit(fogSource, currTexture);

        StopAllCoroutines();
        StartCoroutine(BlendFog());
    }

    IEnumerator BlendFog()
    {
        while (blendAmount < 1)
        {
            blendAmount += Time.deltaTime * blendSpeed;
            instantiatedMaterial.SetFloat("_Blend", blendAmount);
            yield return null;
        }
    }
}
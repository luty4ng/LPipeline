using UnityEngine;

[DisallowMultipleComponent]
// set material instance in real time
public class PerObjectMaterialProperties : MonoBehaviour
{
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    static int cutoffId = Shader.PropertyToID("_Cutoff");
    static int metallicId = Shader.PropertyToID("_Metallic");
	static int smoothnessId = Shader.PropertyToID("_Smoothness");
    static MaterialPropertyBlock material;

    [SerializeField]
    Color baseColor = Color.white;

    [SerializeField, Range(0f, 1f)]
    float alphaCutoff = 0.5f, metallic = 0f, smoothness = 0.5f;

    void Awake()
    {
        OnValidate();
    }

    void OnValidate()
    {
        if (material == null)
        {
            material = new MaterialPropertyBlock();
        }
        material.SetColor(baseColorId, baseColor);
        material.SetFloat(cutoffId, alphaCutoff);
        material.SetFloat(metallicId, metallic);
		material.SetFloat(smoothnessId, smoothness);
        GetComponent<Renderer>().SetPropertyBlock(material);
    }
}
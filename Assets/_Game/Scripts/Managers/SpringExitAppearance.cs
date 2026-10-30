using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class SpringExitAppearance : MonoBehaviour
{
    [SerializeField] Transform model;
    static readonly int TireTracksEnabled = Shader.PropertyToID("_TireTracksEnabled");

    void OnEnable()
    {
        var properties = new MaterialPropertyBlock();
        Transform target = model != null ? model : transform;
        foreach (MeshRenderer renderer in target.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.GetPropertyBlock(properties);



            properties.SetFloat(TireTracksEnabled, 0f);
            renderer.SetPropertyBlock(properties);
            properties.Clear();
        }
    }
}

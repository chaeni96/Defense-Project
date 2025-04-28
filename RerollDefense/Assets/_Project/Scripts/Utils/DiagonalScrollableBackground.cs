using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class DiagonalScrollableBackground : MonoBehaviour
{
    [Header("columns(Garo) tile count")]
    [Min(1)] public int columns = 12;

    [Tooltip("row(Sero) tile count")]
    [Min(1)] public int rows = 12;

    [Header("ScrollSpeed")]
    public float speed = 3f;

    private RawImage rawImage;
    private Material instanceMat;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();

        instanceMat = Instantiate(rawImage.material);
        rawImage.material = instanceMat;

        instanceMat.SetInt("_Columns", columns);
        instanceMat.SetInt("_Rows", rows);
        instanceMat.SetFloat("_Speed", speed);
    }

    void Update()
    {
        instanceMat.SetFloat("_Speed", speed);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (instanceMat != null)
        {
            instanceMat.SetInt("_Columns", columns);
            instanceMat.SetInt("_Rows", rows);
            instanceMat.SetFloat("_Speed", speed);
        }
    }
#endif
}

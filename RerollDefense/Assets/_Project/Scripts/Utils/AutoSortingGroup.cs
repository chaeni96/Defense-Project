using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SortingGroup))]
public class AutoSortingGroup : MonoBehaviour
{
    [SerializeField] private float offsetZ;
    void LateUpdate()
    {
        float worldY = transform.position.y;
        Vector3 lp = transform.localPosition;
        lp.z = worldY + offsetZ;
        transform.localPosition = lp;
    }
}

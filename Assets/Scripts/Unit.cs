using UnityEngine;

/// <summary>
/// Simple component to mark an object as a selectable unit (apartment).
/// Attach this to each apartment/unit in the building.
/// </summary>
public class Unit : MonoBehaviour
{
    [Header("Unit Info")]
    [Tooltip("Opsiyonel: Unit'in adı veya numarası")]
    public string unitName;
    
    [Tooltip("Opsiyonel: Kameranın bu unit'e bakış noktası. Boşsa transform.position kullanılır.")]
    public Transform focusPoint;

    /// <summary>
    /// Kameranın bakacağı nokta
    /// </summary>
    public Vector3 GetFocusPosition()
    {
        if (focusPoint != null)
            return focusPoint.position;
        return transform.position;
    }

    void OnDrawGizmosSelected()
    {
        // Focus noktasını göster
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetFocusPosition(), 0.5f);
        
        if (focusPoint != null)
        {
            Gizmos.DrawLine(transform.position, focusPoint.position);
        }
    }
}

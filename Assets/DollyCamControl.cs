using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Dolly kamera kontrolcüsü. CameraController tarafından tetiklenir.
/// </summary>
public class DollyCamControl : MonoBehaviour
{
    [Header("Spline Ayarları")]
    public CinemachineSplineCart splineCart;
    public float speed = 0.2f;

    [Header("Durum")]
    public float progress = 0f;
    public bool isMoving = false;

    /// <summary>
    /// Spline hareketi başlat (CameraController tarafından çağrılır)
    /// </summary>
    public void MoveAlongPathParent()
    {
        if (isMoving) return;
        progress = 0f;
        StartCoroutine(MoveAlongPath());
    }

    public IEnumerator MoveAlongPath()
    {
        isMoving = true;
        
        while (progress < 1f)
        {
            progress += speed * Time.deltaTime;
            progress = Mathf.Clamp01(progress);
            if (splineCart != null)
                splineCart.SplinePosition = progress;
            yield return null;
        }
        
        isMoving = false;
        Debug.Log("[DollyCamControl] Spline animation completed");
    }

    /// <summary>
    /// Spline'ı başa al
    /// </summary>
    public void ResetSpline()
    {
        progress = 0f;
        if (splineCart != null)
            splineCart.SplinePosition = 0f;
    }
}

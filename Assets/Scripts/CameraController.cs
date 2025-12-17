using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections;

/// <summary>
/// Merkezi kamera yönetim sistemi.
/// Tüm kamera modlarını (Building, Dolly, FPS) tek bir yerden yönetir.
/// </summary>
public class CameraController : MonoBehaviour
{
    public enum CameraMode
    {
        Building,      // Bina genel görünüm
        UnitFocus,     // Unit'e odaklı
        UnitInterior,  // Unit içi
        Dolly,         // Spline animasyonu
        FPS            // Serbest hareket
    }

    [Header("Cameras")]
    [Tooltip("Bina genel görünümü kamerası")]
    public CinemachineCamera buildingCamera;
    
    [Tooltip("Unit'e odaklı kamera")]
    public CinemachineCamera unitFocusCamera;
    
    [Tooltip("Unit içi kamera")]
    public CinemachineCamera unitInteriorCamera;
    
    [Tooltip("Dolly/Spline kamerası")]
    public CinemachineCamera dollyCamera;
    
    [Tooltip("FPS kamerası")]
    public CinemachineCamera fpsCamera;

    [Header("Controllers")]
    [Tooltip("BuildingFocusController referansı")]
    public BuildingFocusController buildingFocusController;
    
    [Tooltip("DollyCamControl referansı")]
    public DollyCamControl dollyCamControl;
    
    [Tooltip("SimpleFPSController referansı")]
    public SimpleFPSController fpsController;

    [Header("Camera Brain")]
    public CinemachineBrain brain;
    public float blendDuration = 1f;
    public CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut;

    [Header("Priority")]
    public int activePriority = 20;
    public int inactivePriority = 0;

    [Header("State")]
    [SerializeField]
    private CameraMode currentMode = CameraMode.Building;
    
    public CameraMode CurrentMode => currentMode;

    void Start()
    {
        // Referansları otomatik bul
        if (buildingFocusController == null)
            buildingFocusController = FindObjectOfType<BuildingFocusController>();
        if (dollyCamControl == null)
            dollyCamControl = FindObjectOfType<DollyCamControl>();
        if (fpsController == null)
            fpsController = FindObjectOfType<SimpleFPSController>();
        if (brain == null)
            brain = FindObjectOfType<CinemachineBrain>();

        // Başlangıç modu
        SetMode(CameraMode.Building);
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (Keyboard.current == null) return;

        // 1 - Building Mode
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            SetMode(CameraMode.Building);
        }
        // 2 - Dolly Mode
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            SetMode(CameraMode.Dolly);
        }
        // 3 - FPS Mode
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            SetMode(CameraMode.FPS);
        }
        // Escape - Building'e dön
        else if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (currentMode != CameraMode.Building)
            {
                SetMode(CameraMode.Building);
            }
        }
        // Space - Dolly modunda spline'ı başlat
        else if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (currentMode == CameraMode.Dolly && dollyCamControl != null)
            {
                dollyCamControl.MoveAlongPathParent();
            }
        }
    }

    /// <summary>
    /// Kamera modunu değiştir
    /// </summary>
    [ContextMenu("Set Mode")]
    public void SetMode(CameraMode mode)
    {
        // Önceki modu temizle
        CleanupCurrentMode();

        currentMode = mode;

        // Blend ayarı
        if (brain != null)
        {
            brain.DefaultBlend = new CinemachineBlendDefinition(blendStyle, blendDuration);
        }

        // Tüm kameraları deaktif yap
        SetAllCamerasPriority(inactivePriority);

        // Yeni modu aktifle
        switch (mode)
        {
            case CameraMode.Building:
                ActivateBuildingMode();
                break;
            case CameraMode.UnitFocus:
                ActivateUnitFocusMode();
                break;
            case CameraMode.UnitInterior:
                ActivateUnitInteriorMode();
                break;
            case CameraMode.Dolly:
                ActivateDollyMode();
                break;
            case CameraMode.FPS:
                ActivateFPSMode();
                break;
        }

        Debug.Log($"[CameraController] Mode changed to: {mode}");
    }

    void CleanupCurrentMode()
    {
        // FPS modundan çıkış
        if (currentMode == CameraMode.FPS && fpsController != null)
        {
            fpsController.DeactivateFPS();
        }

        // Mouse'u serbest bırak
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void SetAllCamerasPriority(int priority)
    {
        if (buildingCamera != null) buildingCamera.Priority = priority;
        if (unitFocusCamera != null) unitFocusCamera.Priority = priority;
        if (unitInteriorCamera != null) unitInteriorCamera.Priority = priority;
        if (dollyCamera != null) dollyCamera.Priority = priority;
        if (fpsCamera != null) fpsCamera.Priority = priority;
    }

    void ActivateBuildingMode()
    {
        if (buildingCamera != null)
            buildingCamera.Priority = activePriority;
    }

    void ActivateUnitFocusMode()
    {
        if (unitFocusCamera != null)
            unitFocusCamera.Priority = activePriority;
    }

    void ActivateUnitInteriorMode()
    {
        if (unitInteriorCamera != null)
            unitInteriorCamera.Priority = activePriority;
    }

    void ActivateDollyMode()
    {
        if (dollyCamera != null)
            dollyCamera.Priority = activePriority;
        
        // Dolly'yi başlangıca al
        if (dollyCamControl != null)
        {
            dollyCamControl.progress = 0f;
            if (dollyCamControl.splineCart != null)
                dollyCamControl.splineCart.SplinePosition = 0f;
        }
    }

    void ActivateFPSMode()
    {
        if (fpsCamera != null)
            fpsCamera.Priority = activePriority;
        
        if (fpsController != null)
        {
            fpsController.ActivateFPS();
        }
    }

    // BuildingFocusController'dan çağrılacak public metodlar
    public void FocusOnUnit()
    {
        SetMode(CameraMode.UnitFocus);
    }

    public void EnterUnit()
    {
        SetMode(CameraMode.UnitInterior);
    }

    public void ExitToBuilding()
    {
        SetMode(CameraMode.Building);
    }
}

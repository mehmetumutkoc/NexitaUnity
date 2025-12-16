using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// Merkezi bina focus kontrolcüsü.
/// Unit'lere tıklayarak kamerayı o unit'e odaklar.
/// </summary>
public class BuildingFocusController : MonoBehaviour
{
    public enum FocusMode
    {
        BuildingView,      // Bina genel görünüm
        UnitFocus,         // Dışarıdan unit'e odaklı
        UnitInterior       // Unit içi
    }

    [Header("Cameras")]
    [Tooltip("Bina genel görünümü kamerası")]
    public CinemachineCamera buildingCamera;
    
    [Tooltip("Unit'e odaklı kamera (dinamik hedef)")]
    public CinemachineCamera unitFocusCamera;
    
    [Tooltip("Unit içi kamera (dinamik hedef)")]
    public CinemachineCamera unitInteriorCamera;

    [Header("Camera Settings")]
    [Tooltip("Main Camera üzerindeki CinemachineBrain")]
    public CinemachineBrain brain;
    
    [Tooltip("Kameralar arası geçiş süresi")]
    public float blendDuration = 2f;
    
    [Tooltip("Blend eğrisi")]
    public CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut;

    [Header("Priority")]
    public int activePriority = 20;
    public int inactivePriority = 0;

    [Header("Input")]
    [Tooltip("Unit seçmek için kullanılacak fare tuşu")]
    public int mouseButton = 0;
    
    [Tooltip("Hangi layer'lar unit olarak kabul edilecek")]
    public LayerMask unitLayerMask = ~0;

    [Header("State")]
    [SerializeField]
    private FocusMode currentMode = FocusMode.BuildingView;
    
    [SerializeField]
    private Unit selectedUnit;
    
    private FocusMode lastAppliedMode = FocusMode.BuildingView;

    public FocusMode CurrentMode => currentMode;
    public Unit SelectedUnit => selectedUnit;

    void Start()
    {
        SetMode(FocusMode.BuildingView);
    }

    void Update()
    {
        // Inspector değişikliği kontrolü
        if (currentMode != lastAppliedMode)
        {
            SetMode(currentMode);
        }

        // Tıklama ile unit seçimi
        HandleUnitSelection();
    }

    void HandleUnitSelection()
    {
        // New Input System kullan
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        
        if (mouse == null)
            return;

        // Building modda değilsek, ESC ile geri dön
        if (currentMode != FocusMode.BuildingView)
        {
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                ExitToBuilding();
            }
            return;
        }

        // Sol tık kontrolü
        if (!mouse.leftButton.wasPressedThisFrame)
            return;

        Debug.Log("[BuildingFocusController] Mouse clicked, casting ray...");

        Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, unitLayerMask))
        {
            Debug.Log($"[BuildingFocusController] Raycast hit: {hit.collider.gameObject.name}");
            
            // Hit edilen objede veya parent'larında Unit component'i var mı?
            Unit unit = hit.collider.GetComponentInParent<Unit>();
            if (unit != null)
            {
                FocusOnUnit(unit);
            }
            else
            {
                Debug.Log($"[BuildingFocusController] No Unit component found on {hit.collider.gameObject.name} or its parents!");
            }
        }
        else
        {
            Debug.Log("[BuildingFocusController] Raycast didn't hit anything!");
        }
    }

    /// <summary>
    /// Belirli bir unit'e odaklan
    /// </summary>
    public void FocusOnUnit(Unit unit)
    {
        if (unit == null)
            return;

        selectedUnit = unit;
        
        // Kamera hedefini ayarla
        UpdateCameraTarget(unit);
        
        SetMode(FocusMode.UnitFocus);
        
        Debug.Log($"[BuildingFocusController] Focused on unit: {unit.unitName ?? unit.gameObject.name}");
    }

    /// <summary>
    /// Seçili unit'in içine gir
    /// </summary>
    [ContextMenu("Enter Unit")]
    public void EnterUnit()
    {
        if (selectedUnit == null)
        {
            Debug.LogWarning("[BuildingFocusController] No unit selected!");
            return;
        }

        UpdateCameraTarget(selectedUnit);
        SetMode(FocusMode.UnitInterior);
    }

    /// <summary>
    /// Unit'ten çık, dışarıdan bakışa dön
    /// </summary>
    [ContextMenu("Exit To Focus")]
    public void ExitToFocus()
    {
        if (selectedUnit != null)
        {
            SetMode(FocusMode.UnitFocus);
        }
        else
        {
            ExitToBuilding();
        }
    }

    /// <summary>
    /// Bina görünümüne dön
    /// </summary>
    [ContextMenu("Exit To Building")]
    public void ExitToBuilding()
    {
        selectedUnit = null;
        SetMode(FocusMode.BuildingView);
    }

    [Header("Auto Framing")]
    [Tooltip("Kamera FOV (derece)")]
    public float cameraFOV = 60f;
    
    [Tooltip("Unit etrafında ekstra margin (çarpan)")]
    public float framingMargin = 1.2f;
    
    [Tooltip("Minimum kamera mesafesi")]
    public float minCameraDistance = 3f;
    
    [Tooltip("Kamera yükseklik açısı (derece, 0=yatay, 90=yukarıdan)")]
    public float cameraElevationAngle = 20f;

    void UpdateCameraTarget(Unit unit)
    {
        Transform focusTarget = unit.focusPoint != null ? unit.focusPoint : unit.transform;
        
        // Unit'in bounds'unu hesapla
        Bounds bounds = CalculateUnitBounds(unit.gameObject);
        Vector3 center = bounds.center;
        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        
        // Kamera mesafesini hesapla (unit'i tam frame'e sığdırmak için)
        float distance = CalculateCameraDistance(size);
        
        // Kamera pozisyonunu hesapla
        Vector3 cameraDirection = CalculateCameraDirection(center);
        Vector3 cameraPosition = center + cameraDirection * distance;
        
        Debug.Log($"[BuildingFocusController] Unit bounds size: {size}, Camera distance: {distance}, Position: {cameraPosition}");
        
        // Unit Focus kamera - Pozisyonu ve hedefi ayarla
        if (unitFocusCamera != null)
        {
            unitFocusCamera.transform.position = cameraPosition;
            unitFocusCamera.transform.LookAt(center);
            unitFocusCamera.Follow = null; // Manuel pozisyon
            unitFocusCamera.LookAt = focusTarget;
        }

        // Interior kamera - Unit'in merkezine yerleştir
        if (unitInteriorCamera != null)
        {
            unitInteriorCamera.transform.position = center + Vector3.up * 1.6f;
            unitInteriorCamera.Follow = null;
            unitInteriorCamera.LookAt = null;
        }
    }
    
    Bounds CalculateUnitBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            // Renderer yoksa collider'a bak
            Collider col = obj.GetComponentInChildren<Collider>();
            if (col != null)
                return col.bounds;
            
            // Hiçbiri yoksa varsayılan bounds
            return new Bounds(obj.transform.position, Vector3.one * 2f);
        }
        
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        
        return bounds;
    }
    
    float CalculateCameraDistance(float objectSize)
    {
        // FOV'a göre mesafeyi hesapla
        float halfFOV = cameraFOV * 0.5f * Mathf.Deg2Rad;
        float distance = (objectSize * framingMargin) / (2f * Mathf.Tan(halfFOV));
        
        return Mathf.Max(distance, minCameraDistance);
    }
    
    Vector3 CalculateCameraDirection(Vector3 targetCenter)
    {
        // Mevcut kameradan hedefe yön (yatay)
        Vector3 camPos = Camera.main.transform.position;
        Vector3 horizontalDir = (camPos - targetCenter);
        horizontalDir.y = 0;
        
        if (horizontalDir.sqrMagnitude < 0.01f)
            horizontalDir = -Vector3.forward;
        
        horizontalDir.Normalize();
        
        // Elevation açısı ekle
        float elevationRad = cameraElevationAngle * Mathf.Deg2Rad;
        Vector3 finalDir = horizontalDir * Mathf.Cos(elevationRad) + Vector3.up * Mathf.Sin(elevationRad);
        
        return finalDir.normalized;
    }

    public void SetMode(FocusMode mode)
    {
        currentMode = mode;
        lastAppliedMode = mode;

        // Blend ayarını güncelle
        if (brain != null)
        {
            brain.DefaultBlend = new CinemachineBlendDefinition(blendStyle, blendDuration);
        }

        // Tüm kameraları düşük priority yap
        if (buildingCamera != null)
            buildingCamera.Priority = inactivePriority;
        if (unitFocusCamera != null)
            unitFocusCamera.Priority = inactivePriority;
        if (unitInteriorCamera != null)
            unitInteriorCamera.Priority = inactivePriority;

        // Seçili kamerayı yüksek priority yap
        switch (mode)
        {
            case FocusMode.BuildingView:
                if (buildingCamera != null)
                    buildingCamera.Priority = activePriority;
                break;

            case FocusMode.UnitFocus:
                if (unitFocusCamera != null)
                    unitFocusCamera.Priority = activePriority;
                break;

            case FocusMode.UnitInterior:
                if (unitInteriorCamera != null)
                    unitInteriorCamera.Priority = activePriority;
                break;
        }

        Debug.Log($"[BuildingFocusController] Mode: {mode}");
    }

    void OnValidate()
    {
        if (Application.isPlaying && currentMode != lastAppliedMode)
        {
            SetMode(currentMode);
        }
    }
}

using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 3-Stage Camera Focus Controller using Cinemachine.
/// Modes: Building View → External Focus → Interior
/// </summary>
public class ApartmentFocusController : MonoBehaviour
{
    public enum FocusMode
    {
        BuildingView,      // Bina genel görünüm
        ExternalFocus,     // Dışarıdan daire odaklı
        Interior           // Daire içi
    }

    [Header("Virtual Cameras")]
    [Tooltip("Bina genel görünümü kamerası")]
    public CinemachineCamera buildingCamera;
    
    [Tooltip("Dışarıdan daire odaklı kamera")]
    public CinemachineCamera externalFocusCamera;
    
    [Tooltip("Daire içi kamera")]
    public CinemachineCamera interiorCamera;

    [Header("Priority Settings")]
    [Tooltip("Aktif kamera için priority değeri")]
    public int activePriority = 20;
    
    [Tooltip("Pasif kameralar için priority değeri")]
    public int inactivePriority = 0;

    [Header("State")]
    [SerializeField]
    private FocusMode currentMode = FocusMode.BuildingView;
    
    private FocusMode lastAppliedMode = FocusMode.BuildingView;

    public FocusMode CurrentMode => currentMode;

    void OnValidate()
    {
        // Inspector'da değişiklik yapıldığında, play modundaysa uygula
        if (Application.isPlaying && currentMode != lastAppliedMode)
        {
            SetMode(currentMode);
        }
    }

    void Update()
    {
        // Her frame kontrol et - Inspector değişikliği için
        if (currentMode != lastAppliedMode)
        {
            SetMode(currentMode);
        }
    }

    void Start()
    {
        // Başlangıçta Building View aktif
        SetMode(FocusMode.BuildingView);
    }

    /// <summary>
    /// Daire moduna geç (dışarıdan bakış)
    /// </summary>
    [ContextMenu("Focus On Apartment")]
    public void FocusOnApartment()
    {
        SetMode(FocusMode.ExternalFocus);
    }

    /// <summary>
    /// Daire içine gir
    /// </summary>
    [ContextMenu("Enter Apartment")]
    public void EnterApartment()
    {
        SetMode(FocusMode.Interior);
    }

    /// <summary>
    /// Dışarı çık (External Focus'a dön)
    /// </summary>
    [ContextMenu("Exit Interior")]
    public void ExitInterior()
    {
        SetMode(FocusMode.ExternalFocus);
    }

    /// <summary>
    /// Bina görünümüne dön
    /// </summary>
    [ContextMenu("Exit To Building")]
    public void ExitToBuilding()
    {
        SetMode(FocusMode.BuildingView);
    }

    [Header("Blend Settings")]
    [Tooltip("Referans: Main Camera üzerindeki CinemachineBrain")]
    public CinemachineBrain brain;
    
    [Tooltip("Kameralar arası geçiş süresi (saniye)")]
    public float blendDuration = 2f;
    
    [Tooltip("Blend eğrisi")]
    public CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut;

    /// <summary>
    /// Belirli bir moda geç
    /// </summary>
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
        if (externalFocusCamera != null)
            externalFocusCamera.Priority = inactivePriority;
        if (interiorCamera != null)
            interiorCamera.Priority = inactivePriority;

        // Seçili kamerayı yüksek priority yap
        switch (mode)
        {
            case FocusMode.BuildingView:
                if (buildingCamera != null)
                    buildingCamera.Priority = activePriority;
                break;

            case FocusMode.ExternalFocus:
                if (externalFocusCamera != null)
                    externalFocusCamera.Priority = activePriority;
                break;

            case FocusMode.Interior:
                if (interiorCamera != null)
                    interiorCamera.Priority = activePriority;
                break;
        }

        Debug.Log($"[ApartmentFocusController] Mode changed to: {mode}, Blend: {blendDuration}s");
    }

    /// <summary>
    /// Bir sonraki moda geç (döngüsel)
    /// </summary>
    [ContextMenu("Next Mode")]
    public void NextMode()
    {
        switch (currentMode)
        {
            case FocusMode.BuildingView:
                FocusOnApartment();
                break;
            case FocusMode.ExternalFocus:
                EnterApartment();
                break;
            case FocusMode.Interior:
                ExitToBuilding();
                break;
        }
    }

    /// <summary>
    /// Bir önceki moda geç
    /// </summary>
    [ContextMenu("Previous Mode")]
    public void PreviousMode()
    {
        switch (currentMode)
        {
            case FocusMode.BuildingView:
                // Zaten en başta
                break;
            case FocusMode.ExternalFocus:
                ExitToBuilding();
                break;
            case FocusMode.Interior:
                ExitInterior();
                break;
        }
    }
}

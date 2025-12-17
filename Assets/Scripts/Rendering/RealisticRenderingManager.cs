using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Runtime script to toggle realistic rendering settings on/off.
/// Attach this to any GameObject in the scene.
/// Press R key to toggle realistic mode or use the public methods.
/// </summary>
public class RealisticRenderingManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the Volume component from your scene. If empty, will try to find one.")]
    public Volume postProcessVolume;
    
    [Header("Toggle Settings")]
    [Tooltip("Key to toggle realistic mode")]
    public KeyCode toggleKey = KeyCode.R;
    
    [Header("Realistic Settings")]
    [Range(0f, 1f)] public float bloomIntensity = 0.3f;
    [Range(0f, 2f)] public float bloomThreshold = 0.8f;
    [Range(0f, 1f)] public float bloomScatter = 0.65f;
    
    [Range(-2f, 2f)] public float postExposure = 0.2f;
    [Range(-100f, 100f)] public float contrast = 8f;
    [Range(-100f, 100f)] public float saturation = 8f;
    
    [Range(0f, 2f)] public float ssaoIntensity = 0.6f;
    
    [Range(0f, 1f)] public float vignetteIntensity = 0.25f;
    
    [Header("Status")]
    [SerializeField] private bool isRealisticModeActive = false;
    
    // Stored original values
    private struct OriginalSettings
    {
        // Bloom
        public bool bloomActive;
        public float bloomIntensity;
        public float bloomThreshold;
        public float bloomScatter;
        
        // Tonemapping
        public bool tonemappingActive;
        public TonemappingMode tonemappingMode;
        
        // Color Adjustments
        public bool colorAdjustmentsActive;
        public float postExposure;
        public float contrast;
        public float saturation;
        
        // Vignette
        public bool vignetteActive;
        public float vignetteIntensity;
    }
    
    private OriginalSettings originalSettings;
    private bool hasStoredOriginals = false;
    
    // Volume components
    private Bloom bloom;
    private Tonemapping tonemapping;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    public bool IsRealisticModeActive => isRealisticModeActive;

    private void Start()
    {
        InitializeVolume();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleRealisticMode();
        }
    }

    private void InitializeVolume()
    {
        if (postProcessVolume == null)
        {
            postProcessVolume = FindFirstObjectByType<Volume>();
            if (postProcessVolume == null)
            {
                Debug.LogWarning("[RealisticRenderingManager] No Volume found in scene. Please assign one.");
                return;
            }
        }

        var profile = postProcessVolume.profile;
        
        // Get or add components
        if (!profile.TryGet(out bloom))
        {
            bloom = profile.Add<Bloom>(false);
        }
        
        if (!profile.TryGet(out tonemapping))
        {
            tonemapping = profile.Add<Tonemapping>(false);
        }
        
        if (!profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = profile.Add<ColorAdjustments>(false);
        }
        
        if (!profile.TryGet(out vignette))
        {
            vignette = profile.Add<Vignette>(false);
        }
        
        StoreOriginalSettings();
    }

    private void StoreOriginalSettings()
    {
        if (hasStoredOriginals) return;
        
        // Store Bloom
        originalSettings.bloomActive = bloom.active;
        originalSettings.bloomIntensity = bloom.intensity.value;
        originalSettings.bloomThreshold = bloom.threshold.value;
        originalSettings.bloomScatter = bloom.scatter.value;
        
        // Store Tonemapping
        originalSettings.tonemappingActive = tonemapping.active;
        originalSettings.tonemappingMode = tonemapping.mode.value;
        
        // Store Color Adjustments
        originalSettings.colorAdjustmentsActive = colorAdjustments.active;
        originalSettings.postExposure = colorAdjustments.postExposure.value;
        originalSettings.contrast = colorAdjustments.contrast.value;
        originalSettings.saturation = colorAdjustments.saturation.value;
        
        // Store Vignette
        originalSettings.vignetteActive = vignette.active;
        originalSettings.vignetteIntensity = vignette.intensity.value;
        
        hasStoredOriginals = true;
        Debug.Log("[RealisticRenderingManager] Original settings stored.");
    }

    /// <summary>
    /// Toggle between realistic and original settings
    /// </summary>
    public void ToggleRealisticMode()
    {
        if (isRealisticModeActive)
        {
            RevertToOriginal();
        }
        else
        {
            ApplyRealisticSettings();
        }
    }

    /// <summary>
    /// Apply realistic rendering settings
    /// </summary>
    [ContextMenu("Apply Realistic Settings")]
    public void ApplyRealisticSettings()
    {
        if (postProcessVolume == null)
        {
            InitializeVolume();
            if (postProcessVolume == null) return;
        }

        // Store originals if not already stored
        StoreOriginalSettings();

        // Apply Bloom
        bloom.active = true;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = bloomIntensity;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = bloomThreshold;
        bloom.scatter.overrideState = true;
        bloom.scatter.value = bloomScatter;
        bloom.highQualityFiltering.overrideState = true;
        bloom.highQualityFiltering.value = true;

        // Apply Tonemapping (ACES)
        tonemapping.active = true;
        tonemapping.mode.overrideState = true;
        tonemapping.mode.value = TonemappingMode.ACES;

        // Apply Color Adjustments
        colorAdjustments.active = true;
        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = postExposure;
        colorAdjustments.contrast.overrideState = true;
        colorAdjustments.contrast.value = contrast;
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = saturation;

        // Apply Vignette
        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.intensity.value = vignetteIntensity;

        isRealisticModeActive = true;
        Debug.Log("[RealisticRenderingManager] ✓ Realistic mode ENABLED (Press " + toggleKey + " to toggle)");
    }

    /// <summary>
    /// Revert to original settings
    /// </summary>
    [ContextMenu("Revert to Original")]
    public void RevertToOriginal()
    {
        if (!hasStoredOriginals)
        {
            Debug.LogWarning("[RealisticRenderingManager] No original settings stored to revert to.");
            return;
        }

        // Revert Bloom
        bloom.active = originalSettings.bloomActive;
        bloom.intensity.value = originalSettings.bloomIntensity;
        bloom.threshold.value = originalSettings.bloomThreshold;
        bloom.scatter.value = originalSettings.bloomScatter;

        // Revert Tonemapping
        tonemapping.active = originalSettings.tonemappingActive;
        tonemapping.mode.value = originalSettings.tonemappingMode;

        // Revert Color Adjustments
        colorAdjustments.active = originalSettings.colorAdjustmentsActive;
        colorAdjustments.postExposure.value = originalSettings.postExposure;
        colorAdjustments.contrast.value = originalSettings.contrast;
        colorAdjustments.saturation.value = originalSettings.saturation;

        // Revert Vignette
        vignette.active = originalSettings.vignetteActive;
        vignette.intensity.value = originalSettings.vignetteIntensity;

        isRealisticModeActive = false;
        Debug.Log("[RealisticRenderingManager] ✗ Realistic mode DISABLED - Reverted to original settings");
    }
}

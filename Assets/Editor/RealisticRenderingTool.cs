using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Editor tool to toggle realistic rendering settings from Tools menu.
/// No need to add any component to scene - works directly!
/// </summary>
public class RealisticRenderingTool : EditorWindow
{
    private static Volume cachedVolume;
    private static bool isRealisticMode = false;
    
    // Realistic settings values
    private static float bloomIntensity = 0.3f;
    private static float bloomThreshold = 0.8f;
    private static float postExposure = 0.2f;
    private static float contrast = 8f;
    private static float saturation = 8f;
    private static float vignetteIntensity = 0.25f;
    
    // Stored original values
    private static float origBloomIntensity;
    private static float origBloomThreshold;
    private static TonemappingMode origTonemappingMode;
    private static float origPostExposure;
    private static float origContrast;
    private static float origSaturation;
    private static float origVignetteIntensity;
    private static bool hasStoredOriginals = false;

    [MenuItem("Tools/Realistic Rendering/Apply Realistic Mode %#r")]
    public static void ApplyRealisticMode()
    {
        var volume = FindVolume();
        if (volume == null) return;

        var profile = volume.profile;
        
        // Store originals first
        if (!hasStoredOriginals)
        {
            StoreOriginals(profile);
        }

        // Get or create components
        if (!profile.TryGet<Bloom>(out var bloom))
            bloom = profile.Add<Bloom>(true);
        if (!profile.TryGet<Tonemapping>(out var tonemapping))
            tonemapping = profile.Add<Tonemapping>(true);
        if (!profile.TryGet<ColorAdjustments>(out var colorAdjustments))
            colorAdjustments = profile.Add<ColorAdjustments>(true);
        if (!profile.TryGet<Vignette>(out var vignette))
            vignette = profile.Add<Vignette>(true);

        // Apply Bloom
        bloom.active = true;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = bloomIntensity;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = bloomThreshold;
        bloom.highQualityFiltering.overrideState = true;
        bloom.highQualityFiltering.value = true;

        // Apply ACES Tonemapping
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

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        isRealisticMode = true;
        
        Debug.Log("✓ <color=green>Realistic Mode ENABLED & SAVED</color> - ACES Tonemapping, Bloom, Color Grading applied!");
    }

    [MenuItem("Tools/Realistic Rendering/Revert to Original %#t")]
    public static void RevertToOriginal()
    {
        if (!hasStoredOriginals)
        {
            Debug.LogWarning("No original settings stored. Apply realistic mode first.");
            return;
        }

        var volume = FindVolume();
        if (volume == null) return;

        var profile = volume.profile;

        if (profile.TryGet<Bloom>(out var bloom))
        {
            bloom.intensity.value = origBloomIntensity;
            bloom.threshold.value = origBloomThreshold;
        }

        if (profile.TryGet<Tonemapping>(out var tonemapping))
        {
            tonemapping.mode.value = origTonemappingMode;
        }

        if (profile.TryGet<ColorAdjustments>(out var colorAdjustments))
        {
            colorAdjustments.postExposure.value = origPostExposure;
            colorAdjustments.contrast.value = origContrast;
            colorAdjustments.saturation.value = origSaturation;
        }

        if (profile.TryGet<Vignette>(out var vignette))
        {
            vignette.intensity.value = origVignetteIntensity;
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        isRealisticMode = false;
        
        Debug.Log("✗ <color=yellow>Realistic Mode DISABLED & SAVED</color> - Reverted to original settings");
    }

    [MenuItem("Tools/Realistic Rendering/Toggle Realistic Mode %r")]
    public static void ToggleRealisticMode()
    {
        if (isRealisticMode)
            RevertToOriginal();
        else
            ApplyRealisticMode();
    }

    private static void StoreOriginals(VolumeProfile profile)
    {
        if (profile.TryGet<Bloom>(out var bloom))
        {
            origBloomIntensity = bloom.intensity.value;
            origBloomThreshold = bloom.threshold.value;
        }

        if (profile.TryGet<Tonemapping>(out var tonemapping))
        {
            origTonemappingMode = tonemapping.mode.value;
        }

        if (profile.TryGet<ColorAdjustments>(out var colorAdjustments))
        {
            origPostExposure = colorAdjustments.postExposure.value;
            origContrast = colorAdjustments.contrast.value;
            origSaturation = colorAdjustments.saturation.value;
        }

        if (profile.TryGet<Vignette>(out var vignette))
        {
            origVignetteIntensity = vignette.intensity.value;
        }

        hasStoredOriginals = true;
    }

    private static Volume FindVolume()
    {
        if (cachedVolume != null) return cachedVolume;

        cachedVolume = Object.FindFirstObjectByType<Volume>();
        
        if (cachedVolume == null)
        {
            Debug.LogError("No Volume found in scene! Please add a Global Volume to your scene first.");
            return null;
        }

        return cachedVolume;
    }
}

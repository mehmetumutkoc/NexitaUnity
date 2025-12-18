using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Directional Light kullanarak gece/gündüz döngüsü ve saat simülasyonu yapan controller.
/// Güneşin/ay'ın pozisyonunu, ışık rengini ve yoğunluğunu gerçek zamanlı kontrol eder.
/// </summary>
public class DayNightController : MonoBehaviour
{
    [Header("=== Referanslar ===")]
    [Tooltip("Sahnenin ana Directional Light'ı (Güneş/Ay)")]
    [SerializeField] private Light directionalLight;
    
    [Header("=== Zaman Ayarları ===")]
    [Tooltip("Başlangıç saati (0-24)")]
    [SerializeField][Range(0f, 24f)] private float startTime = 12f;
    
    [Tooltip("Oyundaki 1 saatin gerçek saniye karşılığı")]
    [SerializeField] private float realSecondsPerGameHour = 60f;
    
    [Tooltip("Zaman otomatik ilerlesin mi?")]
    [SerializeField] private bool autoProgress = true;
    
    [Header("=== Inspector Zaman Kontrolü ===")]
    [Tooltip("Bu slider ile saati Inspector'dan canlı değiştirebilirsiniz!")]
    [SerializeField][Range(0f, 24f)] private float editorCurrentTime = 12f;
    
    [Tooltip("Zaman hızı çarpanı (1 = normal, 2 = 2x hızlı, 0.5 = yarı hız)")]
    [SerializeField][Range(0.1f, 10f)] private float timeSpeedMultiplier = 1f;
    
    [Header("=== Güneş Rotasyonu ===")]
    [Tooltip("Güneşin gün içindeki Y ekseni rotasyonu (yön)")]
    [SerializeField] private float sunYRotation = 0f;
    
    [Tooltip("Güneşin en yüksek noktadaki açısı")]
    [SerializeField] private float maxSunAngle = 80f;
    
    [Header("=== Işık Yoğunluğu ===")]
    [Tooltip("Gündüz maksimum ışık yoğunluğu")]
    [SerializeField] private float dayIntensity = 1.2f;
    
    [Tooltip("Gece minimum ışık yoğunluğu (ay ışığı)")]
    [SerializeField] private float nightIntensity = 0.1f;
    
    [Tooltip("Gün doğumu/batımı geçiş süresi (saat)")]
    [SerializeField] private float transitionDuration = 2f;
    
    [Header("=== Renk Ayarları ===")]
    [Tooltip("Gündüz ışık rengi")]
    [SerializeField] private Color dayColor = new Color(1f, 0.96f, 0.84f);
    
    [Tooltip("Gün doğumu/batımı rengi")]
    [SerializeField] private Color sunsetColor = new Color(1f, 0.5f, 0.2f);
    
    [Tooltip("Gece ışık rengi (ay)")]
    [SerializeField] private Color nightColor = new Color(0.3f, 0.4f, 0.6f);
    
    [Header("=== Gün Döngüsü Saatleri ===")]
    [Tooltip("Gün doğumu başlangıç saati")]
    [SerializeField] private float sunriseStart = 5f;
    
    [Tooltip("Gün doğumu bitiş saati")]
    [SerializeField] private float sunriseEnd = 7f;
    
    [Tooltip("Gün batımı başlangıç saati")]
    [SerializeField] private float sunsetStart = 18f;
    
    [Tooltip("Gün batımı bitiş saati")]
    [SerializeField] private float sunsetEnd = 20f;
    
    [Header("=== Ambient Ayarları ===")]
    [Tooltip("Ambient ışığı da kontrol edilsin mi?")]
    [SerializeField] private bool controlAmbient = true;
    
    [Tooltip("Gündüz ambient rengi")]
    [SerializeField] private Color dayAmbient = new Color(0.5f, 0.5f, 0.55f);
    
    [Tooltip("Gece ambient rengi")]
    [SerializeField] private Color nightAmbient = new Color(0.05f, 0.05f, 0.08f);
    
    [Header("=== Fog Ayarları ===")]
    [Tooltip("Fog kontrolü aktif mi?")]
    [SerializeField] private bool controlFog = true;
    
    [Tooltip("Gündüz fog rengi")]
    [SerializeField] private Color dayFogColor = new Color(0.7f, 0.8f, 0.9f);
    
    [Tooltip("Gece fog rengi")]
    [SerializeField] private Color nightFogColor = new Color(0.05f, 0.05f, 0.1f);
    
    [Header("=== Skybox Ayarları ===")]
    [Tooltip("Skybox kontrolü aktif mi?")]
    [SerializeField] private bool controlSkybox = true;
    
    [Tooltip("Skybox blend modunu kullan (iki skybox arası geçiş)")]
    [SerializeField] private bool useSkyboxBlend = false;
    
    [Tooltip("Gündüz skybox materyali (blend modu için)")]
    [SerializeField] private Material daySkybox;
    
    [Tooltip("Gece skybox materyali (blend modu için)")]
    [SerializeField] private Material nightSkybox;
    
    [Tooltip("Procedural/Gradient skybox için: Gündüz üst rengi")]
    [SerializeField] private Color daySkyColor = new Color(0.5f, 0.65f, 0.8f);
    
    [Tooltip("Procedural/Gradient skybox için: Gece üst rengi")]
    [SerializeField] private Color nightSkyColor = new Color(0.01f, 0.01f, 0.03f);
    
    [Tooltip("Procedural/Gradient skybox için: Gündüz ufuk rengi")]
    [SerializeField] private Color dayHorizonColor = new Color(0.7f, 0.75f, 0.8f);
    
    [Tooltip("Procedural/Gradient skybox için: Gece ufuk rengi")]
    [SerializeField] private Color nightHorizonColor = new Color(0.02f, 0.02f, 0.05f);
    
    [Tooltip("Gün doğumu/batımı skybox rengi")]
    [SerializeField] private Color sunsetSkyColor = new Color(0.8f, 0.45f, 0.3f);
    
    [Header("=== Environment Lighting ===")]
    [Tooltip("Environment Lighting kontrolü aktif mi?")]
    [SerializeField] private bool controlEnvironmentLighting = true;
    
    [Tooltip("Gündüz environment yoğunluğu")]
    [SerializeField][Range(0f, 8f)] private float dayEnvironmentIntensity = 1f;
    
    [Tooltip("Gece environment yoğunluğu")]
    [SerializeField][Range(0f, 8f)] private float nightEnvironmentIntensity = 0.2f;
    
    [Tooltip("Gündüz reflection yoğunluğu")]
    [SerializeField][Range(0f, 1f)] private float dayReflectionIntensity = 1f;
    
    [Tooltip("Gece reflection yoğunluğu")]
    [SerializeField][Range(0f, 1f)] private float nightReflectionIntensity = 0.3f;
    
    [Header("=== Olaylar ===")]
    public UnityEvent onSunrise;
    public UnityEvent onSunset;
    public UnityEvent<float> onHourChanged;
    public UnityEvent<TimeOfDay> onTimeOfDayChanged;
    
    // Günün zaman dilimi enumları
    public enum TimeOfDay
    {
        Night,      // 22:00 - 05:00
        Sunrise,    // 05:00 - 07:00
        Morning,    // 07:00 - 12:00
        Afternoon,  // 12:00 - 18:00
        Sunset,     // 18:00 - 20:00
        Evening     // 20:00 - 22:00
    }
    
    // Property'ler
    public float CurrentTime => currentTime;
    public int CurrentHour => Mathf.FloorToInt(currentTime);
    public int CurrentMinute => Mathf.FloorToInt((currentTime - CurrentHour) * 60);
    public TimeOfDay CurrentTimeOfDay => GetTimeOfDay();
    public bool IsDay => currentTime >= sunriseEnd && currentTime < sunsetStart;
    public bool IsNight => currentTime >= sunsetEnd || currentTime < sunriseStart;
    public string FormattedTime => $"{CurrentHour:00}:{CurrentMinute:00}";
    public float NormalizedTime => currentTime / 24f;
    
    // Private değişkenler
    private float currentTime;
    private TimeOfDay lastTimeOfDay;
    private int lastHour = -1;
    private bool sunriseEventFired = false;
    private bool sunsetEventFired = false;
    
    private void Awake()
    {
        // Directional Light otomatik bulunması
        if (directionalLight == null)
        {
            directionalLight = FindObjectOfType<Light>();
            if (directionalLight != null && directionalLight.type != LightType.Directional)
            {
                // Sahnedekileri tara
                foreach (Light light in FindObjectsOfType<Light>())
                {
                    if (light.type == LightType.Directional)
                    {
                        directionalLight = light;
                        break;
                    }
                }
            }
        }
        
        if (directionalLight == null)
        {
            Debug.LogError("[DayNightController] Directional Light bulunamadı! Lütfen Inspector'dan atayın.");
            enabled = false;
            return;
        }
    }
    
    private void Start()
    {
        currentTime = startTime;
        lastTimeOfDay = GetTimeOfDay();
        lastHour = CurrentHour;
        UpdateLighting();
    }
    
    private void Update()
    {
        if (autoProgress)
        {
            AdvanceTime(Time.deltaTime * timeSpeedMultiplier);
        }
    }
    
    /// <summary>
    /// Zamanı belirli bir miktar ilerletir
    /// </summary>
    public void AdvanceTime(float deltaTime)
    {
        float hoursToAdd = deltaTime / realSecondsPerGameHour;
        currentTime += hoursToAdd;
        
        // 24 saat döngüsü
        if (currentTime >= 24f)
        {
            currentTime -= 24f;
            sunriseEventFired = false;
            sunsetEventFired = false;
        }
        
        UpdateLighting();
        CheckEvents();
    }
    
    /// <summary>
    /// Belirli bir saate atlar
    /// </summary>
    public void SetTime(float hour)
    {
        currentTime = Mathf.Clamp(hour, 0f, 23.99f);
        UpdateLighting();
        CheckEvents();
    }
    
    /// <summary>
    /// Belirli saat ve dakikaya atlar
    /// </summary>
    public void SetTime(int hour, int minute)
    {
        SetTime(hour + minute / 60f);
    }
    
    /// <summary>
    /// Zamanı hızlandırır/yavaşlatır
    /// </summary>
    public void SetTimeScale(float scale)
    {
        realSecondsPerGameHour = 60f / scale;
    }
    
    /// <summary>
    /// Otomatik zaman ilerlemesini kontrol eder
    /// </summary>
    public void SetAutoProgress(bool enabled)
    {
        autoProgress = enabled;
    }
    
    /// <summary>
    /// Tüm ışık ayarlarını günceller
    /// </summary>
    private void UpdateLighting()
    {
        if (directionalLight == null) return;
        
        UpdateSunRotation();
        UpdateLightIntensityAndColor();
        
        if (controlAmbient)
        {
            UpdateAmbientLight();
        }
        
        if (controlFog)
        {
            UpdateFog();
        }
        
        if (controlSkybox)
        {
            UpdateSkybox();
        }
        
        if (controlEnvironmentLighting)
        {
            UpdateEnvironmentLighting();
        }
    }
    
    /// <summary>
    /// Güneş/ay rotasyonunu günceller
    /// </summary>
    private void UpdateSunRotation()
    {
        // Zaman bazlı açı hesaplama: 0 saat = -90 derece (ufukta), 12 saat = 90 derece (tepede)
        // Gece boyunca güneş ufkun altında kalır
        float timeRatio = currentTime / 24f;
        float sunAngle = (timeRatio * 360f) - 90f;
        
        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, sunYRotation, 0f);
    }
    
    /// <summary>
    /// Işık yoğunluğu ve rengini günceller
    /// </summary>
    private void UpdateLightIntensityAndColor()
    {
        float intensity;
        Color lightColor;
        
        // Gece (sunsetEnd -> sunriseStart)
        if (currentTime >= sunsetEnd || currentTime < sunriseStart)
        {
            intensity = nightIntensity;
            lightColor = nightColor;
        }
        // Gün doğumu geçişi (sunriseStart -> sunriseEnd)
        else if (currentTime >= sunriseStart && currentTime < sunriseEnd)
        {
            float t = (currentTime - sunriseStart) / (sunriseEnd - sunriseStart);
            intensity = Mathf.Lerp(nightIntensity, dayIntensity, t);
            
            // Gece -> turuncu -> gündüz geçişi
            if (t < 0.5f)
            {
                lightColor = Color.Lerp(nightColor, sunsetColor, t * 2f);
            }
            else
            {
                lightColor = Color.Lerp(sunsetColor, dayColor, (t - 0.5f) * 2f);
            }
        }
        // Gündüz (sunriseEnd -> sunsetStart)
        else if (currentTime >= sunriseEnd && currentTime < sunsetStart)
        {
            intensity = dayIntensity;
            lightColor = dayColor;
        }
        // Gün batımı geçişi (sunsetStart -> sunsetEnd)
        else // currentTime >= sunsetStart && currentTime < sunsetEnd
        {
            float t = (currentTime - sunsetStart) / (sunsetEnd - sunsetStart);
            intensity = Mathf.Lerp(dayIntensity, nightIntensity, t);
            
            // Gündüz -> turuncu -> gece geçişi
            if (t < 0.5f)
            {
                lightColor = Color.Lerp(dayColor, sunsetColor, t * 2f);
            }
            else
            {
                lightColor = Color.Lerp(sunsetColor, nightColor, (t - 0.5f) * 2f);
            }
        }
        
        directionalLight.intensity = intensity;
        directionalLight.color = lightColor;
    }
    
    /// <summary>
    /// Ambient ışığı günceller
    /// </summary>
    private void UpdateAmbientLight()
    {
        float dayFactor = GetDayFactor();
        RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, dayFactor);
    }
    
    /// <summary>
    /// Fog ayarlarını günceller
    /// </summary>
    private void UpdateFog()
    {
        float dayFactor = GetDayFactor();
        RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, dayFactor);
    }
    
    /// <summary>
    /// Skybox'ı günceller
    /// </summary>
    private void UpdateSkybox()
    {
        float dayFactor = GetDayFactor();
        
        // Blend modu: İki farklı skybox materyali arasında geçiş
        if (useSkyboxBlend && daySkybox != null && nightSkybox != null)
        {
            // Skybox Blend shader kullanılıyorsa (_Blend property'si ile)
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Blend"))
            {
                RenderSettings.skybox.SetFloat("_Blend", 1f - dayFactor);
            }
            else
            {
                // Basit materyal değişimi (threshold kullanarak)
                RenderSettings.skybox = dayFactor > 0.5f ? daySkybox : nightSkybox;
            }
        }
        // Renk modu: Mevcut skybox'ın renklerini değiştir
        else if (RenderSettings.skybox != null)
        {
            Material skyMat = RenderSettings.skybox;
            
            // Gün doğumu/batımı renk geçişi
            Color currentSkyColor;
            Color currentHorizonColor;
            
            // Gece
            if (currentTime >= sunsetEnd || currentTime < sunriseStart)
            {
                currentSkyColor = nightSkyColor;
                currentHorizonColor = nightHorizonColor;
            }
            // Gün doğumu
            else if (currentTime >= sunriseStart && currentTime < sunriseEnd)
            {
                float t = (currentTime - sunriseStart) / (sunriseEnd - sunriseStart);
                if (t < 0.5f)
                {
                    currentSkyColor = Color.Lerp(nightSkyColor, sunsetSkyColor, t * 2f);
                    currentHorizonColor = Color.Lerp(nightHorizonColor, sunsetSkyColor, t * 2f);
                }
                else
                {
                    currentSkyColor = Color.Lerp(sunsetSkyColor, daySkyColor, (t - 0.5f) * 2f);
                    currentHorizonColor = Color.Lerp(sunsetSkyColor, dayHorizonColor, (t - 0.5f) * 2f);
                }
            }
            // Gündüz
            else if (currentTime >= sunriseEnd && currentTime < sunsetStart)
            {
                currentSkyColor = daySkyColor;
                currentHorizonColor = dayHorizonColor;
            }
            // Gün batımı
            else
            {
                float t = (currentTime - sunsetStart) / (sunsetEnd - sunsetStart);
                if (t < 0.5f)
                {
                    currentSkyColor = Color.Lerp(daySkyColor, sunsetSkyColor, t * 2f);
                    currentHorizonColor = Color.Lerp(dayHorizonColor, sunsetSkyColor, t * 2f);
                }
                else
                {
                    currentSkyColor = Color.Lerp(sunsetSkyColor, nightSkyColor, (t - 0.5f) * 2f);
                    currentHorizonColor = Color.Lerp(sunsetSkyColor, nightHorizonColor, (t - 0.5f) * 2f);
                }
            }
            
            // Unity'nin farklı skybox shader'ları için property isimleri
            // Procedural Skybox
            if (skyMat.HasProperty("_SkyTint"))
            {
                skyMat.SetColor("_SkyTint", currentSkyColor);
            }
            if (skyMat.HasProperty("_GroundColor"))
            {
                skyMat.SetColor("_GroundColor", currentHorizonColor);
            }
            
            // Gradient Skybox (eğer varsa)
            if (skyMat.HasProperty("_TopColor"))
            {
                skyMat.SetColor("_TopColor", currentSkyColor);
            }
            if (skyMat.HasProperty("_BottomColor"))
            {
                skyMat.SetColor("_BottomColor", currentHorizonColor);
            }
            if (skyMat.HasProperty("_HorizonColor"))
            {
                skyMat.SetColor("_HorizonColor", currentHorizonColor);
            }
            
            // 6 Sided Skybox (Cubemap) için tint
            if (skyMat.HasProperty("_Tint"))
            {
                skyMat.SetColor("_Tint", Color.Lerp(nightSkyColor, Color.white, dayFactor));
            }
            
            // Exposure ayarı (varsa)
            if (skyMat.HasProperty("_Exposure"))
            {
                float exposure = Mathf.Lerp(0.3f, 1.0f, dayFactor);
                skyMat.SetFloat("_Exposure", exposure);
            }
        }
    }
    
    /// <summary>
    /// Environment Lighting ayarlarını günceller
    /// </summary>
    private void UpdateEnvironmentLighting()
    {
        float dayFactor = GetDayFactor();
        
        // Environment Lighting Intensity
        RenderSettings.ambientIntensity = Mathf.Lerp(nightEnvironmentIntensity, dayEnvironmentIntensity, dayFactor);
        
        // Reflection Intensity
        RenderSettings.reflectionIntensity = Mathf.Lerp(nightReflectionIntensity, dayReflectionIntensity, dayFactor);
        
        // Skybox'ı reflection source olarak güncelle
        if (RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Skybox)
        {
            DynamicGI.UpdateEnvironment();
        }
    }
    
    /// <summary>
    /// Gündüz faktörünü hesaplar (0 = tam gece, 1 = tam gündüz)
    /// </summary>
    private float GetDayFactor()
    {
        if (currentTime >= sunriseEnd && currentTime < sunsetStart)
        {
            return 1f;
        }
        else if (currentTime >= sunsetEnd || currentTime < sunriseStart)
        {
            return 0f;
        }
        else if (currentTime >= sunriseStart && currentTime < sunriseEnd)
        {
            return (currentTime - sunriseStart) / (sunriseEnd - sunriseStart);
        }
        else // sunsetStart -> sunsetEnd
        {
            return 1f - (currentTime - sunsetStart) / (sunsetEnd - sunsetStart);
        }
    }
    
    /// <summary>
    /// Günün zaman dilimini döndürür
    /// </summary>
    private TimeOfDay GetTimeOfDay()
    {
        if (currentTime >= 22f || currentTime < sunriseStart)
            return TimeOfDay.Night;
        if (currentTime >= sunriseStart && currentTime < sunriseEnd)
            return TimeOfDay.Sunrise;
        if (currentTime >= sunriseEnd && currentTime < 12f)
            return TimeOfDay.Morning;
        if (currentTime >= 12f && currentTime < sunsetStart)
            return TimeOfDay.Afternoon;
        if (currentTime >= sunsetStart && currentTime < sunsetEnd)
            return TimeOfDay.Sunset;
        return TimeOfDay.Evening;
    }
    
    /// <summary>
    /// Event'leri kontrol eder ve tetikler
    /// </summary>
    private void CheckEvents()
    {
        // Saat değişimi
        int currentHour = CurrentHour;
        if (currentHour != lastHour)
        {
            lastHour = currentHour;
            onHourChanged?.Invoke(currentHour);
        }
        
        // Zaman dilimi değişimi
        TimeOfDay currentTOD = GetTimeOfDay();
        if (currentTOD != lastTimeOfDay)
        {
            lastTimeOfDay = currentTOD;
            onTimeOfDayChanged?.Invoke(currentTOD);
            
            // Gün doğumu/batımı event'leri
            if (currentTOD == TimeOfDay.Sunrise && !sunriseEventFired)
            {
                sunriseEventFired = true;
                onSunrise?.Invoke();
            }
            else if (currentTOD == TimeOfDay.Sunset && !sunsetEventFired)
            {
                sunsetEventFired = true;
                onSunset?.Invoke();
            }
        }
    }
    
    // ================== EDITOR / DEBUG ==================
    
#if UNITY_EDITOR
    [Header("=== Debug (Sadece Editor) ===")]
    [SerializeField] private bool showDebugInfo = true;
    
    private void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 14;
        style.alignment = TextAnchor.MiddleLeft;
        style.normal.textColor = Color.white;
        
        string info = $"🕐 Saat: {FormattedTime}\n" +
                      $"🌍 Dönem: {CurrentTimeOfDay}\n" +
                      $"☀️ Gündüz: {IsDay}\n" +
                      $"🌙 Gece: {IsNight}\n" +
                      $"💡 Yoğunluk: {directionalLight?.intensity:F2}";
        
        GUI.Box(new Rect(10, 10, 200, 110), info, style);
    }
    
    private float lastEditorTime = -1f;
    
    private void OnValidate()
    {
        // Editor'da editorCurrentTime slider'ı değiştirildiğinde
        if (directionalLight != null)
        {
            // Play mode'da: slider değiştiğinde currentTime'ı güncelle
            if (Application.isPlaying)
            {
                if (!Mathf.Approximately(editorCurrentTime, currentTime))
                {
                    SetTime(editorCurrentTime);
                }
            }
            // Edit mode'da: startTime veya editorCurrentTime değiştiğinde preview güncelle
            else
            {
                if (!Mathf.Approximately(editorCurrentTime, lastEditorTime))
                {
                    lastEditorTime = editorCurrentTime;
                    currentTime = editorCurrentTime;
                    startTime = editorCurrentTime; // startTime'ı da senkronize et
                    UpdateLighting();
                }
            }
        }
    }
    
    // Her frame editorCurrentTime'ı currentTime ile senkronize et (sadece oyun modunda)
    private void LateUpdate()
    {
        if (Application.isPlaying && autoProgress)
        {
            editorCurrentTime = currentTime;
        }
    }
#endif
    
    // ================== HIZLI SAAT ATLAMA ==================
    
    /// <summary>
    /// Gün doğumuna atla
    /// </summary>
    public void JumpToSunrise()
    {
        SetTime(sunriseStart);
    }
    
    /// <summary>
    /// Öğlene atla
    /// </summary>
    public void JumpToNoon()
    {
        SetTime(12f);
    }
    
    /// <summary>
    /// Gün batımına atla
    /// </summary>
    public void JumpToSunset()
    {
        SetTime(sunsetStart);
    }
    
    /// <summary>
    /// Gece yarısına atla
    /// </summary>
    public void JumpToMidnight()
    {
        SetTime(0f);
    }
}

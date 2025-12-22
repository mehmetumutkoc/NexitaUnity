using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Focus modları için UI butonlarını yönetir.
/// BuildingFocusController ile entegre çalışır.
/// </summary>
public class FocusModeUIController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("BuildingFocusController referansı")]
    public BuildingFocusController focusController;

    [Header("UI Buttons")]
    [Tooltip("Focus modundan çıkmak için Exit butonu")]
    public Button exitButton;
    
    [Tooltip("Interior moda girmek için Enter butonu")]
    public Button enterInteriorButton;

    [Header("UI Canvas")]
    [Tooltip("Butonları içeren Canvas (opsiyonel - otomatik oluşturulur)")]
    public Canvas uiCanvas;

    private bool uiCreatedAtRuntime = false;

    void Start()
    {
        // Referansı otomatik bul
        if (focusController == null)
        {
            focusController = FindObjectOfType<BuildingFocusController>();
        }

        // Event listener'ı bağla (OnEnable Start'tan önce çağrıldığında controller null olabilir)
        if (focusController != null)
        {
            focusController.OnModeChanged += OnFocusModeChanged;
        }

        // UI yoksa oluştur
        if (uiCanvas == null || exitButton == null || enterInteriorButton == null)
        {
            CreateUI();
        }

        // Buton event'lerini bağla
        SetupButtonListeners();

        // Başlangıçta butonları gizle
        UpdateButtonVisibility();
    }

    void OnEnable()
    {
        if (focusController != null)
        {
            focusController.OnModeChanged += OnFocusModeChanged;
        }
    }

    void OnDisable()
    {
        if (focusController != null)
        {
            focusController.OnModeChanged -= OnFocusModeChanged;
        }
    }

    void OnFocusModeChanged(BuildingFocusController.FocusMode newMode)
    {
        UpdateButtonVisibility();
    }

    void CreateUI()
    {
        uiCreatedAtRuntime = true;

        // Canvas oluştur
        if (uiCanvas == null)
        {
            GameObject canvasObj = new GameObject("FocusModeCanvas");
            uiCanvas = canvasObj.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Exit Button oluştur
        if (exitButton == null)
        {
            exitButton = CreateButton("ExitButton", "Exit Focus", new Vector2(-120, 50), Color.red);
        }

        // Enter Interior Button oluştur
        if (enterInteriorButton == null)
        {
            enterInteriorButton = CreateButton("EnterInteriorButton", "Enter Interior", new Vector2(120, 50), new Color(0.2f, 0.6f, 0.2f));
        }

        Debug.Log("[FocusModeUIController] UI created at runtime");
    }

    Button CreateButton(string name, string text, Vector2 position, Color bgColor)
    {
        // Button GameObject
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(uiCanvas.transform, false);

        // RectTransform
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0);
        rectTransform.anchorMax = new Vector2(0.5f, 0);
        rectTransform.pivot = new Vector2(0.5f, 0);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(180, 60);

        // Image (background)
        Image image = buttonObj.AddComponent<Image>();
        image.color = bgColor;

        // Button component
        Button button = buttonObj.AddComponent<Button>();
        
        // Color transition
        ColorBlock colors = button.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        colors.selectedColor = bgColor;
        button.colors = colors;

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = 20;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = Color.white;

        return button;
    }

    void SetupButtonListeners()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        if (enterInteriorButton != null)
        {
            enterInteriorButton.onClick.RemoveAllListeners();
            enterInteriorButton.onClick.AddListener(OnEnterInteriorButtonClicked);
        }
    }

    void UpdateButtonVisibility()
    {
        if (focusController == null)
            return;

        BuildingFocusController.FocusMode mode = focusController.CurrentMode;

        // Exit Button: UnitFocus veya UnitInterior modunda göster
        if (exitButton != null)
        {
            bool showExit = mode == BuildingFocusController.FocusMode.UnitFocus || 
                           mode == BuildingFocusController.FocusMode.UnitInterior;
            exitButton.gameObject.SetActive(showExit);
        }

        // Enter Interior Button: Sadece UnitFocus modunda göster
        if (enterInteriorButton != null)
        {
            bool showEnterInterior = mode == BuildingFocusController.FocusMode.UnitFocus;
            enterInteriorButton.gameObject.SetActive(showEnterInterior);
        }
    }

    void OnExitButtonClicked()
    {
        if (focusController == null)
            return;

        Debug.Log("[FocusModeUIController] Exit button clicked");
        focusController.ExitToBuilding();
    }

    void OnEnterInteriorButtonClicked()
    {
        if (focusController == null)
            return;

        Debug.Log("[FocusModeUIController] Enter Interior button clicked");
        focusController.EnterUnit();
    }

    void OnDestroy()
    {
        // Runtime'da oluşturulan UI'ı temizle
        if (uiCreatedAtRuntime && uiCanvas != null)
        {
            Destroy(uiCanvas.gameObject);
        }
    }
}

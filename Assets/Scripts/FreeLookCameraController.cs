using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// Unity Editor Scene View ile AYNI kontroller!
/// - Sağ Tık + WASD: Flythrough
/// - Orta Tık Sürükle: Pan (kaydırma)
/// - Scroll: İleri/geri zoom
/// - Alt + Sol Tık: Orbit (dönme)
/// - F: Seçili objeye odaklan
/// </summary>
public class FreeLookCameraController : MonoBehaviour
{
    [Header("=== Referanslar ===")]
    [Tooltip("Kontrol edilecek Cinemachine kamerası")]
    [SerializeField] private CinemachineCamera targetCamera;
    
    [Tooltip("Orbit modu için pivot noktası (boşsa sahne merkezi)")]
    [SerializeField] private Transform orbitPivot;

    [Header("=== Flythrough (Sağ Tık + WASD) ===")]
    [Tooltip("Normal hareket hızı")]
    [SerializeField] private float flySpeed = 10f;
    
    [Tooltip("Shift ile hız çarpanı")]
    [SerializeField] private float shiftMultiplier = 3f;

    [Header("=== Bakış Hassasiyeti ===")]
    [Tooltip("Mouse bakış hassasiyeti")]
    [SerializeField][Range(0.1f, 10f)] private float lookSensitivity = 2f;
    
    [Tooltip("Y ekseni ters mi?")]
    [SerializeField] private bool invertY = false;

    [Header("=== Pan (Orta Tık Sürükle) ===")]
    [Tooltip("Pan hassasiyeti")]
    [SerializeField] private float panSensitivity = 0.5f;

    [Header("=== Scroll Zoom ===")]
    [Tooltip("Scroll zoom hızı")]
    [SerializeField] private float scrollSpeed = 5f;
    
    [Tooltip("Scroll ile ilerlerken hızlanma")]
    [SerializeField] private bool scrollAcceleration = true;

    [Header("=== Orbit (Alt + Sol Tık) ===")]
    [Tooltip("Orbit hassasiyeti")]
    [SerializeField] private float orbitSensitivity = 2f;
    
    [Tooltip("Varsayılan orbit mesafesi")]
    [SerializeField] private float defaultOrbitDistance = 10f;

    [Header("=== Açı Limitleri ===")]
    [Tooltip("Minimum pitch açısı")]
    [SerializeField] private float minPitch = -89f;
    
    [Tooltip("Maksimum pitch açısı")]
    [SerializeField] private float maxPitch = 89f;

    [Header("=== State ===")]
    [SerializeField] private bool isActive = false;

    // Private değişkenler
    private float yaw = 0f;
    private float pitch = 0f;
    private Vector3 orbitCenter;
    private float orbitDistance;
    
    private Transform camTransform;
    private bool isFlying = false;
    private bool isPanning = false;
    private bool isOrbiting = false;

    public bool IsActive => isActive;

    private void Start()
    {
        if (targetCamera != null)
        {
            camTransform = targetCamera.transform;
            InitializeFromCamera();
        }
    }

    private void Update()
    {
        if (!isActive || camTransform == null) return;
        
        HandleInput();
    }

    private void InitializeFromCamera()
    {
        Vector3 euler = camTransform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        
        // Orbit merkezi
        if (orbitPivot != null)
        {
            orbitCenter = orbitPivot.position;
        }
        else
        {
            orbitCenter = camTransform.position + camTransform.forward * defaultOrbitDistance;
        }
        orbitDistance = Vector3.Distance(camTransform.position, orbitCenter);
    }

    private void HandleInput()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse == null) return;

        bool rightClick = mouse.rightButton.isPressed;
        bool middleClick = mouse.middleButton.isPressed;
        bool leftClick = mouse.leftButton.isPressed;
        bool altKey = keyboard != null && keyboard.altKey.isPressed;
        
        Vector2 mouseDelta = mouse.delta.ReadValue();
        float scroll = mouse.scroll.ReadValue().y;

        // ================== SAĞ TIK: FLYTHROUGH ==================
        if (rightClick)
        {
            if (!isFlying)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                isFlying = true;
            }
            
            // Mouse look
            yaw += mouseDelta.x * lookSensitivity * 0.1f;
            pitch += (invertY ? mouseDelta.y : -mouseDelta.y) * lookSensitivity * 0.1f;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            
            // WASD hareket
            Vector3 move = Vector3.zero;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed) move.z += 1f;
                if (keyboard.sKey.isPressed) move.z -= 1f;
                if (keyboard.aKey.isPressed) move.x -= 1f;
                if (keyboard.dKey.isPressed) move.x += 1f;
                if (keyboard.eKey.isPressed || keyboard.spaceKey.isPressed) move.y += 1f;
                if (keyboard.qKey.isPressed) move.y -= 1f;
                
                float speed = flySpeed;
                if (keyboard.shiftKey.isPressed) speed *= shiftMultiplier;
                
                if (move.sqrMagnitude > 0)
                {
                    move = move.normalized * speed * Time.deltaTime;
                    camTransform.position += camTransform.TransformDirection(move);
                }
            }
            
            // Rotasyonu uygula
            camTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
        else
        {
            if (isFlying)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                isFlying = false;
            }
        }

        // ================== ORTA TIK: PAN ==================
        if (middleClick && !rightClick)
        {
            if (!isPanning)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                isPanning = true;
            }
            
            // Pan hareketi (sağa/sola ve yukarı/aşağı kaydırma)
            Vector3 panOffset = camTransform.right * -mouseDelta.x * panSensitivity * 0.01f +
                               camTransform.up * -mouseDelta.y * panSensitivity * 0.01f;
            
            // Mesafeye göre pan hızını ayarla
            panOffset *= orbitDistance * 0.1f;
            
            camTransform.position += panOffset;
            orbitCenter += panOffset;
        }
        else
        {
            if (isPanning)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                isPanning = false;
            }
        }

        // ================== ALT + SOL TIK: ORBIT ==================
        if (altKey && leftClick && !rightClick && !middleClick)
        {
            if (!isOrbiting)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                isOrbiting = true;
                
                // Orbit mesafesini güncelle
                orbitDistance = Vector3.Distance(camTransform.position, orbitCenter);
            }
            
            // Orbit rotasyonu
            yaw += mouseDelta.x * orbitSensitivity * 0.1f;
            pitch += (invertY ? mouseDelta.y : -mouseDelta.y) * orbitSensitivity * 0.1f;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            
            // Yeni pozisyonu hesapla
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rotation * (Vector3.back * orbitDistance);
            camTransform.position = orbitCenter + offset;
            camTransform.rotation = rotation;
        }
        else
        {
            if (isOrbiting)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                isOrbiting = false;
            }
        }

        // ================== SCROLL: ZOOM ==================
        if (Mathf.Abs(scroll) > 0.01f && !rightClick)
        {
            float zoomAmount = scroll * scrollSpeed * 0.1f;
            
            // Hızlanma: uzaktayken daha hızlı zoom
            if (scrollAcceleration)
            {
                zoomAmount *= Mathf.Max(1f, orbitDistance * 0.1f);
            }
            
            // İleri/geri hareket
            camTransform.position += camTransform.forward * zoomAmount;
            
            // Orbit mesafesini güncelle
            orbitDistance = Mathf.Max(0.5f, orbitDistance - zoomAmount);
        }

        // ================== F: FOCUS ==================
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            FocusOnPivot();
        }
    }

    /// <summary>
    /// Pivot noktasına odaklan
    /// </summary>
    public void FocusOnPivot()
    {
        if (orbitPivot != null)
        {
            orbitCenter = orbitPivot.position;
        }
        
        // Kamerayı orbit mesafesine yerleştir
        Vector3 direction = (camTransform.position - orbitCenter).normalized;
        if (direction.sqrMagnitude < 0.01f)
            direction = -Vector3.forward;
        
        camTransform.position = orbitCenter + direction * defaultOrbitDistance;
        camTransform.LookAt(orbitCenter);
        
        // Açıları güncelle
        Vector3 euler = camTransform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
        if (pitch > 180f) pitch -= 360f;
        
        orbitDistance = defaultOrbitDistance;
        
        Debug.Log($"[FreeLook] Focused on pivot at {orbitCenter}");
    }

    /// <summary>
    /// Belirli bir objeye odaklan
    /// </summary>
    public void FocusOnObject(GameObject target)
    {
        if (target == null) return;
        
        // Objenin bounds'unu hesapla
        Bounds bounds = new Bounds(target.transform.position, Vector3.one);
        var renderers = target.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }
        
        orbitCenter = bounds.center;
        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        orbitDistance = size * 2f;
        
        // Kamerayı yerleştir
        Vector3 direction = (camTransform.position - orbitCenter).normalized;
        if (direction.sqrMagnitude < 0.01f)
            direction = -Vector3.forward;
        
        camTransform.position = orbitCenter + direction * orbitDistance;
        camTransform.LookAt(orbitCenter);
        
        // Açıları güncelle
        Vector3 euler = camTransform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
        if (pitch > 180f) pitch -= 360f;
        
        Debug.Log($"[FreeLook] Focused on {target.name}");
    }

    /// <summary>
    /// Aktifleştir
    /// </summary>
    public void Activate()
    {
        isActive = true;
        
        if (targetCamera == null)
            targetCamera = GetComponent<CinemachineCamera>();
        
        if (targetCamera != null)
        {
            camTransform = targetCamera.transform;
            InitializeFromCamera();
        }
        
        Debug.Log("[FreeLookCameraController] Activated - Unity Editor controls enabled");
    }

    /// <summary>
    /// Deaktifleştir
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        isFlying = false;
        isPanning = false;
        isOrbiting = false;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("[FreeLookCameraController] Deactivated");
    }

    /// <summary>
    /// Orbit pivot'unu ayarla
    /// </summary>
    public void SetOrbitPivot(Transform pivot)
    {
        orbitPivot = pivot;
        if (pivot != null)
        {
            orbitCenter = pivot.position;
        }
    }

    /// <summary>
    /// Hassasiyet ayarla
    /// </summary>
    public void SetSensitivity(float sensitivity)
    {
        lookSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
        orbitSensitivity = lookSensitivity;
    }

    /// <summary>
    /// Hareket hızını ayarla
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        flySpeed = Mathf.Max(1f, speed);
    }

    /// <summary>
    /// Y ekseni ters çevir
    /// </summary>
    public void SetInvertY(bool invert)
    {
        invertY = invert;
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!isActive) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 11;
        style.normal.textColor = Color.white;
        
        string mode = "Ready";
        if (isFlying) mode = "Flying (RMB+WASD)";
        else if (isPanning) mode = "Panning (MMB)";
        else if (isOrbiting) mode = "Orbiting (Alt+LMB)";
        
        string info = $"[FreeLook] {mode} | F=Focus | Scroll=Zoom";
        GUI.Label(new Rect(10, Screen.height - 25, 400, 20), info, style);
    }
#endif
}

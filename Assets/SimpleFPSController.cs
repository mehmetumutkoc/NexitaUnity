using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// Basit FPS kontrolcüsü. CameraController tarafından aktive edilir.
/// Sadece hareket ve mouse bakış işler.
/// </summary>
public class SimpleFPSController : MonoBehaviour
{
    [Header("Kamera Referansı")]
    [Tooltip("FPS için kullanılacak CinemachineCamera")]
    public CinemachineCamera fpsCamera;

    [Header("Hareket Ayarları")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.5f;

    [Header("Mouse Bakış Ayarları")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;

    [Header("Durum")]
    public bool isActive = false;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private Transform cameraTransform;

    void Start()
    {
        if (fpsCamera == null)
        {
            fpsCamera = GetComponentInChildren<CinemachineCamera>();
        }

        if (fpsCamera != null)
        {
            cameraTransform = fpsCamera.transform;
        }
    }

    void Update()
    {
        if (isActive)
        {
            HandleMouseLook();
            HandleMovement();
        }
    }

    /// <summary>
    /// FPS modunu aktifleştir (CameraController tarafından çağrılır)
    /// </summary>
    public void ActivateFPS()
    {
        isActive = true;

        // Mevcut rotasyonu al
        yRotation = transform.eulerAngles.y;
        xRotation = 0f;

        // Mouse'u kilitle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[SimpleFPSController] FPS Mode Activated");
    }

    /// <summary>
    /// FPS modunu deaktif et (CameraController tarafından çağrılır)
    /// </summary>
    public void DeactivateFPS()
    {
        isActive = false;

        // Mouse'u serbest bırak
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[SimpleFPSController] FPS Mode Deactivated");
    }

    void HandleMouseLook()
    {
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        yRotation += mouseDelta.x * mouseSensitivity * 0.1f;
        xRotation -= mouseDelta.y * mouseSensitivity * 0.1f;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        // GameObject'i yatay döndür
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        
        // Kamerayı dikey döndür
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    void HandleMovement()
    {
        if (Keyboard.current == null) return;

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.wKey.isPressed) vertical += 1f;
        if (Keyboard.current.sKey.isPressed) vertical -= 1f;
        if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
        if (Keyboard.current.dKey.isPressed) horizontal += 1f;

        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        moveDirection.Normalize();

        float currentSpeed = moveSpeed;
        if (Keyboard.current.leftShiftKey.isPressed)
        {
            currentSpeed *= sprintMultiplier;
        }

        transform.position += moveDirection * currentSpeed * Time.deltaTime;
    }

    void OnDisable()
    {
        if (isActive)
        {
            DeactivateFPS();
        }
    }
}


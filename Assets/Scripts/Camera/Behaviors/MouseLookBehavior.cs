using UnityEngine;
using UnityEngine.InputSystem;

namespace CameraSystem.Behaviors
{
    /// <summary>
    /// Mouse ile bakış davranışı.
    /// FPS modu için kullanılır.
    /// </summary>
    [System.Serializable]
    public class MouseLookBehavior : ICameraBehavior
    {
        [Header("Referanslar")]
        [SerializeField] private Transform bodyTransform;  // Yatay dönüş için
        [SerializeField] private Transform cameraTransform; // Dikey dönüş için
        
        [Header("Ayarlar")]
        [SerializeField] private float sensitivity = 2f;
        [SerializeField] private float maxLookAngle = 80f;

        private float xRotation = 0f;
        private float yRotation = 0f;

        public bool IsEnabled { get; set; } = true;
        
        public Transform BodyTransform
        {
            get => bodyTransform;
            set => bodyTransform = value;
        }
        
        public Transform CameraTransform
        {
            get => cameraTransform;
            set => cameraTransform = value;
        }
        
        public float Sensitivity
        {
            get => sensitivity;
            set => sensitivity = value;
        }
        
        public float MaxLookAngle
        {
            get => maxLookAngle;
            set => maxLookAngle = value;
        }

        public void OnEnable()
        {
            // Mevcut rotasyonu al
            if (bodyTransform != null)
            {
                yRotation = bodyTransform.eulerAngles.y;
            }
            xRotation = 0f;
        }

        public void OnDisable() { }

        public void OnUpdate()
        {
            if (!IsEnabled) return;
            if (Mouse.current == null) return;
            if (bodyTransform == null) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            yRotation += mouseDelta.x * sensitivity * 0.1f;
            xRotation -= mouseDelta.y * sensitivity * 0.1f;
            xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

            // Body'yi yatay döndür
            bodyTransform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            
            // Kamerayı dikey döndür
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }
        }
    }
}

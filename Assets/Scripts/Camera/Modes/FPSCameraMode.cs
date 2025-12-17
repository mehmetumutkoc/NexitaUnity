using UnityEngine;
using CameraSystem.Behaviors;

namespace CameraSystem.Modes
{
    /// <summary>
    /// FPS kamera modu.
    /// WASD hareket + Mouse bakış.
    /// </summary>
    public class FPSCameraMode : BaseCameraMode
    {
        [Header("FPS Referansları")]
        [SerializeField] private Transform cameraTransform;
        
        private MovementBehavior movementBehavior;
        private MouseLookBehavior mouseLookBehavior;

        public override string ModeName => "FPS";

        protected override void Awake()
        {
            base.Awake();
            
            // Movement behavior oluştur
            movementBehavior = new MovementBehavior();
            movementBehavior.Target = transform;
            
            // MouseLook behavior oluştur
            mouseLookBehavior = new MouseLookBehavior();
            mouseLookBehavior.BodyTransform = transform;
            mouseLookBehavior.CameraTransform = cameraTransform;
            
            // Config'den ayarları al
            if (config != null)
            {
                movementBehavior.MoveSpeed = config.moveSpeed;
                movementBehavior.SprintMultiplier = config.sprintMultiplier;
                mouseLookBehavior.Sensitivity = config.mouseSensitivity;
                mouseLookBehavior.MaxLookAngle = config.maxLookAngle;
            }
            
            AddBehavior(movementBehavior);
            AddBehavior(mouseLookBehavior);
        }

        private void Start()
        {
            // Camera transform otomatik bul
            if (cameraTransform == null && cinemachineCamera != null)
            {
                cameraTransform = cinemachineCamera.transform;
                mouseLookBehavior.CameraTransform = cameraTransform;
            }
        }

        public override void HandleInput()
        {
            // FPS'de özel input yok - behavior'lar hallediyor
        }
    }
}

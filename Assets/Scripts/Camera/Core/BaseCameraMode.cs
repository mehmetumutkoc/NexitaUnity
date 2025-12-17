using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

namespace CameraSystem
{
    /// <summary>
    /// Tüm kamera modlarının base sınıfı.
    /// Ortak işlevselliği sağlar.
    /// </summary>
    public abstract class BaseCameraMode : MonoBehaviour, ICameraMode
    {
        [Header("Kamera")]
        [SerializeField] protected CinemachineCamera cinemachineCamera;
        
        [Header("Konfigürasyon")]
        [SerializeField] protected CameraModeConfig config;

        protected List<ICameraBehavior> behaviors = new List<ICameraBehavior>();
        protected bool isActive = false;

        public abstract string ModeName { get; }
        public CinemachineCamera Camera => cinemachineCamera;
        public CameraModeConfig Config => config;
        public bool IsActive => isActive;

        protected virtual void Awake()
        {
            // Alt sınıflar behavior'ları burada ekleyebilir
        }

        public virtual void Activate()
        {
            isActive = true;
            
            // Priority ayarla
            if (cinemachineCamera != null && config != null)
            {
                cinemachineCamera.Priority = config.activePriority;
            }
            
            // Cursor ayarla
            if (config != null)
            {
                Cursor.lockState = config.lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !config.hideCursor;
            }
            
            // Behavior'ları aktifle
            foreach (var behavior in behaviors)
            {
                behavior.IsEnabled = true;
                behavior.OnEnable();
            }
            
            // Event tetikle
            CameraEvents.TriggerModeActivated(this);
            
            Debug.Log($"[{ModeName}] Activated");
        }

        public virtual void Deactivate()
        {
            isActive = false;
            
            // Priority ayarla
            if (cinemachineCamera != null && config != null)
            {
                cinemachineCamera.Priority = config.inactivePriority;
            }
            
            // Behavior'ları deaktif et
            foreach (var behavior in behaviors)
            {
                behavior.OnDisable();
                behavior.IsEnabled = false;
            }
            
            // Event tetikle
            CameraEvents.TriggerModeDeactivated(this);
            
            Debug.Log($"[{ModeName}] Deactivated");
        }

        public virtual void OnUpdate()
        {
            if (!isActive) return;
            
            // Behavior'ları güncelle
            foreach (var behavior in behaviors)
            {
                if (behavior.IsEnabled)
                {
                    behavior.OnUpdate();
                }
            }
        }

        public virtual void HandleInput()
        {
            // Alt sınıflar override edebilir
        }

        protected void AddBehavior(ICameraBehavior behavior)
        {
            if (!behaviors.Contains(behavior))
            {
                behaviors.Add(behavior);
            }
        }

        protected void RemoveBehavior(ICameraBehavior behavior)
        {
            behaviors.Remove(behavior);
        }
    }
}

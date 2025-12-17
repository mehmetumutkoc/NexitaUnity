using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections.Generic;
using CameraSystem.Modes;

namespace CameraSystem
{
    /// <summary>
    /// Merkezi kamera yönetim sistemi.
    /// Tüm kamera modlarını yönetir ve aralarında geçiş yapar.
    /// </summary>
    public class ModularCameraController : MonoBehaviour
    {
        [Header("Registered Modes")]
        [SerializeField] private List<BaseCameraMode> registeredModes = new List<BaseCameraMode>();
        
        [Header("Camera Brain")]
        [SerializeField] private CinemachineBrain brain;

        [Header("State")]
        [SerializeField] private int currentModeIndex = 0;
        
        private ICameraMode currentMode;
        private ICameraMode previousMode;

        public ICameraMode CurrentMode => currentMode;
        public ICameraMode PreviousMode => previousMode;
        public IReadOnlyList<BaseCameraMode> RegisteredModes => registeredModes;

        void Start()
        {
            // Brain otomatik bul
            if (brain == null)
            {
                brain = FindObjectOfType<CinemachineBrain>();
            }

            // Sahnedeki tüm modları otomatik bul
            if (registeredModes.Count == 0)
            {
                registeredModes.AddRange(FindObjectsOfType<BaseCameraMode>());
                Debug.Log($"[ModularCameraController] Found {registeredModes.Count} camera modes");
            }

            // İlk modu aktifle
            if (registeredModes.Count > 0)
            {
                SetMode(registeredModes[0]);
            }
        }

        void Update()
        {
            HandleInput();
            
            // Aktif modun update'ini çağır
            currentMode?.OnUpdate();
            currentMode?.HandleInput();
        }

        void HandleInput()
        {
            if (Keyboard.current == null) return;

            // Numpad tuşları ile mod değiştir
            for (int i = 0; i < registeredModes.Count && i < 9; i++)
            {
                var mode = registeredModes[i];
                if (mode.Config != null && mode.Config.activationKey != Key.None)
                {
                    if (Keyboard.current[mode.Config.activationKey].wasPressedThisFrame)
                    {
                        SetMode(mode);
                        return;
                    }
                }
            }

            // 1-2-3 fallback
            if (Keyboard.current.digit1Key.wasPressedThisFrame && registeredModes.Count > 0)
            {
                SetMode(registeredModes[0]);
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame && registeredModes.Count > 1)
            {
                SetMode(registeredModes[1]);
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame && registeredModes.Count > 2)
            {
                SetMode(registeredModes[2]);
            }
            // Escape - ilk moda dön (genellikle Building)
            else if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (registeredModes.Count > 0 && currentMode != registeredModes[0])
                {
                    SetMode(registeredModes[0]);
                }
            }
        }

        /// <summary>
        /// Belirli bir moda geç
        /// </summary>
        public void SetMode(ICameraMode newMode)
        {
            if (newMode == null || newMode == currentMode) return;

            previousMode = currentMode;

            // Event: Geçiş başlıyor
            CameraEvents.TriggerModeTransitionStart(previousMode, newMode);

            // Önceki modu deaktif et
            if (currentMode != null)
            {
                currentMode.Deactivate();
            }

            // Blend ayarla
            if (brain != null && newMode.Config != null)
            {
                brain.DefaultBlend = new CinemachineBlendDefinition(
                    newMode.Config.blendStyle,
                    newMode.Config.blendDuration
                );
            }

            // Yeni modu aktifle
            currentMode = newMode;
            currentMode.Activate();

            // Index güncelle
            for (int i = 0; i < registeredModes.Count; i++)
            {
                if (registeredModes[i] == newMode)
                {
                    currentModeIndex = i;
                    break;
                }
            }

            // Event: Geçiş tamamlandı
            CameraEvents.TriggerModeTransitionComplete(currentMode);

            Debug.Log($"[ModularCameraController] Mode changed to: {currentMode.ModeName}");
        }

        /// <summary>
        /// İsme göre mod bul ve geç
        /// </summary>
        public void SetModeByName(string modeName)
        {
            var mode = registeredModes.Find(m => m.ModeName == modeName);
            if (mode != null)
            {
                SetMode(mode);
            }
            else
            {
                Debug.LogWarning($"[ModularCameraController] Mode not found: {modeName}");
            }
        }

        /// <summary>
        /// Sonraki moda geç
        /// </summary>
        public void NextMode()
        {
            if (registeredModes.Count == 0) return;
            
            int nextIndex = (currentModeIndex + 1) % registeredModes.Count;
            SetMode(registeredModes[nextIndex]);
        }

        /// <summary>
        /// Önceki moda geç
        /// </summary>
        public void GoToPreviousMode()
        {
            if (registeredModes.Count == 0) return;
            
            int prevIndex = currentModeIndex - 1;
            if (prevIndex < 0) prevIndex = registeredModes.Count - 1;
            SetMode(registeredModes[prevIndex]);
        }

        /// <summary>
        /// Mod kaydet
        /// </summary>
        public void RegisterMode(BaseCameraMode mode)
        {
            if (!registeredModes.Contains(mode))
            {
                registeredModes.Add(mode);
                Debug.Log($"[ModularCameraController] Registered mode: {mode.ModeName}");
            }
        }

        /// <summary>
        /// Mod kaydını sil
        /// </summary>
        public void UnregisterMode(BaseCameraMode mode)
        {
            registeredModes.Remove(mode);
        }
    }
}

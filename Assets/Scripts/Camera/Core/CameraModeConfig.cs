using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace CameraSystem
{
    /// <summary>
    /// Kamera modu konfigürasyonu.
    /// Her mod için ayrı bir ScriptableObject oluşturulabilir.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraModeConfig", menuName = "Camera/Mode Config")]
    public class CameraModeConfig : ScriptableObject
    {
        [Header("Temel Ayarlar")]
        [Tooltip("Mod ismi")]
        public string modeName = "New Mode";
        
        [Tooltip("Bu modu aktifleştirmek için kullanılacak tuş")]
        public Key activationKey = Key.None;

        [Header("Priority")]
        [Tooltip("Aktifken kamera priority değeri")]
        public int activePriority = 20;
        
        [Tooltip("İnaktifken kamera priority değeri")]
        public int inactivePriority = 0;

        [Header("Blend Ayarları")]
        [Tooltip("Bu moda geçiş süresi")]
        public float blendDuration = 1f;
        
        [Tooltip("Blend eğrisi")]
        public CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut;

        [Header("Cursor Ayarları")]
        [Tooltip("Bu modda cursor kilitli mi?")]
        public bool lockCursor = false;
        
        [Tooltip("Bu modda cursor gizli mi?")]
        public bool hideCursor = false;

        [Header("Hareket Ayarları (FPS için)")]
        [Tooltip("Hareket hızı")]
        public float moveSpeed = 5f;
        
        [Tooltip("Sprint çarpanı")]
        public float sprintMultiplier = 1.5f;
        
        [Tooltip("Mouse hassasiyeti")]
        public float mouseSensitivity = 2f;
        
        [Tooltip("Yukarı/aşağı bakış sınırı")]
        public float maxLookAngle = 80f;

        [Header("Dolly Ayarları")]
        [Tooltip("Spline hareket hızı")]
        public float dollySpeed = 0.2f;
    }
}

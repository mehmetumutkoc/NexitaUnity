using Unity.Cinemachine;

namespace CameraSystem
{
    /// <summary>
    /// Tüm kamera modlarının implemente etmesi gereken interface.
    /// </summary>
    public interface ICameraMode
    {
        /// <summary>
        /// Mod ismi (Building, Dolly, FPS vb.)
        /// </summary>
        string ModeName { get; }
        
        /// <summary>
        /// Bu moda ait Cinemachine kamera
        /// </summary>
        CinemachineCamera Camera { get; }
        
        /// <summary>
        /// ScriptableObject konfigürasyonu
        /// </summary>
        CameraModeConfig Config { get; }
        
        /// <summary>
        /// Mod aktif mi?
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Mod aktifleştirildiğinde çağrılır
        /// </summary>
        void Activate();
        
        /// <summary>
        /// Mod deaktif edildiğinde çağrılır
        /// </summary>
        void Deactivate();
        
        /// <summary>
        /// Her frame çağrılır (sadece aktifken)
        /// </summary>
        void OnUpdate();
        
        /// <summary>
        /// Mod için özel input handling
        /// </summary>
        void HandleInput();
    }
}

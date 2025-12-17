using System;

namespace CameraSystem
{
    /// <summary>
    /// Kamera sistemi eventleri.
    /// Diğer sistemler bu eventlere subscribe olabilir.
    /// </summary>
    public static class CameraEvents
    {
        /// <summary>
        /// Bir kamera modu aktifleştirildiğinde tetiklenir.
        /// </summary>
        public static event Action<ICameraMode> OnModeActivated;
        
        /// <summary>
        /// Bir kamera modu deaktif edildiğinde tetiklenir.
        /// </summary>
        public static event Action<ICameraMode> OnModeDeactivated;
        
        /// <summary>
        /// Mod geçişi başladığında tetiklenir (from, to).
        /// </summary>
        public static event Action<ICameraMode, ICameraMode> OnModeTransitionStart;
        
        /// <summary>
        /// Mod geçişi tamamlandığında tetiklenir.
        /// </summary>
        public static event Action<ICameraMode> OnModeTransitionComplete;

        // Event trigger metodları
        public static void TriggerModeActivated(ICameraMode mode)
        {
            OnModeActivated?.Invoke(mode);
        }

        public static void TriggerModeDeactivated(ICameraMode mode)
        {
            OnModeDeactivated?.Invoke(mode);
        }

        public static void TriggerModeTransitionStart(ICameraMode from, ICameraMode to)
        {
            OnModeTransitionStart?.Invoke(from, to);
        }

        public static void TriggerModeTransitionComplete(ICameraMode mode)
        {
            OnModeTransitionComplete?.Invoke(mode);
        }

        /// <summary>
        /// Tüm event subscriptionlarını temizle (test için)
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnModeActivated = null;
            OnModeDeactivated = null;
            OnModeTransitionStart = null;
            OnModeTransitionComplete = null;
        }
    }
}

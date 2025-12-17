using UnityEngine;

namespace CameraSystem.Modes
{
    /// <summary>
    /// Bina genel görünüm kamera modu.
    /// Statik kamera, kullanıcı kontrolü yok.
    /// </summary>
    public class BuildingCameraMode : BaseCameraMode
    {
        public override string ModeName => "Building";

        protected override void Awake()
        {
            base.Awake();
            // Building modunda behavior yok - statik kamera
        }

        public override void HandleInput()
        {
            // Building modunda özel input yok
        }
    }
}

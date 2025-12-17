using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using CameraSystem.Behaviors;

namespace CameraSystem.Modes
{
    /// <summary>
    /// Dolly/Spline kamera modu.
    /// Spline üzerinde hareket eder.
    /// </summary>
    public class DollyCameraMode : BaseCameraMode
    {
        [Header("Dolly Ayarları")]
        [SerializeField] private CinemachineSplineCart splineCart;
        
        private DollyBehavior dollyBehavior;

        public override string ModeName => "Dolly";
        
        public DollyBehavior DollyBehavior => dollyBehavior;

        protected override void Awake()
        {
            base.Awake();
            
            // Dolly behavior oluştur
            dollyBehavior = new DollyBehavior();
            dollyBehavior.SplineCart = splineCart;
            dollyBehavior.SetCoroutineRunner(this);
            
            if (config != null)
            {
                dollyBehavior.Speed = config.dollySpeed;
            }
            
            AddBehavior(dollyBehavior);
        }

        public override void Activate()
        {
            base.Activate();
            
            // Dolly'yi başlangıca al
            dollyBehavior.ResetToStart();
        }

        public override void HandleInput()
        {
            if (!isActive) return;
            
            // Space ile spline hareketi başlat
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (!dollyBehavior.IsMoving)
                {
                    dollyBehavior.StartMovement();
                }
            }
        }

        /// <summary>
        /// Spline hareketini dışarıdan başlat
        /// </summary>
        public void StartSplineMovement()
        {
            if (isActive && !dollyBehavior.IsMoving)
            {
                dollyBehavior.StartMovement();
            }
        }
    }
}

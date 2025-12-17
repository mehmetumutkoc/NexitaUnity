using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

namespace CameraSystem.Behaviors
{
    /// <summary>
    /// Spline üzerinde hareket davranışı.
    /// Dolly kamera modu için kullanılır.
    /// </summary>
    [System.Serializable]
    public class DollyBehavior : ICameraBehavior
    {
        [Header("Referanslar")]
        [SerializeField] private CinemachineSplineCart splineCart;
        [SerializeField] private MonoBehaviour coroutineRunner; // Coroutine için
        
        [Header("Ayarlar")]
        [SerializeField] private float speed = 0.2f;
        [SerializeField] private bool autoStart = false;

        private float progress = 0f;
        private bool isMoving = false;
        private Coroutine moveCoroutine;

        public bool IsEnabled { get; set; } = true;
        
        public CinemachineSplineCart SplineCart
        {
            get => splineCart;
            set => splineCart = value;
        }
        
        public float Speed
        {
            get => speed;
            set => speed = value;
        }
        
        public float Progress => progress;
        public bool IsMoving => isMoving;

        public void SetCoroutineRunner(MonoBehaviour runner)
        {
            coroutineRunner = runner;
        }

        public void OnEnable()
        {
            // Başlangıca al
            ResetToStart();
            
            if (autoStart)
            {
                StartMovement();
            }
        }

        public void OnDisable()
        {
            StopMovement();
        }

        public void OnUpdate()
        {
            // Coroutine ile çalışıyor, burada bir şey yapmaya gerek yok
        }

        public void ResetToStart()
        {
            progress = 0f;
            if (splineCart != null)
            {
                splineCart.SplinePosition = 0f;
            }
        }

        public void StartMovement()
        {
            if (isMoving || coroutineRunner == null) return;
            
            progress = 0f;
            moveCoroutine = coroutineRunner.StartCoroutine(MoveAlongPath());
        }

        public void StopMovement()
        {
            if (moveCoroutine != null && coroutineRunner != null)
            {
                coroutineRunner.StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }
            isMoving = false;
        }

        private IEnumerator MoveAlongPath()
        {
            isMoving = true;
            
            while (progress < 1f)
            {
                progress += speed * Time.deltaTime;
                progress = Mathf.Clamp01(progress);
                
                if (splineCart != null)
                {
                    splineCart.SplinePosition = progress;
                }
                
                yield return null;
            }
            
            isMoving = false;
            Debug.Log("[DollyBehavior] Spline animation completed");
        }
    }
}

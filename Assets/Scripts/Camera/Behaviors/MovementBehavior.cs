using UnityEngine;
using UnityEngine.InputSystem;

namespace CameraSystem.Behaviors
{
    /// <summary>
    /// WASD hareket davranışı.
    /// FPS ve diğer serbest hareket modları için kullanılır.
    /// </summary>
    [System.Serializable]
    public class MovementBehavior : ICameraBehavior
    {
        [Header("Referanslar")]
        [SerializeField] private Transform target;
        
        [Header("Ayarlar")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.5f;

        public bool IsEnabled { get; set; } = true;
        
        public Transform Target
        {
            get => target;
            set => target = value;
        }
        
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }
        
        public float SprintMultiplier
        {
            get => sprintMultiplier;
            set => sprintMultiplier = value;
        }

        public void OnEnable() { }
        public void OnDisable() { }

        public void OnUpdate()
        {
            if (!IsEnabled || target == null) return;
            if (Keyboard.current == null) return;

            float horizontal = 0f;
            float vertical = 0f;

            if (Keyboard.current.wKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed) vertical -= 1f;
            if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed) horizontal += 1f;

            Vector3 moveDirection = target.right * horizontal + target.forward * vertical;
            moveDirection.Normalize();

            float currentSpeed = moveSpeed;
            if (Keyboard.current.leftShiftKey.isPressed)
            {
                currentSpeed *= sprintMultiplier;
            }

            target.position += moveDirection * currentSpeed * Time.deltaTime;
        }
    }
}

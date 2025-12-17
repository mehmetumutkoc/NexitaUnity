namespace CameraSystem
{
    /// <summary>
    /// Kamera davranışları için interface.
    /// Modlar birden fazla behavior kullanabilir.
    /// </summary>
    public interface ICameraBehavior
    {
        /// <summary>
        /// Behavior aktif mi?
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// Behavior başlatıldığında çağrılır
        /// </summary>
        void OnEnable();
        
        /// <summary>
        /// Behavior durdurulduğunda çağrılır
        /// </summary>
        void OnDisable();
        
        /// <summary>
        /// Her frame çağrılır (sadece enabled iken)
        /// </summary>
        void OnUpdate();
    }
}

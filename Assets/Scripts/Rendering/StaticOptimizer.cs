using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hareket edecek objeleri otomatik olarak non-static yapar,
/// geri kalanları static yaparak batching sağlar.
/// </summary>
public class StaticOptimizer : MonoBehaviour
{
    [Header("Hareket Edecek Objeler")]
    [Tooltip("Bu objelerin static flag'i kapatılacak (hareket edebilir).")]
    public Transform[] movableObjects;

    [Header("AttractionSequence Referansı")]
    [Tooltip("Eğer atanırsa, shells otomatik olarak movable listeye eklenir.")]
    public AttractionSequence attractionSequence;

    [Header("Ayarlar")]
    [Tooltip("True ise, movable olmayan TÜM child objeleri static yapar.")]
    public bool makeOthersStatic = true;

    [Tooltip("Static yapılacak root objeler (movable olmayanlar için).")]
    public Transform[] staticRoots;

    [Header("Gölge Optimizasyonu")]
    [Tooltip("True ise, küçük objeler için gölge kapatılır.")]
    public bool optimizeShadows = true;

    [Tooltip("Bu boyutun altındaki objeler gölge atmaz (bounds magnitude).")]
    public float shadowSizeThreshold = 1.0f;

    [Header("Performans")]
    [Tooltip("Her frame'de işlenecek maksimum obje sayısı (lag önleme).")]
    public int objectsPerFrame = 500;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private HashSet<Transform> movableSet = new HashSet<Transform>();
    private bool isOptimizing = false;

    void Awake()
    {
        // Lag önlemek için coroutine ile yap
        StartCoroutine(OptimizeSceneAsync());
    }

    [ContextMenu("Optimize Scene Now")]
    public void OptimizeScene()
    {
        StartCoroutine(OptimizeSceneAsync());
    }

    /// <summary>
    /// Lag önlemek için optimizasyonu birden fazla frame'e yayar.
    /// </summary>
    private IEnumerator OptimizeSceneAsync()
    {
        if (isOptimizing)
            yield break;

        isOptimizing = true;

        // 1. Movable objeleri topla (hızlı, tek frame)
        CollectMovableObjects();
        yield return null;

        // 2. Static flag'leri ayarla (yavaş, async)
        if (makeOthersStatic)
            yield return StartCoroutine(SetStaticForNonMovablesAsync());

        // 3. Gölge optimizasyonu (yavaş, async)
        if (optimizeShadows)
            yield return StartCoroutine(OptimizeShadowCastersAsync());

        if (showDebugLogs)
            Debug.Log($"[StaticOptimizer] Optimizasyon tamamlandı - Movable: {movableSet.Count} obje");

        isOptimizing = false;
    }

    private void CollectMovableObjects()
    {
        movableSet.Clear();

        // Manuel listeden ekle
        if (movableObjects != null)
        {
            foreach (var t in movableObjects)
            {
                if (t != null)
                    AddWithChildren(t);
            }
        }

        // AttractionSequence'dan shells ekle
        if (attractionSequence != null)
        {
            // Core her zaman hareket eder
            if (attractionSequence.core != null)
                AddWithChildren(attractionSequence.core);

            // Shells dizisi
            if (attractionSequence.shells != null)
            {
                foreach (var shell in attractionSequence.shells)
                {
                    if (shell != null)
                        AddWithChildren(shell);
                }
            }

            // Camera rig
            if (attractionSequence.cameraRig != null)
                AddWithChildren(attractionSequence.cameraRig);
        }

        // Movable objeleri non-static yap (bu hızlı, tek frame'de yapılabilir)
        foreach (var t in movableSet)
        {
            if (t != null && t.gameObject.isStatic)
            {
                t.gameObject.isStatic = false;
            }
        }

        if (showDebugLogs)
            Debug.Log($"[StaticOptimizer] {movableSet.Count} obje movable olarak işaretlendi");
    }

    private void AddWithChildren(Transform root)
    {
        if (root == null) return;
        
        movableSet.Add(root);
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            movableSet.Add(child);
        }
    }

    private IEnumerator SetStaticForNonMovablesAsync()
    {
        if (staticRoots == null) yield break;

        int processedThisFrame = 0;

        foreach (var root in staticRoots)
        {
            if (root == null) continue;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            
            foreach (Transform t in children)
            {
                // Movable değilse static yap
                if (!movableSet.Contains(t) && !t.gameObject.isStatic)
                {
                    t.gameObject.isStatic = true;
                }

                processedThisFrame++;
                if (processedThisFrame >= objectsPerFrame)
                {
                    processedThisFrame = 0;
                    yield return null; // Sonraki frame'e geç
                }
            }
        }
    }

    private IEnumerator OptimizeShadowCastersAsync()
    {
        // Sahnedeki tüm renderer'ları bul
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        int optimizedCount = 0;
        int processedThisFrame = 0;

        foreach (var r in allRenderers)
        {
            if (r == null) continue;

            // Movable objeler için gölge ayarına dokunma
            if (movableSet.Contains(r.transform))
                continue;

            // Küçük objeler için gölgeyi kapat
            float size = r.bounds.size.magnitude;
            if (size < shadowSizeThreshold)
            {
                if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    optimizedCount++;
                }
            }

            processedThisFrame++;
            if (processedThisFrame >= objectsPerFrame)
            {
                processedThisFrame = 0;
                yield return null; // Sonraki frame'e geç
            }
        }

        if (showDebugLogs && optimizedCount > 0)
            Debug.Log($"[StaticOptimizer] {optimizedCount} küçük objenin gölgesi kapatıldı");
    }

    /// <summary>
    /// Runtime'da yeni bir objeyi movable olarak işaretler.
    /// Static batching zaten yapıldıysa, bu obje batch'den çıkmış olur.
    /// </summary>
    public void MarkAsMovable(Transform obj)
    {
        if (obj == null) return;

        AddWithChildren(obj);
        obj.gameObject.isStatic = false;

        // Tüm child'ları da non-static yap
        foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.isStatic = false;
        }
    }

    /// <summary>
    /// Runtime'da dinamik olarak static batching uygular.
    /// Dikkat: Bu işlem geri alınamaz!
    /// </summary>
    [ContextMenu("Apply Runtime Static Batching")]
    public void ApplyRuntimeStaticBatching()
    {
        StartCoroutine(ApplyRuntimeStaticBatchingAsync());
    }

    private IEnumerator ApplyRuntimeStaticBatchingAsync()
    {
        if (staticRoots == null || staticRoots.Length == 0)
        {
            Debug.LogWarning("[StaticOptimizer] staticRoots boş, batching uygulanamadı.");
            yield break;
        }

        foreach (var root in staticRoots)
        {
            if (root == null) continue;

            // Sadece non-movable objeleri topla
            List<GameObject> toCombine = new List<GameObject>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!movableSet.Contains(t) && t.gameObject.isStatic)
                {
                    toCombine.Add(t.gameObject);
                }
            }

            if (toCombine.Count > 0)
            {
                StaticBatchingUtility.Combine(toCombine.ToArray(), root.gameObject);
                if (showDebugLogs)
                    Debug.Log($"[StaticOptimizer] Runtime batching: {root.name} ({toCombine.Count} obje)");
            }

            yield return null; // Her root için bir frame bekle
        }
    }
}


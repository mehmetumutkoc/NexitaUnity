using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class AttractionSequence : MonoBehaviour
{
    [Header("Core")]
    public Transform core;
    [Tooltip("Manuel target. Eğer autoTarget açıksa bu otomatik hesaplanır.")]
    public Transform target;
    public float coreMoveDuration = 2f;

    [Header("Auto Target (Core-Camera arası boş alan)")]
    [Tooltip("True ise target pozisyonu otomatik hesaplanır.")]
    public bool autoTarget = true;
    [Tooltip("Core'dan kameraya doğru bu oran kadar ilerle (0=core, 1=kamera).")]
    [Range(0.1f, 0.9f)]
    public float autoTargetRatio = 0.5f;
    [Tooltip("Boş alan kontrolü için küre yarıçapı.")]
    public float autoTargetCheckRadius = 1.0f;
    [Tooltip("Boş alan bulunamazsa en yakın uygun noktayı ara.")]
    public bool autoTargetFindNearest = true;
    [Tooltip("Arama adım sayısı (core->camera arası).")]
    public int autoTargetSearchSteps = 10;
    [Tooltip("AutoTarget hesaplamasında ignore edilecek layer'lar.")]
    public LayerMask autoTargetIgnoreMask;

    [Header("Shells")]
    public Transform[] shells;
    public float shellScatterDistance = 2.5f;
    public float shellScatterDuration = 0.8f;
    public float shellUpForce = 1.2f;
    [Tooltip("Delay between each shell's scatter/return start.")]
    public float shellStagger = 0.03f;
    [Tooltip("Extra multiplier for how far shells move on each axis (X,Y,Z).")]
    public Vector3 shellScatterAxisMultiplier = new Vector3(1.3f, 1.3f, 1.3f);

    [Header("Obstacle Raycast → Shells")]
    [Tooltip("If true, raycast from core to target and add hit objects into the moving shell list.")]
    public bool collectShellsFromRaycast = true;
    public LayerMask obstacleMask = ~0;
    public QueryTriggerInteraction obstacleTriggerInteraction = QueryTriggerInteraction.Ignore;
    [Tooltip("If true, moved objects are pushed sideways so the ray line clears.")]
    public bool avoidRayDirection = true;

    [Header("Shell Direction Bias")]
    [Tooltip("Preferred direction (XYZ). Examples: (1,0,0)=+X, (-1,0,0)=-X, (0,0,1)=+Z. Leave (0,0,0) for no bias.")]
    public Vector3 shellDirectionBias = Vector3.zero;
    [Tooltip("If true, shellDirectionBias is treated as parent-local direction; otherwise world direction.")]
    public bool shellBiasInParentSpace = true;
    [Range(0f, 5f)]
    [Tooltip("How strongly the bias dominates the scatter direction. 0 = no bias.")]
    public float shellBiasStrength = 1.5f;
    [Tooltip("If true, scatter direction is horizontal (XZ). Vertical movement is controlled by shellUpForce.")]
    public bool shellHorizontalOnly = true;

    [Header("Camera")]
    public Transform cameraRig;          // Kamera veya parent rig
    public Vector3 cameraOffset = new Vector3(0, 2.5f, -4f);
    public float cameraMoveDuration = 2.2f;
    public Ease cameraEase = Ease.InOutSine;

    [Header("Tweaks")]
    public Ease coreEase = Ease.InOutCubic;
    public Ease shellEase = Ease.OutBack;

    [Header("Focus State")]
    public bool isFocused;

    [Header("Optimization")]
    [Tooltip("Opsiyonel: Raycast ile bulunan objeler otomatik non-static yapılır.")]
    public StaticOptimizer staticOptimizer;

    [Header("Defaults")]
    [Tooltip("If true, default (initial) transforms are captured once on Start.")]
    public bool captureDefaultsOnStart = true;

    // Runtime hesaplanan target pozisyonu
    private Vector3 calculatedTargetPosition;

    private Sequence activeSequence;

    private struct TransformState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
    }

    private struct ShellState
    {
        public Transform shell;
        public Transform parent;
        public int siblingIndex;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
    }

    private TransformState coreState;
    private TransformState cameraState;
    private ShellState[] shellStates;
    private bool hasDefaultState;

    private readonly List<ShellState> runtimeShellStates = new List<ShellState>();
    private readonly HashSet<Transform> runtimeShellSet = new HashSet<Transform>();

    void Start()
    {
        if (captureDefaultsOnStart)
            CaptureDefaults();
    }

    [ContextMenu("Capture Defaults Now")]
    public void CaptureDefaults()
    {
        if (!core)
            return;

        hasDefaultState = true;

        coreState = new TransformState
        {
            position = core.position,
            rotation = core.rotation,
            localScale = core.localScale
        };

        if (cameraRig != null)
        {
            cameraState = new TransformState
            {
                position = cameraRig.position,
                rotation = cameraRig.rotation,
                localScale = cameraRig.localScale
            };
        }

        if (shells == null)
        {
            shellStates = null;
            return;
        }

        shellStates = new ShellState[shells.Length];
        for (int i = 0; i < shells.Length; i++)
        {
            Transform s = shells[i];
            if (s == null)
                continue;

            shellStates[i] = new ShellState
            {
                shell = s,
                parent = s.parent,
                siblingIndex = s.GetSiblingIndex(),
                localPosition = s.localPosition,
                localRotation = s.localRotation,
                localScale = s.localScale
            };
        }
    }

    void OnDisable()
    {
        KillActive();
    }

    void OnDestroy()
    {
        KillActive();
    }

    private void KillActive()
    {
        if (activeSequence != null)
        {
            activeSequence.Kill();
            activeSequence = null;
        }
    }

    public void StartSequence()
    {
        BeginFocus();
    }

    public void ToggleFocus()
    {
        if (isFocused)
            EndFocus();
        else
            BeginFocus();
    }

    public void BeginFocus()
    {
        if (!core)
            return;

        // AutoTarget aktifse pozisyonu hesapla
        if (autoTarget)
        {
            calculatedTargetPosition = CalculateAutoTargetPosition();
        }
        else if (target != null)
        {
            calculatedTargetPosition = target.position;
        }
        else
        {
            Debug.LogWarning("[AttractionSequence] Target yok ve autoTarget kapalı!");
            return;
        }

        if (!hasDefaultState)
            CaptureDefaults();

        RefreshRuntimeShellsFromRaycast();
        KillActive();

        isFocused = true;

        activeSequence = DOTween.Sequence().SetTarget(this);

        // 1) Shells fly out first
        activeSequence.Append(BuildShellScatterSequence());

        // 2) Core pulls to focus point + camera moves in
        activeSequence.Append(BuildCoreAndCameraInSequence());

        activeSequence.OnKill(() => activeSequence = null);
    }

    /// <summary>
    /// Core ve Camera arasında boş alan bularak target pozisyonu hesaplar.
    /// </summary>
    private Vector3 CalculateAutoTargetPosition()
    {
        if (core == null)
            return Vector3.zero;

        // Kamera pozisyonu (yoksa core'un arkasını kullan)
        Vector3 cameraPos;
        if (cameraRig != null)
            cameraPos = cameraRig.position;
        else
            cameraPos = core.position - core.forward * 5f; // Varsayılan: core'un 5m arkası

        Vector3 corePos = core.position;
        Vector3 direction = (cameraPos - corePos).normalized;
        float totalDistance = Vector3.Distance(corePos, cameraPos);

        // İdeal pozisyon: core'dan kameraya doğru belirli oranda
        Vector3 idealPos = corePos + direction * (totalDistance * autoTargetRatio);

        // Bu pozisyon boş mu kontrol et
        if (IsPositionClear(idealPos))
        {
            return idealPos;
        }

        // Boş değilse, en yakın boş noktayı ara
        if (autoTargetFindNearest)
        {
            return FindNearestClearPosition(corePos, cameraPos, direction, totalDistance);
        }

        // Hiçbir şey bulunamazsa ideal pozisyonu kullan
        return idealPos;
    }

    /// <summary>
    /// Verilen pozisyonda belirli yarıçapta engel var mı kontrol eder.
    /// </summary>
    private bool IsPositionClear(Vector3 position)
    {
        // autoTargetIgnoreMask'ın tersini al (bu layer'lar HARİÇ hepsini kontrol et)
        // Eğer ignoreMask boşsa (0), tüm layer'ları kontrol et
        LayerMask checkMask = autoTargetIgnoreMask.value == 0 ? ~0 : ~autoTargetIgnoreMask;

        Collider[] hits = Physics.OverlapSphere(position, autoTargetCheckRadius, checkMask, QueryTriggerInteraction.Ignore);

        // Core ve shells'i ignore et
        foreach (var hit in hits)
        {
            if (hit == null) continue;

            // Core'un parçasıysa ignore
            if (core != null && hit.transform.IsChildOf(core))
                continue;

            // Shell'lerin parçasıysa ignore
            bool isShell = false;
            if (shells != null)
            {
                foreach (var shell in shells)
                {
                    if (shell != null && hit.transform.IsChildOf(shell))
                    {
                        isShell = true;
                        break;
                    }
                }
            }
            if (isShell) continue;

            // Gerçek bir engel bulundu
            return false;
        }

        return true;
    }

    /// <summary>
    /// Core-Camera hattı boyunca en yakın boş pozisyonu arar.
    /// </summary>
    private Vector3 FindNearestClearPosition(Vector3 corePos, Vector3 cameraPos, Vector3 direction, float totalDistance)
    {
        float stepSize = totalDistance / autoTargetSearchSteps;
        float idealDistance = totalDistance * autoTargetRatio;

        // Önce ideal noktadan core'a doğru ara
        for (int i = 0; i < autoTargetSearchSteps; i++)
        {
            float distance = idealDistance - (stepSize * i);
            if (distance < stepSize) break;

            Vector3 testPos = corePos + direction * distance;
            if (IsPositionClear(testPos))
                return testPos;
        }

        // Sonra ideal noktadan kameraya doğru ara
        for (int i = 1; i < autoTargetSearchSteps; i++)
        {
            float distance = idealDistance + (stepSize * i);
            if (distance > totalDistance - stepSize) break;

            Vector3 testPos = corePos + direction * distance;
            if (IsPositionClear(testPos))
                return testPos;
        }

        // Hiçbir yer boş değilse ideal pozisyonu döndür
        return corePos + direction * idealDistance;
    }

    public void EndFocus()
    {
        if (!hasDefaultState)
            return;

        KillActive();

        isFocused = false;

        activeSequence = DOTween.Sequence().SetTarget(this);

        // 1) Core + camera go back
        activeSequence.Append(BuildCoreAndCameraOutSequence());

        // 2) Shells return back (and re-parent)
        activeSequence.Append(BuildShellReturnSequence());

        activeSequence.OnComplete(() =>
        {
            RestoreShellParentsAndLocals();
            runtimeShellStates.Clear();
            runtimeShellSet.Clear();
            activeSequence = null;
        });

        activeSequence.OnKill(() => activeSequence = null);
    }

    // Defaults are captured by CaptureDefaults()

    private Sequence BuildCoreAndCameraInSequence()
    {
        Sequence seq = DOTween.Sequence();

        // calculatedTargetPosition kullan (auto veya manuel)
        Vector3 focusPoint = calculatedTargetPosition;

        if (core != null)
        {
            seq.Join(core.DOMove(focusPoint, coreMoveDuration)
                .SetEase(coreEase)
                .SetTarget(core));
        }

        if (cameraRig != null)
        {
            Vector3 camTargetPos = focusPoint + cameraOffset;
            
            // Kamera pozisyon animasyonu
            seq.Join(cameraRig.DOMove(camTargetPos, cameraMoveDuration)
                .SetEase(cameraEase)
                .SetTarget(cameraRig));

            // Kameranın target'a bakacağı rotasyonu hesapla
            Vector3 lookDirection = focusPoint - camTargetPos;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                
                // Smooth rotasyon animasyonu
                seq.Join(cameraRig.DORotateQuaternion(targetRotation, cameraMoveDuration)
                    .SetEase(cameraEase)
                    .SetTarget(cameraRig));
            }
        }

        return seq;
    }

    private Sequence BuildCoreAndCameraOutSequence()
    {
        Sequence seq = DOTween.Sequence();

        if (core != null)
        {
            seq.Join(core.DOMove(coreState.position, coreMoveDuration)
                .SetEase(coreEase)
                .SetTarget(core));

            // Rotation/scale restore to be safe
            seq.Join(core.DORotateQuaternion(coreState.rotation, coreMoveDuration)
                .SetEase(coreEase)
                .SetTarget(core));
            seq.Join(core.DOScale(coreState.localScale, coreMoveDuration)
                .SetEase(coreEase)
                .SetTarget(core));
        }

        if (cameraRig != null)
        {
            seq.Join(cameraRig.DOMove(cameraState.position, cameraMoveDuration)
                .SetEase(cameraEase)
                .SetTarget(cameraRig));

            seq.Join(cameraRig.DORotateQuaternion(cameraState.rotation, cameraMoveDuration)
                .SetEase(cameraEase)
                .SetTarget(cameraRig));
        }

        return seq;
    }

private Sequence BuildShellScatterSequence()
{
    Sequence seq = DOTween.Sequence();
    if (core == null)
        return seq;

    List<ShellState> states = GetAllActiveShellStates();
    if (states.Count == 0)
        return seq;

    // Sort shells by distance to core (farthest first)
    states.Sort((a, b) =>
    {
        if (a.shell == null && b.shell == null) return 0;
        if (a.shell == null) return 1;
        if (b.shell == null) return -1;
        float distA = (a.shell.position - core.position).sqrMagnitude;
        float distB = (b.shell.position - core.position).sqrMagnitude;
        return distB.CompareTo(distA); // Descending
    });

    // Core'un focus noktasına gittiği yön (calculatedTargetPosition kullan)
    Vector3 coreToTarget = (calculatedTargetPosition - core.position);
    if (coreToTarget.sqrMagnitude < 0.0001f)
        coreToTarget = Vector3.forward;
    coreToTarget.Normalize();

    // Shell'lerin gideceği ana yön = Core'un TERSİ (core ileri giderse shells geri gider)
    Vector3 oppositeDir = -coreToTarget;

    // Kullanılan yönleri takip et (çakışma kontrolü için)
    List<Vector3> usedDirections = new List<Vector3>();
    float minAngleBetweenShells = 360f / Mathf.Max(1, states.Count); // Minimum açı farkı
    minAngleBetweenShells = Mathf.Min(minAngleBetweenShells, 45f); // Max 45 derece

    float delayAcc = 0f;

    foreach (var state in states)
    {
        Transform shell = state.shell;
        if (!shell)
            continue;

        // Yön hesapla
        Vector3 dir = CalculateUniqueShellDirection(oppositeDir, usedDirections, minAngleBetweenShells);

        // Bias ekle (varsa)
        Vector3 bias = shellDirectionBias;
        if (shellBiasInParentSpace && state.parent)
            bias = state.parent.TransformDirection(bias);

        if (shellHorizontalOnly)
        {
            dir.y = 0f;
            bias.y = 0f;
        }

        // Bias'ı yöne ekle
        if (bias.sqrMagnitude > 0.0001f && shellBiasStrength > 0f)
        {
            dir = (dir + bias.normalized * shellBiasStrength).normalized;
        }

        // Normalize ve kaydet
        if (dir.sqrMagnitude < 0.0001f)
            dir = oppositeDir;
        else
            dir = dir.normalized;

        usedDirections.Add(dir);

        // Only detach if it's under the core hierarchy (otherwise detaching scene objects can be risky).
        if (shell.parent != null && shell.IsChildOf(core))
            shell.SetParent(null, true);

        float dist = Random.Range(shellScatterDistance * 0.4f, shellScatterDistance);
        float up = Random.Range(0.05f, shellUpForce);

        Vector3 lateral = dir * dist;
        lateral = Vector3.Scale(lateral, new Vector3(shellScatterAxisMultiplier.x, 1f, shellScatterAxisMultiplier.z));
        Vector3 vertical = Vector3.up * (up * shellScatterAxisMultiplier.y);

        Vector3 targetPos = shell.position + lateral + vertical;

        seq.Join(
            shell.DOMove(targetPos, shellScatterDuration)
                .SetDelay(delayAcc)
                .SetEase(shellEase)
                .SetTarget(shell)
        );

        delayAcc += Mathf.Max(0f, shellStagger);
    }

    return seq;
}

/// <summary>
/// Mevcut yönlerle çakışmayan benzersiz bir yön hesaplar.
/// </summary>
private Vector3 CalculateUniqueShellDirection(Vector3 baseDirection, List<Vector3> usedDirections, float minAngle)
{
    const int maxAttempts = 20;
    
    for (int attempt = 0; attempt < maxAttempts; attempt++)
    {
        // Rastgele sapma ekle
        Vector3 randomOffset;
        if (shellHorizontalOnly)
        {
            // Sadece XZ düzleminde rastgele açı
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            randomOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
        else
        {
            randomOffset = Random.insideUnitSphere.normalized;
        }

        // Base direction'ı rastgele sapma ile karıştır
        float blendFactor = Random.Range(0.3f, 0.7f);
        Vector3 candidateDir = (baseDirection * (1f - blendFactor) + randomOffset * blendFactor).normalized;

        if (shellHorizontalOnly)
            candidateDir.y = 0f;

        if (candidateDir.sqrMagnitude < 0.0001f)
            candidateDir = baseDirection;
        else
            candidateDir = candidateDir.normalized;

        // Bu yön diğerleriyle çakışıyor mu kontrol et
        bool isUnique = true;
        foreach (var usedDir in usedDirections)
        {
            float angle = Vector3.Angle(candidateDir, usedDir);
            if (angle < minAngle)
            {
                isUnique = false;
                break;
            }
        }

        if (isUnique || usedDirections.Count == 0)
            return candidateDir;
    }

    // Max deneme aşıldı, en iyi tahminle devam et
    // Eşit aralıklı açılardan birini seç
    int index = usedDirections.Count;
    float spreadAngle = (360f / Mathf.Max(1, usedDirections.Count + 5)) * index;
    
    if (shellHorizontalOnly)
    {
        float rad = spreadAngle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)).normalized;
    }
    else
    {
        // Spherical distribution
        float theta = spreadAngle * Mathf.Deg2Rad;
        float phi = Mathf.Acos(1f - 2f * ((float)index / Mathf.Max(1, usedDirections.Count + 1)));
        return new Vector3(
            Mathf.Sin(phi) * Mathf.Cos(theta),
            Mathf.Cos(phi),
            Mathf.Sin(phi) * Mathf.Sin(theta)
        ).normalized;
    }
}
    private Vector3 ResolveDirection(Vector3 originalDir, Vector3 bias)
    {
        Vector3 dir = originalDir;
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.zero;
        else
            dir = dir.normalized;

        if (bias.sqrMagnitude > 0.0001f && shellBiasStrength > 0f)
        {
            bias = bias.normalized;
            dir = (dir + bias * shellBiasStrength);
            if (dir.sqrMagnitude < 0.0001f)
                dir = bias;
            else
                dir = dir.normalized;
        }

        if (dir.sqrMagnitude < 0.0001f)
            dir = RandomHorizontalDir();

        return dir;
    }

    private Sequence BuildShellReturnSequence()
    {
        Sequence seq = DOTween.Sequence();
        List<ShellState> states = GetAllActiveShellStates();
        if (states.Count == 0)
            return seq;

        // Sort shells by distance to core (closest first) for return
        states.Sort((a, b) =>
        {
            if (a.shell == null && b.shell == null) return 0;
            if (a.shell == null) return 1;
            if (b.shell == null) return -1;
            float distA = (a.shell.position - core.position).sqrMagnitude;
            float distB = (b.shell.position - core.position).sqrMagnitude;
            return distA.CompareTo(distB); // Ascending
        });

        float baseDelay = 0f;

        foreach (var state in states)
        {
            Transform shell = state.shell;
            if (shell == null)
                continue;

            float delay = baseDelay;
            baseDelay += Mathf.Max(0f, shellStagger);

            // Re-parent first (keep world), then tween back to saved LOCAL defaults.
            shell.SetParent(state.parent, true);

            seq.Join(shell.DOLocalMove(state.localPosition, shellScatterDuration)
                .SetDelay(delay)
                .SetEase(Ease.InOutSine)
                .SetTarget(shell));

            seq.Join(shell.DOLocalRotateQuaternion(state.localRotation, shellScatterDuration)
                .SetDelay(delay)
                .SetEase(Ease.InOutSine)
                .SetTarget(shell));

            seq.Join(shell.DOScale(state.localScale, shellScatterDuration)
                .SetDelay(delay)
                .SetEase(Ease.InOutSine)
                .SetTarget(shell));
        }

        // Ensure we re-parent after motion finishes.
        seq.AppendCallback(RestoreShellParentsAndLocals);
        return seq;
    }

    private void RestoreShellParentsAndLocals()
    {
        List<ShellState> states = GetAllActiveShellStates();
        for (int i = 0; i < states.Count; i++)
        {
            ShellState st = states[i];
            if (st.shell == null)
                continue;

            st.shell.SetParent(st.parent, false);
            st.shell.SetSiblingIndex(st.siblingIndex);
            st.shell.localPosition = st.localPosition;
            st.shell.localRotation = st.localRotation;
            st.shell.localScale = st.localScale;
        }
    }

    private List<ShellState> GetAllActiveShellStates()
    {
        // Returns a unique set of states from: inspector shells (captured defaults) + raycast shells (runtime)
        List<ShellState> result = new List<ShellState>();
        HashSet<Transform> seen = new HashSet<Transform>();

        if (shellStates != null)
        {
            for (int i = 0; i < shellStates.Length; i++)
            {
                ShellState st = shellStates[i];
                if (st.shell == null)
                    continue;
                if (seen.Add(st.shell))
                    result.Add(st);
            }
        }

        for (int i = 0; i < runtimeShellStates.Count; i++)
        {
            ShellState st = runtimeShellStates[i];
            if (st.shell == null)
                continue;
            if (seen.Add(st.shell))
                result.Add(st);
        }

        return result;
    }

    private void RefreshRuntimeShellsFromRaycast()
    {
        runtimeShellStates.Clear();
        runtimeShellSet.Clear();

        if (!collectShellsFromRaycast || core == null || target == null)
            return;

        Vector3 origin = core.position;
        Vector3 toTarget = target.position - origin;
        float dist = toTarget.magnitude;
        if (dist < 0.001f)
            return;

        Vector3 dir = toTarget / dist;
        HashSet<Collider> ignored = new HashSet<Collider>();

        // Iteratively add the next closest obstacle until nothing blocks the line.
        for (int safety = 0; safety < 256; safety++)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, dir, dist, obstacleMask, obstacleTriggerInteraction);
            if (hits == null || hits.Length == 0)
                break;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            Collider next = null;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i].collider;
                if (c == null)
                    continue;
                if (ignored.Contains(c))
                    continue;
                if (IsPartOf(core, c.transform) || IsPartOf(target, c.transform))
                    continue;

                next = c;
                break;
            }

            if (next == null)
                break;

            ignored.Add(next);

            Transform moveTr = PickMoveTransform(next);
            if (moveTr == null)
                continue;

            if (runtimeShellSet.Contains(moveTr))
                continue;

            runtimeShellSet.Add(moveTr);
            runtimeShellStates.Add(new ShellState
            {
                shell = moveTr,
                parent = moveTr.parent,
                siblingIndex = moveTr.GetSiblingIndex(),
                localPosition = moveTr.localPosition,
                localRotation = moveTr.localRotation,
                localScale = moveTr.localScale
            });

            // Raycast ile bulunan objeyi non-static yap (batching'den çıkar)
            if (staticOptimizer != null)
                staticOptimizer.MarkAsMovable(moveTr);
        }
    }

    private static bool IsPartOf(Transform root, Transform candidate)
    {
        if (root == null || candidate == null)
            return false;
        return candidate == root || candidate.IsChildOf(root);
    }

    private static Transform PickMoveTransform(Collider col)
    {
        if (col == null)
            return null;

        if (col.attachedRigidbody != null)
            return col.attachedRigidbody.transform;

        Renderer r = col.GetComponentInParent<Renderer>();
        if (r != null)
            return r.transform;

        return col.transform;
    }

    // ---------------- UTILS ----------------
    Vector3 RandomHorizontalDir()
    {
        Vector2 v = Random.insideUnitCircle.normalized;
        return new Vector3(v.x, 0f, v.y);
    }

    // ---------------- EDITOR GIZMOS ----------------
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (core == null)
            return;

        // Focus noktasını hesapla (editor'da preview için)
        Vector3 focusPoint;
        if (autoTarget && cameraRig != null)
        {
            // AutoTarget preview: core-camera arası
            Vector3 cameraPos = cameraRig.position;
            Vector3 direction = (cameraPos - core.position).normalized;
            float totalDistance = Vector3.Distance(core.position, cameraPos);
            focusPoint = core.position + direction * (totalDistance * autoTargetRatio);
        }
        else if (target != null)
        {
            focusPoint = target.position;
        }
        else
        {
            return;
        }

        // Kameranın gideceği pozisyon
        Vector3 camTargetPos = focusPoint + cameraOffset;

        // Yeşil küre = Kamera hedef pozisyonu
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(camTargetPos, 0.5f);
        Gizmos.DrawSphere(camTargetPos, 0.15f);

        // Yeşil çizgi = Kamera -> Target bakış yönü
        Gizmos.DrawLine(camTargetPos, focusPoint);

        // Sarı/Turuncu küre = Focus Point (AutoTarget için turuncu)
        Gizmos.color = autoTarget ? new Color(1f, 0.5f, 0f) : Color.yellow;
        Gizmos.DrawWireSphere(focusPoint, autoTargetCheckRadius);
        Gizmos.DrawSphere(focusPoint, 0.15f);

        // Core - mavi
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(core.position, 0.25f);
        Gizmos.DrawLine(core.position, focusPoint);

        // Kamera rig varsa, mevcut pozisyondan hedef pozisyona çizgi
        if (cameraRig != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawLine(cameraRig.position, camTargetPos);

            // AutoTarget: core->camera hattını göster
            if (autoTarget)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Gizmos.DrawLine(core.position, cameraRig.position);
            }
        }

        // Labels
        UnityEditor.Handles.Label(camTargetPos + Vector3.up * 0.7f, "Cam Target");
        UnityEditor.Handles.Label(focusPoint + Vector3.up * 0.5f, autoTarget ? "Auto Focus" : "Focus Point");
        UnityEditor.Handles.Label(core.position + Vector3.up * 0.4f, "Core");
    }

    private void OnDrawGizmos()
    {
        if (core == null) return;

        // Her zaman görünür hafif gizmo
        Vector3 focusPoint;
        if (autoTarget && cameraRig != null)
        {
            Vector3 direction = (cameraRig.position - core.position).normalized;
            float totalDistance = Vector3.Distance(core.position, cameraRig.position);
            focusPoint = core.position + direction * (totalDistance * autoTargetRatio);
        }
        else if (target != null)
        {
            focusPoint = target.position;
        }
        else
        {
            return;
        }

        Vector3 camTargetPos = focusPoint + cameraOffset;
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(camTargetPos, 0.3f);

        // AutoTarget için turuncu focus point
        if (autoTarget)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(focusPoint, autoTargetCheckRadius * 0.5f);
        }
    }
#endif
}



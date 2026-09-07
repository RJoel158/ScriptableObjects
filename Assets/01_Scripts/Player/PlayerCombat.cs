using System;
using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public static PlayerCombat Instance { get; private set; }

    [Header("Weapon Mount & Aiming")]
    [Tooltip("Hueso de la mano derecha. Si lo dejas vacío, el script lo detectará automáticamente")]
    public Transform handTransform;
    public Transform firePoint;
    public Transform firstPersonCamera;

    [Header("Live Hand Weapon Adjustment")]
    [Tooltip("Si activas esto, puedes calibrar posición, rotación y escala directamente desde el Inspector en Play Mode")]
    public bool enableLiveTransformTuning = false;
    public Vector3 liveWeaponOffset = new Vector3(0.06f, 0.04f, 0.02f);
    public Vector3 liveWeaponRotation = new Vector3(-10f, 85f, -90f);
    public Vector3 liveWeaponScale = new Vector3(2.8f, 2.8f, 2.8f);

    [Header("Default Visuals & Prefabs")]
    public GameObject defaultProjectilePrefab;

    [Header("Current Weapon State")]
    public WeaponData currentWeapon;
    [Tooltip("Opcional: Modelo de pistola en la mano derecha del soldado en la jerarquía.")]
    public GameObject customHandWeaponModel;
    [Tooltip("Opcional: Modelo de fusil (AK47/AK74) en la mano derecha del soldado en la jerarquía.")]
    public GameObject customHandRifleModel;
    private GameObject currentEquippedModel;
    private float nextFireTime = 0f;
    private Camera mainCamera;
    private Animator animator;
    private RuntimeAnimatorController defaultAnimatorController;

    private Transform spineBone;
    private Transform chestBone;
    private Transform headBone;

    [Header("Ammo System")]
    public int currentMagAmmo;
    public int currentReserveAmmo;
    public bool isReloading = false;

    // Animator Hashes
    private static readonly int WeaponTypeHash = Animator.StringToHash("WeaponType");
    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int ReloadHash = Animator.StringToHash("Reload");

    // Eventos
    public event Action<int, int, bool> OnAmmoChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            defaultAnimatorController = animator.runtimeAnimatorController;
        }

        AutoFindHierarchyWeapons();
    }

    void Start()
    {
        mainCamera = Camera.main;
        AutoDetectRightHand();
        DetectAimBones();

        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnWeaponEquipped += EquipWeapon;

            if (Inventory.Instance.equippedWeapon != null)
            {
                EquipWeapon(Inventory.Instance.equippedWeapon);
            }
        }

        if (currentWeapon != null)
        {
            EquipWeapon(currentWeapon);
        }
        else
        {
            // Cargar pistola inicial por defecto si no se asignó ninguna
            WeaponData[] allWeapons = Resources.FindObjectsOfTypeAll<WeaponData>();
            foreach (var w in allWeapons)
            {
                if (w != null && w.weaponCategory == WeaponType.Pistol)
                {
                    currentWeapon = w;
                    EquipWeapon(w);
                    break;
                }
            }

            if (animator != null && currentWeapon == null)
            {
                animator.SetInteger(WeaponTypeHash, 0); // 0 = Pistol
            }
        }
    }

    void OnDestroy()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnWeaponEquipped -= EquipWeapon;
        }
    }

    void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // Mantener WeaponType sincronizado en tiempo real con el Animator
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            int targetWeaponType = currentWeapon != null ? (int)currentWeapon.weaponCategory : 0;
            if (animator.GetInteger(WeaponTypeHash) != targetWeaponType)
            {
                animator.SetInteger(WeaponTypeHash, targetWeaponType);
            }
        }

        // Sincronizar posición, rotación y escala del arma en tiempo real
        if (currentEquippedModel != null && currentEquippedModel != customHandWeaponModel)
        {
            if (enableLiveTransformTuning)
            {
                currentEquippedModel.transform.localPosition = liveWeaponOffset;
                currentEquippedModel.transform.localEulerAngles = liveWeaponRotation;
                currentEquippedModel.transform.localScale = liveWeaponScale;
            }
            else if (currentWeapon != null)
            {
                currentEquippedModel.transform.localPosition = currentWeapon.weaponEquipOffset;
                currentEquippedModel.transform.localEulerAngles = currentWeapon.weaponEquipRotation;
                currentEquippedModel.transform.localScale = currentWeapon.weaponScale != Vector3.zero ? currentWeapon.weaponScale : Vector3.one;
            }
        }

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null && health.IsDead) return;

        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            TryAttack();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            TryReload();
        }
    }

    public void AutoDetectRightHand()
    {
        if (handTransform != null && (handTransform.name.ToLower().Contains("righthand") || handTransform.name.ToLower().Contains("hand_r")))
        {
            return;
        }

        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator != null && animator.isHuman)
        {
            Transform rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHandBone != null)
            {
                handTransform = rightHandBone;
                Debug.Log($"[PlayerCombat] Mano detectada vía HumanBodyBones: {handTransform.name}");
                return;
            }
        }

        Transform soldier = transform.Find("Soldier");
        Transform searchRoot = soldier != null ? soldier : transform;

        string[] handKeywords = new string[] { "righthand", "hand_r", "hand.r", "right_hand", "hand" };
        Transform found = SearchBoneRecursive(searchRoot, handKeywords);
        if (found != null)
        {
            handTransform = found;
            Debug.Log($"[PlayerCombat] Mano detectada por jerarquía de huesos: {handTransform.name}");
        }
        else
        {
            handTransform = transform;
        }
    }

    public void DetectAimBones()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator != null && animator.isHuman)
        {
            spineBone = animator.GetBoneTransform(HumanBodyBones.Spine);
            chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        }

        Transform soldier = transform.Find("Soldier");
        Transform searchRoot = soldier != null ? soldier : transform;

        if (spineBone == null) spineBone = SearchBoneRecursive(searchRoot, new string[] { "spine1", "spine_01", "spine" });
        if (chestBone == null) chestBone = SearchBoneRecursive(searchRoot, new string[] { "spine2", "spine_02", "chest" });
        if (headBone == null) headBone = SearchBoneRecursive(searchRoot, new string[] { "head" });
    }

    [Header("Aim Bone Tilting")]
    [Tooltip("Habilita la inclinación manual del torso con el ratón. Desactivado por defecto para evitar tembleques.")]
    public bool enableUpperBodyAimPitch = false;
    private float smoothAimPitch = 0f;

    void LateUpdate()
    {
        if (!enableUpperBodyAimPitch) return;

        PerspectiveCameraController camCtrl = PerspectiveCameraController.Instance;
        if (camCtrl != null && camCtrl.IsInOverTheShoulder)
        {
            smoothAimPitch = Mathf.Lerp(smoothAimPitch, camCtrl.CurrentPitch, Time.deltaTime * 18f);
            if (Mathf.Abs(smoothAimPitch) > 0.01f)
            {
                // Inclinar el torso superior de forma sólida y suave (el cuello, cabeza y brazos se mueven naturalmente al unísono)
                Transform targetBone = chestBone != null ? chestBone : spineBone;
                if (targetBone != null)
                {
                    targetBone.rotation = Quaternion.AngleAxis(smoothAimPitch * 0.60f, transform.right) * targetBone.rotation;
                }
            }
        }
        else
        {
            smoothAimPitch = 0f;
        }
    }

    private Transform SearchBoneRecursive(Transform current, string[] keywords)
    {
        string currentName = current.name.ToLower();
        foreach (var kw in keywords)
        {
            if (currentName.Contains(kw) && current != transform)
            {
                return current;
            }
        }

        for (int i = 0; i < current.childCount; i++)
        {
            Transform result = SearchBoneRecursive(current.GetChild(i), keywords);
            if (result != null) return result;
        }

        return null;
    }

    public void AutoFindHierarchyWeapons()
    {
        AutoDetectRightHand();

        Transform rootToSearch = handTransform != null ? handTransform : transform;

        // Auto-descubrir Pistola en jerarquía si no está asignada
        if (customHandWeaponModel == null)
        {
            customHandWeaponModel = FindChildWithKeywords(rootToSearch, new string[] { "colt", "m1911", "pistol", "gun" });
            if (customHandWeaponModel == null && handTransform != transform)
            {
                customHandWeaponModel = FindChildWithKeywords(transform, new string[] { "colt", "m1911", "pistol", "gun" });
            }
        }

        // Auto-descubrir Fusil AK47 / AK74 en jerarquía si no está asignado
        if (customHandRifleModel == null)
        {
            customHandRifleModel = FindChildWithKeywords(rootToSearch, new string[] { "ak74", "ak47", "ak-74", "ak-47", "rifle", "fusil", "m4" });
            if (customHandRifleModel == null && handTransform != transform)
            {
                customHandRifleModel = FindChildWithKeywords(transform, new string[] { "ak74", "ak47", "ak-74", "ak-47", "rifle", "fusil", "m4" });
            }
        }
    }

    private GameObject FindChildWithKeywords(Transform parent, string[] keywords)
    {
        if (parent == null) return null;
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>(true);
        foreach (var child in allChildren)
        {
            if (child == parent || child == transform) continue;
            string childName = child.name.ToLower();
            foreach (var kw in keywords)
            {
                if (childName.Contains(kw))
                {
                    return child.gameObject;
                }
            }
        }
        return null;
    }

    public void EquipWeapon(WeaponData weapon)
    {
        if (weapon == null) return;
        currentWeapon = weapon;
        isReloading = false;

        AutoFindHierarchyWeapons();

        // 1. Destruir cualquier modelo de arma previamente instanciado dinámicamente
        if (currentEquippedModel != null && currentEquippedModel != customHandWeaponModel && currentEquippedModel != customHandRifleModel)
        {
            Destroy(currentEquippedModel);
            currentEquippedModel = null;
        }

        // 2. Manejar modelos manuales en la mano según categoría
        if (weapon.weaponCategory == WeaponType.Pistol)
        {
            if (customHandWeaponModel != null)
            {
                customHandWeaponModel.SetActive(true);
                currentEquippedModel = customHandWeaponModel;
            }
            if (customHandRifleModel != null)
            {
                customHandRifleModel.SetActive(false);
            }
        }
        else if (weapon.weaponCategory == WeaponType.Rifle)
        {
            if (customHandWeaponModel != null)
            {
                customHandWeaponModel.SetActive(false);
            }
            if (customHandRifleModel != null)
            {
                customHandRifleModel.SetActive(true);
                currentEquippedModel = customHandRifleModel;
            }
        }
        else
        {
            if (customHandWeaponModel != null) customHandWeaponModel.SetActive(false);
            if (customHandRifleModel != null) customHandRifleModel.SetActive(false);
        }

        // 3. Si no hay modelo manual configurado o detectado en la mano para esta categoría, instanciar el prefab del ScriptableObject
        if (currentEquippedModel == null || (currentEquippedModel != customHandWeaponModel && currentEquippedModel != customHandRifleModel))
        {
            GameObject prefabToSpawn = weapon.weaponModelPrefab != null ? weapon.weaponModelPrefab : weapon.itemPrefab;
            if (prefabToSpawn != null)
            {
                Transform parentMount = handTransform != null ? handTransform : transform;
                currentEquippedModel = Instantiate(prefabToSpawn, parentMount);
                currentEquippedModel.transform.SetParent(parentMount, false);
                currentEquippedModel.transform.localPosition = weapon.weaponEquipOffset;
                currentEquippedModel.transform.localEulerAngles = weapon.weaponEquipRotation;
                currentEquippedModel.transform.localScale = weapon.weaponScale != Vector3.zero ? weapon.weaponScale : Vector3.one;
            }
        }

        // Actualizar valores de ajuste live para el inspector
        liveWeaponOffset = weapon.weaponEquipOffset;
        liveWeaponRotation = weapon.weaponEquipRotation;
        liveWeaponScale = weapon.weaponScale != Vector3.zero ? weapon.weaponScale : Vector3.one;

        if (weapon.usesAmmo)
        {
            currentMagAmmo = weapon.magazineCapacity;
            currentReserveAmmo = weapon.maxReserveAmmo;
        }
        else
        {
            currentMagAmmo = -1;
            currentReserveAmmo = -1;
        }

        // Actualizar Animator con el tipo de arma o con AnimatorOverrideController
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            if (weapon.animatorOverride != null)
            {
                animator.runtimeAnimatorController = weapon.animatorOverride;
            }
            else if (defaultAnimatorController != null && animator.runtimeAnimatorController != defaultAnimatorController)
            {
                animator.runtimeAnimatorController = defaultAnimatorController;
            }

            animator.SetInteger(WeaponTypeHash, (int)weapon.weaponCategory);
        }

        NotifyAmmoChanged();
    }

    public void OnPerspectiveChanged(CameraPerspective perspective)
    {
        if (currentEquippedModel != null && currentWeapon != null && !enableLiveTransformTuning)
        {
            currentEquippedModel.transform.localPosition = currentWeapon.weaponEquipOffset;
            currentEquippedModel.transform.localEulerAngles = currentWeapon.weaponEquipRotation;
        }
    }

    public bool IsReloadingOrPlayingReload()
    {
        if (isReloading) return true;
        if (animator != null && animator.layerCount > 0)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Player_Reload") || state.IsTag("Reload"))
            {
                return true;
            }
        }
        return false;
    }

    private void TryAttack()
    {
        if (currentWeapon == null)
        {
            if (Inventory.Instance != null && Inventory.Instance.equippedWeapon != null)
            {
                EquipWeapon(Inventory.Instance.equippedWeapon);
            }
            else if (Inventory.Instance != null && Inventory.Instance.items.Count > 0)
            {
                InventorySlot weaponSlot = Inventory.Instance.items.Find(s => s.item is WeaponData);
                if (weaponSlot != null && weaponSlot.item is WeaponData w)
                {
                    Inventory.Instance.EquipWeapon(w);
                }
            }

            if (currentWeapon == null) return;
        }

        // Bloqueo total de disparo durante toda la duración de la recarga
        if (IsReloadingOrPlayingReload()) return;
        if (Time.time < nextFireTime) return;

        if (currentWeapon.usesAmmo)
        {
            if (currentMagAmmo <= 0)
            {
                TryReload();
                return;
            }

            currentMagAmmo--;
            NotifyAmmoChanged();
        }

        float attackRate = currentWeapon.attackSpeed > 0 ? currentWeapon.attackSpeed : 2.5f;
        nextFireTime = Time.time + (1f / attackRate);

        if (animator != null)
        {
            animator.SetTrigger(ShootHash);
        }

        PerformAttack();
    }

    private void PerformAttack()
    {
        PerspectiveCameraController camCtrl = PerspectiveCameraController.Instance;
        bool isFirstPerson = camCtrl != null && camCtrl.IsFirstPerson;

        // Calcular posición de salida del proyectil (boca del cañón / mano)
        Vector3 spawnPos;
        if (firePoint != null)
        {
            spawnPos = firePoint.position;
        }
        else if (currentEquippedModel != null)
        {
            spawnPos = currentEquippedModel.transform.position + transform.forward * 0.35f + Vector3.up * 0.08f;
        }
        else if (handTransform != null)
        {
            spawnPos = handTransform.position + transform.forward * 0.35f + Vector3.up * 0.05f;
        }
        else
        {
            spawnPos = transform.position + transform.forward * 0.6f + Vector3.up * 1.3f;
        }

        Vector3 aimDirection = transform.forward;
        Vector3 targetPoint = spawnPos + transform.forward * 50f;

        if (isFirstPerson && mainCamera != null)
        {
            // Raycast desde el centro exacto de la pantalla (la mira en cruz)
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, 150f, ~0, QueryTriggerInteraction.Ignore);
            
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            bool foundHit = false;

            foreach (var h in hits)
            {
                if (h.collider != null && h.collider.gameObject != gameObject && !h.transform.IsChildOf(transform))
                {
                    targetPoint = h.point;
                    foundHit = true;
                    break;
                }
            }

            if (!foundHit)
            {
                targetPoint = ray.GetPoint(100f);
            }

            aimDirection = (targetPoint - spawnPos).normalized;
        }
        else if (mainCamera != null)
        {
            // Vista Top-down / Tercera persona: apuntar hacia el cursor del ratón
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 150f, ~0, QueryTriggerInteraction.Ignore);
            
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            bool foundHit = false;

            foreach (var h in hits)
            {
                if (h.collider != null && h.collider.gameObject != gameObject && !h.transform.IsChildOf(transform))
                {
                    targetPoint = h.point;
                    foundHit = true;
                    break;
                }
            }

            if (!foundHit)
            {
                Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, spawnPos.y, 0f));
                if (groundPlane.Raycast(ray, out float enterDist))
                {
                    targetPoint = ray.GetPoint(enterDist);
                }
                else
                {
                    targetPoint = ray.GetPoint(50f);
                }
            }

            aimDirection = (targetPoint - spawnPos).normalized;
        }

        int damage = currentWeapon.damage > 0 ? currentWeapon.damage : 20;
        float speed = currentWeapon.projectileSpeed > 0 ? currentWeapon.projectileSpeed : 45f;
        float range = currentWeapon.attackRange > 0 ? currentWeapon.attackRange : 50f;
        Color projColor = currentWeapon.projectileColor;

        GameObject projPrefab = currentWeapon.projectilePrefab != null 
            ? currentWeapon.projectilePrefab 
            : defaultProjectilePrefab;

        GameObject projObj;
        if (projPrefab != null)
        {
            projObj = Instantiate(projPrefab, spawnPos, Quaternion.LookRotation(aimDirection));
        }
        else
        {
            projObj = CreateDefaultProjectileObject(spawnPos, aimDirection, projColor);
        }

        Projectile projectile = projObj.GetComponent<Projectile>();
        if (projectile == null) projectile = projObj.AddComponent<Projectile>();

        projectile.Initialize(damage, speed, range, aimDirection, projColor, fromPlayer: true, shooter: gameObject);
    }

    public void TryReload()
    {
        if (currentWeapon == null || !currentWeapon.usesAmmo || isReloading) return;
        if (currentMagAmmo >= currentWeapon.magazineCapacity) return;
        if (currentReserveAmmo <= 0)
        {
            HUDUI hud = FindAnyObjectByType<HUDUI>();
            if (hud != null) hud.ShowNotification("¡Sin munición! Visita la tienda 'T'");
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        NotifyAmmoChanged();

        if (animator != null)
        {
            animator.ResetTrigger(ShootHash);
            animator.SetTrigger(ReloadHash);
        }

        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null) hud.ShowNotification("Recargando...");

        float duration = (currentWeapon != null && currentWeapon.reloadDuration > 0) ? currentWeapon.reloadDuration : 3.2f;
        yield return new WaitForSeconds(duration);

        if (currentWeapon != null)
        {
            int needed = currentWeapon.magazineCapacity - currentMagAmmo;
            int toLoad = Mathf.Min(needed, currentReserveAmmo);

            currentMagAmmo += toLoad;
            currentReserveAmmo -= toLoad;
        }

        // Breve pausa para asegurar que la animación haya salido por completo de la transición
        yield return new WaitForSeconds(0.15f);

        isReloading = false;
        NotifyAmmoChanged();
    }

    public void RefillAmmo(int amount)
    {
        if (currentWeapon != null && currentWeapon.usesAmmo)
        {
            currentReserveAmmo += amount;
            currentReserveAmmo = Mathf.Min(currentReserveAmmo, currentWeapon.maxReserveAmmo * 2);
        }
        else
        {
            currentReserveAmmo += amount;
        }

        NotifyAmmoChanged();

        HUDUI hud = HUDUI.Instance != null ? HUDUI.Instance : FindAnyObjectByType<HUDUI>();
        if (hud != null)
        {
            hud.UpdateAmmoUI(currentMagAmmo, currentReserveAmmo, isReloading);
            hud.ShowNotification($"+{amount} Balas recibidas");
        }
    }

    public void NotifyAmmoChanged()
    {
        OnAmmoChanged?.Invoke(currentMagAmmo, currentReserveAmmo, isReloading);
        HUDUI hud = HUDUI.Instance != null ? HUDUI.Instance : FindAnyObjectByType<HUDUI>();
        if (hud != null)
        {
            hud.UpdateAmmoUI(currentMagAmmo, currentReserveAmmo, isReloading);
        }
    }

    private GameObject CreateDefaultProjectileObject(Vector3 pos, Vector3 dir, Color color)
    {
        GameObject proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        proj.name = "Projectile_Player";
        proj.transform.position = pos;
        proj.transform.localScale = Vector3.one * 0.25f;
        proj.transform.rotation = Quaternion.LookRotation(dir);

        SphereCollider col = proj.GetComponent<SphereCollider>();
        if (col != null) col.isTrigger = true;

        Renderer r = proj.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = color;
        }

        TrailRenderer trail = proj.AddComponent<TrailRenderer>();
        trail.time = 0.2f;
        trail.startWidth = 0.2f;
        trail.endWidth = 0.02f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);

        return proj;
    }
}
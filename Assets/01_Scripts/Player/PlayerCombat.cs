using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Weapon Mount")]
    public Transform handTransform;
    public Transform firePoint;

    [Header("Default Visuals & Prefabs")]
    [SerializeField] private GameObject defaultProjectilePrefab;
    
    private GameObject currentEquippedModel;
    private WeaponData currentWeapon;
    private float nextFireTime = 0f;

    void Start()
    {
        // Suscribirse a eventos de inventario
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnWeaponEquipped += EquipWeapon;

            // Si ya hay un arma equipada por defecto
            if (Inventory.Instance.equippedWeapon != null)
            {
                EquipWeapon(Inventory.Instance.equippedWeapon);
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
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null && health.IsDead) return;

        // Disparo con clic izquierdo si no está sobre UI
        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space))
        {
            // Evitar disparar si el puntero está sobre elementos de UI bloqueantes
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            TryAttack();
        }
    }

    public void EquipWeapon(WeaponData weapon)
    {
        currentWeapon = weapon;

        // Destruir modelo previo en la mano
        if (currentEquippedModel != null)
        {
            Destroy(currentEquippedModel);
            currentEquippedModel = null;
        }

        if (handTransform == null)
        {
            handTransform = transform;
        }

        // Instanciar modelo visual del arma
        if (weapon != null)
        {
            if (weapon.weaponModelPrefab != null)
            {
                currentEquippedModel = Instantiate(weapon.weaponModelPrefab, handTransform);
            }
            else if (weapon.itemPrefab != null)
            {
                currentEquippedModel = Instantiate(weapon.itemPrefab, handTransform);
            }
            else
            {
                // Crear modelo procedural básico si no hay prefab asignado
                currentEquippedModel = CreateProceduralWeaponModel(weapon.weaponCategory);
                currentEquippedModel.transform.SetParent(handTransform, false);
            }

            if (currentEquippedModel != null)
            {
                currentEquippedModel.transform.localPosition = weapon.weaponEquipOffset;
                currentEquippedModel.transform.localEulerAngles = weapon.weaponEquipRotation;
            }
        }
    }

    private void TryAttack()
    {
        if (currentWeapon == null)
        {
            // Intenta equipar la primera arma del inventario si no tiene ninguna equipada
            if (Inventory.Instance != null)
            {
                InventorySlot weaponSlot = Inventory.Instance.items.Find(s => s.item is WeaponData);
                if (weaponSlot != null && weaponSlot.item is WeaponData w)
                {
                    Inventory.Instance.EquipWeapon(w);
                }
            }
        }

        if (Time.time < nextFireTime) return;

        float attackRate = currentWeapon != null ? currentWeapon.attackSpeed : 1.5f;
        nextFireTime = Time.time + (1f / Mathf.Max(attackRate, 0.1f));

        PerformAttack();
    }

    private void PerformAttack()
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : (handTransform != null ? handTransform.position + transform.forward * 0.5f : transform.position + transform.forward * 0.8f);
        Vector3 aimDirection = transform.forward;

        int damage = currentWeapon != null ? currentWeapon.damage : 15;
        float speed = currentWeapon != null ? currentWeapon.projectileSpeed : 22f;
        float range = currentWeapon != null ? currentWeapon.attackRange : 30f;
        Color projColor = currentWeapon != null ? currentWeapon.projectileColor : Color.cyan;

        GameObject projPrefab = currentWeapon != null && currentWeapon.projectilePrefab != null 
            ? currentWeapon.projectilePrefab 
            : defaultProjectilePrefab;

        GameObject projObj;
        if (projPrefab != null)
        {
            projObj = Instantiate(projPrefab, spawnPos, Quaternion.LookRotation(aimDirection));
        }
        else
        {
            // Crear proyectil básico si no hay prefab asignado
            projObj = CreateDefaultProjectileObject(spawnPos, aimDirection, projColor);
        }

        Projectile projectile = projObj.GetComponent<Projectile>();
        if (projectile == null) projectile = projObj.AddComponent<Projectile>();

        projectile.Initialize(damage, speed, range, aimDirection, projColor, fromPlayer: true);
    }

    private GameObject CreateProceduralWeaponModel(WeaponType type)
    {
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.name = $"Model_{type}";
        Collider col = model.GetComponent<Collider>();
        if (col != null) Destroy(col);

        switch (type)
        {
            case WeaponType.Sword:
                model.transform.localScale = new Vector3(0.08f, 0.7f, 0.15f);
                model.transform.localPosition = new Vector3(0.3f, 0.2f, 0.4f);
                model.transform.localEulerAngles = new Vector3(45f, 0f, 0f);
                model.GetComponent<Renderer>().material.color = Color.cyan;
                break;
            case WeaponType.Bow:
                model.transform.localScale = new Vector3(0.1f, 0.8f, 0.2f);
                model.transform.localPosition = new Vector3(0.3f, 0.1f, 0.3f);
                model.GetComponent<Renderer>().material.color = new Color(0.6f, 0.3f, 0.1f);
                break;
            default:
                model.transform.localScale = new Vector3(0.12f, 0.12f, 0.6f);
                model.transform.localPosition = new Vector3(0.3f, 0f, 0.4f);
                model.GetComponent<Renderer>().material.color = Color.yellow;
                break;
        }

        return model;
    }

    private GameObject CreateDefaultProjectileObject(Vector3 pos, Vector3 dir, Color color)
    {
        GameObject proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        proj.name = "Projectile_Player";
        proj.transform.position = pos;
        proj.transform.localScale = Vector3.one * 0.35f;
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

using UnityEngine;

public class SpikeProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 18f;
    [SerializeField] private int damage = 25;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float hitRadius = 0.45f;

    private Vector3 moveDirection;
    private bool hasHit = false;
    private float spawnTime;

    public void Initialize(Vector3 direction, int damageAmount, float projectileSpeed = 18f, float maxLifetime = 5f)
    {
        this.moveDirection = direction.normalized;
        this.damage = damageAmount;
        this.speed = projectileSpeed;
        this.lifetime = maxLifetime;
        this.spawnTime = Time.time;

        transform.forward = this.moveDirection;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (hasHit) return;

        float distanceThisFrame = speed * Time.deltaTime;
        Vector3 nextPos = transform.position + moveDirection * distanceThisFrame;

        // Detección de impacto mediante Raycast continuo
        if (Physics.Raycast(transform.position, moveDirection, out RaycastHit hit, distanceThisFrame + 0.1f))
        {
            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Player") || hit.collider.GetComponent<PlayerHealth>() != null)
                {
                    PlayerHealth pHealth = hit.collider.GetComponent<PlayerHealth>();
                    if (pHealth == null) pHealth = hit.collider.GetComponentInParent<PlayerHealth>();

                    if (pHealth != null && !pHealth.IsDead)
                    {
                        pHealth.TakeDamage(damage, hit.point, moveDirection);
                    }
                    OnImpact(hit.point, true);
                    return;
                }
                else if (!hit.collider.isTrigger && !hit.collider.CompareTag("Enemy"))
                {
                    OnImpact(hit.point, false);
                    return;
                }
            }
        }

        transform.position = nextPos;
    }

    private void OnImpact(Vector3 point, bool hitPlayer)
    {
        hasHit = true;

        // Efecto de impacto
        GameObject impactEffect = new GameObject("Spike_Impact", typeof(Light));
        impactEffect.transform.position = point;
        Light l = impactEffect.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = hitPlayer ? Color.red : new Color(0.3f, 1f, 0.4f);
        l.range = 3.5f;
        l.intensity = 3f;
        Destroy(impactEffect, 0.2f);

        Destroy(gameObject);
    }

    public static SpikeProjectile CreateSpike(Vector3 position, Vector3 direction, int damage, float speed = 18f, Color? spikeColor = null)
    {
        GameObject spikeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spikeObj.name = "Mutant_Spike_Projectile";
        spikeObj.transform.position = position;
        spikeObj.transform.localScale = new Vector3(0.12f, 0.6f, 0.12f);
        spikeObj.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);

        // Desactivar colisionador físico estándar (usamos Raycast interno para máxima precisión)
        Collider col = spikeObj.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Renderer rend = spikeObj.GetComponent<Renderer>();
        Color finalColor = spikeColor.HasValue ? spikeColor.Value : new Color(0.85f, 0.2f, 0.15f);
        if (rend != null)
        {
            rend.material.color = finalColor;
            if (rend.material.HasProperty("_EmissionColor"))
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", finalColor * 1.5f);
            }
        }

        // Estela visual brillante
        TrailRenderer trail = spikeObj.AddComponent<TrailRenderer>();
        trail.time = 0.25f;
        trail.startWidth = 0.22f;
        trail.endWidth = 0.02f;
        trail.material = rend != null ? rend.material : new Material(Shader.Find("Sprites/Default"));
        trail.startColor = finalColor;
        trail.endColor = new Color(finalColor.r, finalColor.g, finalColor.b, 0f);

        SpikeProjectile spikeComp = spikeObj.AddComponent<SpikeProjectile>();
        spikeComp.Initialize(direction, damage, speed, 4.5f);

        return spikeComp;
    }
}

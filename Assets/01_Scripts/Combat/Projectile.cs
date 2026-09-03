using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Properties")]
    private int damage = 20;
    private float speed = 25f;
    private float maxLifetime = 4f;
    private bool isPlayerProjectile = true;
    private Vector3 moveDirection = Vector3.forward;

    [Header("Visuals")]
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private MeshRenderer meshRenderer;

    public void Initialize(int damage, float speed, float maxDistance, Vector3 direction, Color color, bool fromPlayer = true)
    {
        this.damage = damage;
        this.speed = speed;
        this.moveDirection = direction.normalized;
        this.isPlayerProjectile = fromPlayer;
        this.maxLifetime = maxDistance / Mathf.Max(speed, 1f);

        if (meshRenderer != null)
        {
            meshRenderer.material.color = color;
        }

        if (trailRenderer != null)
        {
            trailRenderer.startColor = color;
            trailRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
        }

        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        transform.position += moveDirection * (speed * Time.deltaTime);
        transform.forward = moveDirection;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si es proyectil del jugador, impacta enemigos
        if (isPlayerProjectile)
        {
            if (other.CompareTag("Enemy") || other.GetComponentInParent<EnemyController>() != null)
            {
                EnemyController enemy = other.GetComponent<EnemyController>();
                if (enemy == null) enemy = other.GetComponentInParent<EnemyController>();

                if (enemy != null && !enemy.IsDead)
                {
                    enemy.TakeDamage(damage, transform.position, moveDirection);
                    SpawnImpactEffect();
                    Destroy(gameObject);
                }
            }
            else if (other.CompareTag("Obstacle") || other.CompareTag("Wall") || other.gameObject.layer == LayerMask.NameToLayer("Default"))
            {
                if (!other.CompareTag("Player") && !other.isTrigger)
                {
                    SpawnImpactEffect();
                    Destroy(gameObject);
                }
            }
        }
        else
        {
            // Si es proyectil enemigo, impacta jugador
            if (other.CompareTag("Player") || other.GetComponent<PlayerHealth>() != null)
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null && !playerHealth.IsDead)
                {
                    playerHealth.TakeDamage(damage, transform.position, moveDirection);
                    SpawnImpactEffect();
                    Destroy(gameObject);
                }
            }
        }
    }

    private void SpawnImpactEffect()
    {
        // Destello rápido de impacto
        GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spark.transform.position = transform.position;
        spark.transform.localScale = Vector3.one * 0.2f;
        Collider col = spark.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        Renderer r = spark.GetComponent<Renderer>();
        if (r != null && meshRenderer != null)
        {
            r.material.color = meshRenderer.material.color;
        }
        Destroy(spark, 0.15f);
    }
}

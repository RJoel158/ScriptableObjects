using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Properties")]
    private int damage = 20;
    private float speed = 35f;
    private float maxLifetime = 4f;
    private bool isPlayerProjectile = true;
    private Vector3 moveDirection = Vector3.forward;
    private GameObject owner;

    [Header("Visuals")]
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private MeshRenderer meshRenderer;

    public void Initialize(int damage, float speed, float maxDistance, Vector3 direction, Color color, bool fromPlayer = true, GameObject shooter = null)
    {
        this.damage = damage;
        this.speed = speed > 0 ? speed : 35f;
        this.moveDirection = direction.normalized;
        this.isPlayerProjectile = fromPlayer;
        this.maxLifetime = maxDistance / Mathf.Max(this.speed, 1f);
        this.owner = shooter;

        // Ignorar colisiones físicas con el propio tirador
        if (shooter != null)
        {
            Collider myCollider = GetComponent<Collider>();
            if (myCollider != null)
            {
                Collider[] shooterColliders = shooter.GetComponentsInChildren<Collider>();
                foreach (var col in shooterColliders)
                {
                    Physics.IgnoreCollision(myCollider, col, true);
                }
            }
        }

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
        Vector3 step = moveDirection * (speed * Time.deltaTime);
        float stepDist = step.magnitude;

        // Detección continua de colisión (SphereCast) para evitar que balas rápidas atraviesen enemigos
        if (Physics.SphereCast(transform.position, 0.22f, moveDirection, out RaycastHit hit, stepDist, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && (owner == null || (hit.collider.gameObject != owner && !hit.transform.IsChildOf(owner.transform))))
            {
                transform.position = hit.point;
                OnTriggerEnter(hit.collider);
                return;
            }
        }

        transform.position += step;
        transform.forward = moveDirection;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // Ignorar si colisiona con el dueño del disparo
        if (owner != null && (other.gameObject == owner || other.transform.IsChildOf(owner.transform)))
        {
            return;
        }

        // Si es proyectil del jugador, impacta enemigos
        if (isPlayerProjectile)
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy == null) enemy = other.GetComponentInParent<EnemyController>();

            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(damage, transform.position, moveDirection);
                SpawnImpactEffect();
                Destroy(gameObject);
                return;
            }

            // Ignorar al jugador y triggers
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player == null && !other.isTrigger)
            {
                SpawnImpactEffect();
                Destroy(gameObject);
            }
        }
        else
        {
            // Si es proyectil enemigo, impacta jugador
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth == null) playerHealth = other.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null && !playerHealth.IsDead)
            {
                playerHealth.TakeDamage(damage, transform.position, moveDirection);
                SpawnImpactEffect();
                Destroy(gameObject);
            }
        }
    }

    private void SpawnImpactEffect()
    {
        GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spark.transform.position = transform.position;
        spark.transform.localScale = Vector3.one * 0.18f;
        Collider col = spark.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        Renderer r = spark.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = (meshRenderer != null) ? meshRenderer.material.color : Color.yellow;
        }
        Destroy(spark, 0.12f);
    }
}

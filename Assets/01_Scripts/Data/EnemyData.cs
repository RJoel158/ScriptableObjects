using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LootDrop
{
    public ItemData item;
    [Range(0f, 1f)] public float dropChance = 0.5f; // 50% de probabilidad
    [Min(1)] public int minQuantity = 1;
    [Min(1)] public int maxQuantity = 1;
}

public enum EnemyType
{
    GoblinMelee,
    SkeletonShooter,
    OrcBerserker,
    BossGolem
}

[CreateAssetMenu(fileName = "Enemy_New", menuName = "RPG/Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identificación")]
    public string enemyName = "Enemigo Base";
    public EnemyType enemyType = EnemyType.GoblinMelee;
    public GameObject enemyPrefab;

    [Header("Estadísticas Base")]
    [Min(1)] public int maxHealth = 100;
    [Min(1)] public int baseDamage = 15;
    [Min(0.5f)] public float moveSpeed = 3.5f;
    [Min(0.5f)] public float attackRange = 1.5f;
    [Min(0.1f)] public float attackCooldown = 1.2f;

    [Header("Botín (Loot) & Recompensas")]
    [Min(0)] public int minGoldDrop = 5;
    [Min(0)] public int maxGoldDrop = 15;
    public List<LootDrop> lootTable = new List<LootDrop>();

    [Header("Animaciones Opcionales")]
    public RuntimeAnimatorController animatorController;
    public Avatar enemyAvatar;

    [Header("Aspecto Visual & Altura")]
    [Tooltip("Ajuste de altura sobre el suelo (útil para mutantes o crawlers cuyo pivote está en la cintura)")]
    public float groundYOffset = 0f;
    public Color bodyColor = new Color(0.8f, 0.2f, 0.2f);
    public Vector3 scale = Vector3.one;
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Progression Settings")]
    [SerializeField] private int currentWave = 0;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private int baseEnemiesPerWave = 6;
    [SerializeField] private int enemiesIncreasePerWave = 3;
    [SerializeField] private float spawnRadius = 12f;

    [Header("Progressive Horde Flow")]
    [Tooltip("Cantidad de zombies que aparecen de golpe al arrancar la ronda")]
    [SerializeField] private int initialBurstCount = 4;
    [Tooltip("Máximo de zombies activos simultáneamente en pantalla para no saturar el mapa")]
    [SerializeField] private int maxSimultaneousEnemies = 7;
    [Tooltip("Intervalo en segundos entre cada nuevo enemigo que entra como refuerzo")]
    [SerializeField] private float progressiveSpawnInterval = 2.0f;

    [Header("Common Enemies (Horda Estándar)")]
    [Tooltip("Lista exclusiva para enemigos comunes (Zombis regulares). El jefe no se mezcla aquí.")]
    public List<EnemyData> enemyDataList = new List<EnemyData>();
    public GameObject enemyBasePrefab;

    [Header("Boss Configuration (Jefe de Hito)")]
    [Tooltip("ScriptableObject del Jefe Mutante (Enemy_MutantMonster)")]
    public EnemyData bossEnemyData;
    [Tooltip("Prefab directo del Jefe Mutante con MutantBossController")]
    public GameObject bossPrefab;
    [Tooltip("Cada cuántas rondas aparece el Jefe (ej: 5 para Ronda 5, 10, 15...)")]
    public int bossWaveInterval = 5;
    public bool enableBossWaves = true;

    [Header("Runtime State")]
    private int totalEnemiesInWave = 0;
    private int enemiesSpawnedSoFar = 0;
    private int enemiesKilledThisWave = 0;
    private bool isWaveInProgress = false;
    private float waveCountdownTimer = 0f;
    private bool bossSpawnedThisWave = false;

    private readonly List<GameObject> activeEnemiesList = new List<GameObject>();

    public int CurrentWave => currentWave;
    public int EnemiesAlive => activeEnemiesList.Count;
    public int EnemiesRemainingInWave => Mathf.Max(0, totalEnemiesInWave - enemiesKilledThisWave);
    public bool IsWaveInProgress => isWaveInProgress;

    // Eventos
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    public event Action<int, int> OnEnemyCountChanged; // (restantes, total)
    public event Action<float> OnWaveCountdownChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        CleanupEnemyDataList();
    }

    void Start()
    {
        StartCoroutine(WaveRoutine());
    }

    private void CleanupEnemyDataList()
    {
        if (enemyDataList != null)
        {
            // Mantener en enemyDataList únicamente enemigos comunes (no el jefe)
            enemyDataList.RemoveAll(e => e == null || e.name.ToLower().Contains("mutant") || e.enemyType == EnemyType.MutantBoss);
        }
        else
        {
            enemyDataList = new List<EnemyData>();
        }

        if (enemyDataList.Count == 0)
        {
            EnemyData[] found = Resources.FindObjectsOfTypeAll<EnemyData>();
            if (found != null && found.Length > 0)
            {
                foreach (var e in found)
                {
                    if (e != null && !e.name.ToLower().Contains("mutant") && e.enemyType != EnemyType.MutantBoss && !enemyDataList.Contains(e))
                    {
                        enemyDataList.Add(e);
                    }
                }
            }
        }

        // Auto-asignar bossEnemyData si está vacío
        if (bossEnemyData == null)
        {
            EnemyData[] found = Resources.FindObjectsOfTypeAll<EnemyData>();
            foreach (var e in found)
            {
                if (e != null && (e.name.ToLower().Contains("mutant") || e.enemyType == EnemyType.MutantBoss))
                {
                    bossEnemyData = e;
                    break;
                }
            }
        }
    }

    private IEnumerator WaveRoutine()
    {
        yield return new WaitForSeconds(1.2f);

        while (true)
        {
            currentWave++;
            totalEnemiesInWave = baseEnemiesPerWave + (currentWave - 1) * enemiesIncreasePerWave;
            enemiesSpawnedSoFar = 0;
            enemiesKilledThisWave = 0;
            activeEnemiesList.Clear();
            isWaveInProgress = true;

            CleanupEnemyDataList();

            OnWaveStarted?.Invoke(currentWave);
            OnEnemyCountChanged?.Invoke(totalEnemiesInWave, totalEnemiesInWave);

            Debug.Log($"[Oleadas] ¡Comenzando Oleada {currentWave}! Total de enemigos en esta ronda: {totalEnemiesInWave}");

            bool isBossWave = (currentWave % 5 == 0);
            if (isBossWave)
            {
                HUDUI hud = FindAnyObjectByType<HUDUI>();
                if (hud != null) hud.ShowNotification("ALERTA: ¡JEFE MUTANTE EN CAMINO!");
            }

            // Multiplicadores progresivos
            float healthMult = 1f + (currentWave - 1) * 0.25f;
            float damageMult = 1f + (currentWave - 1) * 0.20f;
            float speedMult = Mathf.Min(1.6f, 1f + (currentWave - 1) * 0.05f);

            // 1. RÁFAGA INICIAL: Generar un grupo inicial de zombies
            int burstToSpawn = Mathf.Min(initialBurstCount, totalEnemiesInWave);
            for (int i = 0; i < burstToSpawn; i++)
            {
                SpawnSingleEnemy(enemiesSpawnedSoFar, totalEnemiesInWave, healthMult, damageMult, speedMult);
                enemiesSpawnedSoFar++;
                yield return new WaitForSeconds(0.15f);
            }

            // Si es ronda de Jefe, spawnear al Jefe Mutante dedicado
            if (isBossWave && enableBossWaves)
            {
                yield return new WaitForSeconds(0.8f);
                SpawnBoss(healthMult * 1.5f, damageMult * 1.25f, speedMult);
            }

            // 2. REFUERZOS PROGRESIVOS: Ir soltando zombies poco a poco
            while (enemiesSpawnedSoFar < totalEnemiesInWave)
            {
                activeEnemiesList.RemoveAll(e => e == null);

                if (activeEnemiesList.Count < maxSimultaneousEnemies)
                {
                    SpawnSingleEnemy(enemiesSpawnedSoFar, totalEnemiesInWave, healthMult, damageMult, speedMult);
                    enemiesSpawnedSoFar++;
                    yield return new WaitForSeconds(progressiveSpawnInterval);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // 3. FASE FINAL DE LA RONDA: Esperar a que todos los enemigos generados sean eliminados
            while (enemiesKilledThisWave < totalEnemiesInWave || activeEnemiesList.Count > 0)
            {
                activeEnemiesList.RemoveAll(e => e == null);

                // Failsafe de seguridad: si ya no hay enemigos activos en escena pero no cuadró el contador, cerrar ronda
                if (enemiesSpawnedSoFar >= totalEnemiesInWave && activeEnemiesList.Count == 0)
                {
                    enemiesKilledThisWave = totalEnemiesInWave;
                    break;
                }

                yield return new WaitForSeconds(0.4f);
            }

            isWaveInProgress = false;
            OnWaveCompleted?.Invoke(currentWave);
            Debug.Log($"[Oleadas] ¡Oleada {currentWave} superada con éxito!");

            // Recompensa de Oro
            int waveBonusGold = isBossWave ? (100 + currentWave * 20) : (25 + currentWave * 10);
            if (Inventory.Instance != null)
            {
                Inventory.Instance.AddGold(waveBonusGold);
                HUDUI hud = FindAnyObjectByType<HUDUI>();
                if (hud != null)
                {
                    hud.ShowNotification(isBossWave 
                        ? $"¡JEFE DERROTADO! +{waveBonusGold} Oro" 
                        : $"¡Ronda {currentWave} superada! +{waveBonusGold} Oro");
                }
            }

            // Descanso entre rondas con cuenta regresiva
            waveCountdownTimer = timeBetweenWaves;
            while (waveCountdownTimer > 0)
            {
                OnWaveCountdownChanged?.Invoke(waveCountdownTimer);
                yield return new WaitForSeconds(1f);
                waveCountdownTimer -= 1f;
            }
        }
    }

    public void SpawnBoss(float healthMult, float damageMult, float speedMult)
    {
        Vector3 spawnPos = GetSpawnPosition(0, 1, (bossEnemyData != null) ? bossEnemyData.groundYOffset : 0.65f);
        GameObject bossObj = null;

        if (bossPrefab != null)
        {
            bossObj = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        }
        else if (bossEnemyData != null && bossEnemyData.enemyPrefab != null)
        {
            bossObj = Instantiate(bossEnemyData.enemyPrefab, spawnPos, Quaternion.identity);
        }

        if (bossObj == null)
        {
            // Cargar prefab del mutante si no estaba asignado
            GameObject loadedPrefab = Resources.Load<GameObject>("Base mesh MonsterMutant7 skin1");
            if (loadedPrefab != null)
            {
                bossObj = Instantiate(loadedPrefab, spawnPos, Quaternion.identity);
            }
        }

        if (bossObj == null) return;
        bossObj.name = "Mutant_Boss";

        MutantBossController bossCtrl = bossObj.GetComponent<MutantBossController>();
        if (bossCtrl == null) bossCtrl = bossObj.AddComponent<MutantBossController>();

        bossCtrl.Initialize(bossEnemyData, healthMult, damageMult, speedMult);

        if (!activeEnemiesList.Contains(bossObj))
        {
            activeEnemiesList.Add(bossObj);
        }
    }

    private void SpawnSingleEnemy(int index, int total, float healthMult, float damageMult, float speedMult)
    {
        if (enemyDataList == null || enemyDataList.Count == 0)
        {
            CleanupEnemyDataList();
        }

        EnemyData chosenData = null;
        if (enemyDataList != null && enemyDataList.Count > 0)
        {
            List<EnemyData> validList = enemyDataList.FindAll(e => e != null);
            if (validList.Count > 0)
            {
                int enemyIndex = UnityEngine.Random.Range(0, validList.Count);
                chosenData = validList[enemyIndex];
            }
        }

        Vector3 spawnPos = GetSpawnPosition(index, total, chosenData != null ? chosenData.groundYOffset : 0f);

        GameObject enemyObj = null;
        if (chosenData != null && chosenData.enemyPrefab != null)
        {
            enemyObj = Instantiate(chosenData.enemyPrefab, spawnPos, Quaternion.identity);
        }
        else if (enemyBasePrefab != null)
        {
            enemyObj = Instantiate(enemyBasePrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            enemyObj = CreateProceduralEnemyObject(spawnPos, chosenData);
        }

        if (enemyObj == null) return;

        EnemyController controller = enemyObj.GetComponent<EnemyController>();
        if (controller == null) controller = enemyObj.AddComponent<EnemyController>();

        controller.Initialize(chosenData, healthMult, damageMult, speedMult);

        if (!activeEnemiesList.Contains(enemyObj))
        {
            activeEnemiesList.Add(enemyObj);
        }

        OnEnemyCountChanged?.Invoke(EnemiesRemainingInWave, totalEnemiesInWave);
    }

    private Vector3 GetSpawnPosition(int index, int total, float yOffset = 0f)
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        Vector3 center = player != null ? player.transform.position : Vector3.zero;

        float baseAngle = (total > 0) ? (index * (360f / total)) : UnityEngine.Random.Range(0f, 360f);
        float angle = (baseAngle + UnityEngine.Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
        float dist = UnityEngine.Random.Range(spawnRadius * 0.85f, spawnRadius * 1.25f);

        float x = center.x + Mathf.Sin(angle) * dist;
        float z = center.z + Mathf.Cos(angle) * dist;

        return new Vector3(x, yOffset, z);
    }

    private GameObject CreateProceduralEnemyObject(Vector3 pos, EnemyData data)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = data != null ? $"Enemy_{data.enemyName}" : "Enemy_Procedural";
        enemy.tag = "Enemy";
        enemy.transform.position = pos;

        Renderer r = enemy.GetComponent<Renderer>();
        if (r != null && data != null)
        {
            r.material.color = data.bodyColor;
        }

        Rigidbody rb = enemy.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        return enemy;
    }

    public void OnEnemyDefeated(GameObject enemyObj)
    {
        if (enemyObj != null && activeEnemiesList.Contains(enemyObj))
        {
            activeEnemiesList.Remove(enemyObj);
        }
        else
        {
            activeEnemiesList.RemoveAll(e => e == null);
        }

        enemiesKilledThisWave++;
        OnEnemyCountChanged?.Invoke(EnemiesRemainingInWave, totalEnemiesInWave);
    }

    public void OnEnemyDefeated(EnemyController enemy)
    {
        if (enemy != null) OnEnemyDefeated(enemy.gameObject);
    }

    public void UnregisterActiveEnemy(EnemyController enemy)
    {
        if (enemy != null) OnEnemyDefeated(enemy.gameObject);
    }
}

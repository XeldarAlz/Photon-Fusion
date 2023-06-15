using Fusion;
using UnityEngine;
using static Fusion.TickTimer;

// This class controls the spawning of enemies in the game.
public class EnemySpawner : NetworkBehaviour
{
    #region Singleton

    // Singleton pattern: instance field and property.
    private static EnemySpawner _instance;

    public static EnemySpawner Instance
    {
        get
        {
            // Check if the instance is null.
            if (_instance == null)
            {
                // Try to find an existing LevelController object in the scene.
                _instance = FindObjectOfType<EnemySpawner>();

                // If no LevelController object exists, create a new one.
                if (_instance == null)
                {
                    var singletonObject = new GameObject("EnemySpawner");
                    _instance = singletonObject.AddComponent<EnemySpawner>();
                }

                // Ensure the singleton object persists across scenes.
                DontDestroyOnLoad(_instance.gameObject);
            }

            // Return the instance.
            return _instance;
        }
    }

    #endregion

    // Cooldown timer to control enemy spawn frequency.
    [Networked] private TickTimer Cooldown { get; set; }

    // Prefab of the enemy that will be spawned.
    [SerializeField] private Enemy enemyPrefab;

    // Array of enemy settings. Each element defines a different type of enemy.
    [SerializeField] public EnemyData[] enemySettings;

    // Time in seconds between spawns.
    [SerializeField] private float spawnDelay;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    // Initialize the spawn cooldown.
    public void Init()
    {
        Cooldown = CreateFromSeconds(Runner, spawnDelay);
    }

    // Overridden method from NetworkBehaviour which is called every networked physics frame.
    public override void FixedUpdateNetwork()
    {
        // Check if the network spawner is initialized before spawning enemies.
        if (NetworkSpawner.Instance.Initialized)
            // Check if the cooldown timer has expired or hasn't started yet.
            if (Cooldown.ExpiredOrNotRunning(Runner))
                // Only the server spawns new enemies.
                if (Runner.IsServer)
                {
                    SpawnEnemy();
                    // Reset the cooldown.
                    Cooldown = CreateFromSeconds(Runner, spawnDelay);
                }
    }

    // Method to spawn a new enemy.
    private void SpawnEnemy()
    {
        // Create a new enemy instance.
        var networkEnemyObject =
            Runner.Spawn(enemyPrefab, Vector3.zero, Quaternion.identity, Object.InputAuthority);

        // Choose a random type of enemy from the settings array.
        var enemyDataIndex = Random.Range(0, enemySettings.Length);

        // Initialize the enemy.
        networkEnemyObject.Init();

        // Set the enemy data over the network.
        networkEnemyObject.RPC_SetEnemyData(enemyDataIndex);
    }
}
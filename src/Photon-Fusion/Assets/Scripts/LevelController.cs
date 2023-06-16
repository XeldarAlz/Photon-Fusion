using Fusion;
using UnityEngine;

// This class controls the overall logic of the level
public class LevelController : MonoBehaviour
{
    #region Singleton
    // Singleton pattern: instance field and property.
    private static LevelController _instance;
    public static LevelController Instance
    {
        get
        {
            // Check if the instance is null.
            if (_instance == null)
            {
                // Try to find an existing LevelController object in the scene.
                _instance = FindObjectOfType<LevelController>();

                // If no LevelController object exists, create a new one.
                if (_instance == null)
                {
                    var singletonObject = new GameObject("LevelController");
                    _instance = singletonObject.AddComponent<LevelController>();
                }

                // Ensure the singleton object persists across scenes.
                DontDestroyOnLoad(_instance.gameObject);
            }

            // Return the instance.
            return _instance;
        }
    }

    #endregion

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    // Delegate and event for game over scenario.
    public delegate void OnPlayerEliminatedDelegate();
    public event OnPlayerEliminatedDelegate OnPlayerEliminated;

    // Delegate and event for when an enemy goes out of bounds.
    public delegate void OnEnemyOutOfBoundsDelegate(EnemyData enemyData);
    public event OnEnemyOutOfBoundsDelegate OnEnemyOutOfBounds;

    public delegate void OnPlayerCollidedWithEnemyDelegate(Player player, EnemyData enemy);
    public event OnPlayerCollidedWithEnemyDelegate OnPlayerCollidedWithEnemy; 

    // Method to call when the game is over.
    public void PlayerEliminated()
    {
        OnPlayerEliminated?.Invoke();
    }

    // Method to call when an enemy goes out of bounds.
    public void EnemyOutOfBounds(EnemyData enemyData)
    {
        OnEnemyOutOfBounds?.Invoke(enemyData);
    }

    public void PlayerCollidedWithEnemy(Player player, EnemyData enemy)
    {
        OnPlayerCollidedWithEnemy?.Invoke(player, enemy);
    }
}
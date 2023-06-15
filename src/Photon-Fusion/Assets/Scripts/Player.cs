using System;
using Fusion;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

// Player class derived from NetworkBehaviour in the Fusion library, allows synchronized multiplayer interactions
public class Player : NetworkBehaviour
{
    // Variables for player attributes, exposed in Unity inspector for easy tweaking
    [SerializeField] private float speed;
    [SerializeField] private int startingHealth;
    [SerializeField] public GameObject body;

    // Networked properties, synchronized across all clients in a multiplayer session
    [Networked] private Color PlayerColor { get; set; }
    [Networked] private int PlayerHealth { get; set; }
    [Networked] private int PlayerIndex { get; set; }
    [Networked] private int PlayerScore { get; set; }

    // Components and variables used within this script
    private TextMeshPro _playerNameText;
    private SpriteRenderer _spriteRenderer;
    private BoxCollider2D _boxCollider2D;
    private float _halfMeshSize;
    private bool _disabled;  // Boolean flag to check if the player is currently disabled
    
    private void Awake()
    {
        // Get required components
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _playerNameText = GetComponentInChildren<TextMeshPro>();
        _halfMeshSize = _spriteRenderer.size.x / 2 * transform.localScale.x;
    }
    
    private void OnEnable()
    {
        LevelController.Instance.OnEnemyOutOfBounds += OnEnemyOutOfBounds;
        LevelController.Instance.OnPlayerCollidedWithEnemy += OnPlayerCollidedWithEnemy;
    }
    
    private void OnDisable()
    {
        LevelController.Instance.OnEnemyOutOfBounds -= OnEnemyOutOfBounds;
        LevelController.Instance.OnPlayerCollidedWithEnemy -= OnPlayerCollidedWithEnemy;
    }
    
    // Method called when the player is spawned
    public override void Spawned()
    {
        // Initialize player's attributes
        PlayerColor = Color.white;
        PlayerHealth = startingHealth;
        PlayerIndex = 0;
        PlayerScore = 0;
    }
    
    // Method to initialize player's data, networked via Fusion RPC
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Init()
    {
        // Assign a random color and index number to the player
        PlayerColor = Random.ColorHSV();
        PlayerIndex = Runner.SessionInfo.PlayerCount;
    }
    
    // Render method, called every frame
    public override void Render()
    {
        // Different rendering based on whether player is enabled or disabled
        if (_disabled)
        {
            // If player is disabled, make player's sprite transparent and remove text
            _spriteRenderer.color = Color.clear;
            _playerNameText.text = String.Empty;
        }
        else
        {
            // If player is enabled, render player's sprite and name text
            _spriteRenderer.color = PlayerColor;
            _playerNameText.text = $"Player {PlayerIndex} \n Health: {PlayerHealth} \n Score: {PlayerScore}";
        }
    }

    // FixedUpdateNetwork is called at a fixed interval
    public override void FixedUpdateNetwork()
    {
        // Get input data and move the player
        if (GetInput(out NetworkInputData data))
        {
            // Normalize direction vector for consistent speed in all directions
            data.Direction.Normalize();
            Move(data.Direction * Runner.DeltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the collided object is an enemy
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            var collidedEnemy = other.gameObject.GetComponent<Enemy>();
            var enemyData = collidedEnemy.EnemyData;

            // Check if enemy is active before handling collision
            if (collidedEnemy.gameObject.activeInHierarchy)
            {
                LevelController.Instance.PlayerCollidedWithEnemy(this, enemyData);
            }

            // Destroy the enemy after handling collision
            collidedEnemy.DestroyEnemy();
        }
    }
    
    // Method to move player in a specific direction
    private void Move(Vector3 direction)
    {
        // Translate the player's position and clamp it within screen bounds
        transform.Translate(direction * speed);
        var position = transform.position;
        position.x = Mathf.Clamp(position.x, ScreenBorders.BottomLeft.x + _halfMeshSize, ScreenBorders.BottomRight.x - _halfMeshSize);
        transform.position = position;
    }

    // Method to deal damage to the player
    private void DealDamage(int damage)
    {
        // Reduce player's health by the damage amount or eliminate player if health is depleted
        if (PlayerHealth - damage > 1)
        {
            PlayerHealth -= damage;
        }
        else
        {
            OnPlayerEliminated();
            LevelController.Instance.PlayerEliminated();
        }
    }
    
    // Method to increase player's score
    private void IncreaseScore(int amount)
    {
        PlayerScore += amount;
    }
    
    // Event handler for when an enemy goes out of bounds
    private void OnEnemyOutOfBounds(EnemyData enemyData)
    {
        IncreaseScore(enemyData.Score);
    }
    
    // Event handler for when the player collides with an enemy
    private void OnPlayerCollidedWithEnemy(Player player, EnemyData enemy)
    {
        // Differentiate between self-collision and other player's collision with enemy
        if (player != this)
        {
            // If another player collides with an enemy, increase this player's score
            IncreaseScore(enemy.Score);
        }
        else
        {
            // If this player collides with an enemy, deal damage to this player
            DealDamage(enemy.Score);
        }
    }
    
    // Method to reset player data across all clients, networked with Fusion RPC
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ResetPlayerData()
    {
        // Re-enable the player
        _disabled = false;
        PlayerHealth = startingHealth;
        PlayerScore = 0;
        _boxCollider2D.enabled = true;
        body.SetActive(true);
    }
    
    // Method to disable the player when they are eliminated
    private void OnPlayerEliminated()
    {
        _disabled = true;
        _boxCollider2D.enabled = false;
        body.SetActive(false);
    }
}

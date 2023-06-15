using Fusion;
using TMPro;
using UnityEngine;

// This class represents the player character, networked via Fusion.
public class Player : NetworkBehaviour
{
    // These variables hold player properties, exposed in the Unity inspector.
    [SerializeField] private float speed;
    [SerializeField] private int startingHealth;
    [SerializeField] private TextMeshPro playerNameText;

    // These properties are networked, they hold the player's visual color and health.
    [Networked] private Color PlayerColor { get; set; }
    [Networked] private int PlayerHealth { get; set; }
    [Networked] private int PlayerIndex { get; set; }
    [Networked] private int PlayerScore { get; set; }

    // Variables for internal usage
    private SpriteRenderer _spriteRenderer;
    private int _score;
    private float _halfMeshSize;

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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
        LevelController.Instance.OnPlayerCollidedWithEnemy += OnPlayerCollidedWithEnemy;
    }

    // Method called when this player is eliminated.
    private void OnPlayerEliminated()
    {
        // Reset player health and score.
        PlayerHealth = startingHealth;
        PlayerScore = 0;
        gameObject.SetActive(false);
    }

    // Method called when an enemy is out of bounds.
    private void OnEnemyOutOfBounds(EnemyData enemyData)
    {
        // Increment score by the score value of the enemy.
        RPC_IncreaseScore(enemyData.Score);
    }
    
    private void OnPlayerCollidedWithEnemy(Player player, EnemyData enemy)
    {
        if (player != this)
        {
            RPC_IncreaseScore(enemy.Score);
        }
        else
        {
            DealDamage(enemy.Score);
        }
    }

    // Method called when the player is spawned.
    public override void Spawned()
    {
        // Initialize player networked variables
        PlayerColor = Color.white;
        PlayerHealth = startingHealth;
        PlayerIndex = 0;
        PlayerScore = 0;
    }

    // Method for initializing player color. This method can be called over the network.
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Init()
    {
        // Set a random color.
        PlayerColor = Random.ColorHSV();
        PlayerIndex = Runner.SessionInfo.PlayerCount;
    }

    // Method called every frame for rendering.
    public override void Render()
    {
        // Interpolate the player's color to the new color.
        _spriteRenderer.color = PlayerColor;
        playerNameText.text = $"Player {PlayerIndex} \n Health: {PlayerHealth} \n Score: {PlayerScore}";
    }

    // Method called every network tick.
    public override void FixedUpdateNetwork()
    {
        // Get input and move the player.
        if (GetInput(out NetworkInputData data))
        {
            data.Direction.Normalize();
            Move(data.Direction * Runner.DeltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the other object is an enemy.
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            // If so, destroy the enemy and deal damage to the player.
            var collidedEnemy = other.gameObject.GetComponent<Enemy>();
            var enemyData = collidedEnemy.EnemyData;

            if (collidedEnemy.gameObject.activeInHierarchy)
            {
                LevelController.Instance.PlayerCollidedWithEnemy(this, enemyData);
            }

            collidedEnemy.DestroyEnemy();
        }
    }

    // Method for moving the player.
    private void Move(Vector3 direction)
    {
        // Translate the player and keep it within screen bounds.
        transform.Translate(direction * speed);
        var position = transform.position;
        position.x = Mathf.Clamp(position.x, ScreenBorders.BottomLeft.x + _halfMeshSize,
            ScreenBorders.BottomRight.x - _halfMeshSize);
        transform.position = position;
    }

    // Method for dealing damage to the player. This method can be called over the network.
    private void DealDamage(int damage)
    {
        // Subtract the damage from the player's health or eliminate the player if health is depleted.
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

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_IncreaseScore(int amount)
    {
        PlayerScore += amount;
    }
}
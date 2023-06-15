using Fusion;
using UnityEngine;

// This class represents the player character, networked via Fusion.
public class Player : NetworkBehaviour
{
    // These variables hold player properties, exposed in the Unity inspector.
    [SerializeField] private float speed;
    [SerializeField] private int startingHealth;

    // These properties are networked, they hold the player's visual color and health.
    [Networked] private Color PlayerColor { get; set; }
    [Networked] private int PlayerHealth { get; set; }

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
        LevelController.Instance.OnGameOver += OnGameOver;
        LevelController.Instance.OnEnemyOutOfBounds += OnEnemyOutOfBounds;
    }

    private void OnDisable()
    {
        LevelController.Instance.OnGameOver -= OnGameOver;
        LevelController.Instance.OnEnemyOutOfBounds -= OnEnemyOutOfBounds;
    }

    // Method called when the game is over.
    private void OnGameOver()
    {
        // Reset player health and score.
        PlayerHealth = startingHealth;
        _score = 0;
    }

    // Method called when an enemy is out of bounds.
    private void OnEnemyOutOfBounds(EnemyData enemyData)
    {
        // Increment score by the score value of the enemy.
        _score += enemyData.Score;
    }

    // Method called when the player is spawned.
    public override void Spawned()
    {
        // Initialize player color, health and score.
        PlayerColor = Color.white;
        PlayerHealth = startingHealth;
        _score = 0;
    }

    // Method for initializing player color. This method can be called over the network.
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Init()
    {
        // Set a random color.
        PlayerColor = Random.ColorHSV();
    }

    // Method called every frame for rendering.
    public override void Render()
    {
        // Interpolate the player's color to the new color.
        _spriteRenderer.color = Color.Lerp(_spriteRenderer.color, PlayerColor, 1);
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

    // Method called when a collider enters the trigger.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the other object is an enemy.
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            // If so, destroy the enemy and deal damage to the player.
            var collidedEnemy = other.gameObject.GetComponent<Enemy>();
            collidedEnemy.DestroyEnemy();
            RPC_DealDamage(collidedEnemy.damage);
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
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_DealDamage(int damage)
    {
        // Subtract the damage from the player's health and end the game if health is zero.
        if (PlayerHealth - damage > 0)
            PlayerHealth -= damage;
        else
            LevelController.Instance.GameOver();
    }
}
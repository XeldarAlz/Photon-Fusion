using Fusion;
using UnityEngine;

// The Enemy class is responsible for controlling individual enemy behavior in the game. 
// This includes handling its network properties, initial placement, movement, and removal from the game when necessary.
public class Enemy : NetworkBehaviour
{
    // These properties are networked so that they stay synchronized across all instances of the game.
    [Networked] private int EnemyIndex { get; set; }
    [Networked] private float Speed { get; set; }
    [Networked] private int Damage { get; set; }
    [Networked] private int Score { get; set; }

    // These public variables hold the Damage and Score values for use in non-networked game code.
    public int damage;
    public int score;

    // The Sprite of this enemy and the Renderer to display it.
    private Sprite Sprite { get; set; }
    private SpriteRenderer _spriteRenderer;

    // Holds data specific to this enemy.
    private EnemyData _data;

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // Spawned is called when the enemy is spawned into the game. This sets the initial networked properties.
    public override void Spawned()
    {
        Speed = 0;
        Damage = 0;
        Score = 0;
    }

    public void Init()
    {
        // Set enemy position to random x value within screen borders and at top of screen
        transform.position = new Vector3(Random.Range(LeftTopCornerX(), RightTopCornerX()),
            ScreenBorders.TopLeft.y, 0);
    }

    // FixedUpdateNetwork is called on every network tick. This handles the movement of the enemy and checks if it has gone out of bounds.
    public override void FixedUpdateNetwork()
    {
        // Move enemy down the screen at a constant speed
        transform.Translate(Vector3.down * Speed * Runner.DeltaTime, Space.World);

        // Check if enemy is within screen borders
        if (!IsWithinBorders())
        {
            // If not, notify level controller and destroy enemy
            LevelController.Instance.EnemyOutOfBounds(_data);
            DestroyEnemy();
        }
    }

    // Render updates the enemy's sprite each frame.
    public override void Render()
    {
        _spriteRenderer.sprite = Sprite;
    }

    // RPC_SetEnemyData is a networked function that sets the enemy's properties from a given index in the enemy settings.
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetEnemyData(int enemyIndex)
    {
        EnemyIndex = enemyIndex;
        _data = EnemySpawner.Instance.enemySettings[EnemyIndex];
        Sprite = _data.Sprite;
        Speed = _data.Speed;
        Damage = _data.Damage;
        damage = Damage;
        Score = _data.Score;
        score = Score;
        _spriteRenderer.sprite = Sprite;
    }

    // DestroyEnemy is called when the enemy has gone out of bounds and needs to be removed.
    public void DestroyEnemy()
    {
        Runner.Despawn(Object);
    }

    // IsWithinBorders checks if the enemy is still within the boundaries of the screen.
    private bool IsWithinBorders()
    {
        return transform.position.y + transform.lossyScale.y / 2 * _spriteRenderer.size.y >=
               ScreenBorders.BottomLeft.y;
    }

    // These functions calculate the X coordinates of the left and right top corners of the screen.
    private float LeftTopCornerX()
    {
        return ScreenBorders.TopLeft.x + transform.lossyScale.x / 2 * _spriteRenderer.size.x;
    }

    private float RightTopCornerX()
    {
        return ScreenBorders.TopRight.x - transform.lossyScale.x / 2 * _spriteRenderer.size.x;
    }
}
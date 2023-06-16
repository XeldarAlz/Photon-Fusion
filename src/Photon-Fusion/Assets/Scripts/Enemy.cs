using Fusion;
using UnityEngine;
using UnityEngine.Serialization;

// The Enemy class controls the behavior of the individual enemies in the game, and handles their network properties.
// It is responsible for their initial placement, movement, and destruction when necessary.
public class Enemy : NetworkBehaviour
{
    // Networked properties for individual enemy instances to keep them synchronized across 
    [Networked] private int EnemyIndex { get; set; }
    [Networked] private float Speed { get; set; }
    [Networked] private int Damage { get; set; }
    [Networked] private int Score { get; set; }

    // SpriteRenderer is used to display the enemy sprite on the screen.
    private Sprite _sprite;
    private SpriteRenderer _spriteRenderer;

    // Holds data specific to this enemy.
    public EnemyData EnemyData { get; private set; }

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // The Spawned method is called when an enemy is spawned into the game, and initializes the networked properties.
    public override void Spawned()
    {
        Speed = 0;
        Damage = 0;
        Score = 0;
    }

    // Init method positions the enemy at a random x-value within screen borders and at the top of the screen.
    public void Init()
    {
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
            LevelController.Instance.EnemyOutOfBounds(EnemyData);
            DestroyEnemy();
        }
    }

    // Render updates the enemy's sprite each frame.
    public override void Render()
    {
        _spriteRenderer.sprite = _sprite;
    }

    // Networked function that sets the enemy's properties from a given index in the enemy settings.
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetEnemyData(int enemyIndex)
    {
        EnemyIndex = enemyIndex;
        EnemyData = EnemySpawner.Instance.enemySettings[EnemyIndex];
        _sprite = EnemyData.Sprite;
        Speed = EnemyData.Speed;
        Damage = EnemyData.Damage;
        Score = EnemyData.Score;
        _spriteRenderer.sprite = _sprite;
    }

    // Called when the enemy has gone out of bounds or needs to be removed.
    public void DestroyEnemy()
    {
        if (Object)
        {
            Runner.Despawn(Object);
        }
    }

    // Checks if the enemy is still within the boundaries of the screen.
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
using UnityEngine;

// This class represents the settings of an enemy object within the game.
[CreateAssetMenu(menuName = "Enemies/Default enemy", fileName = "New enemy")]
public class EnemyData : ScriptableObject
{
    [SerializeField] private Sprite _sprite;
    public Sprite Sprite => _sprite;

    [SerializeField] private float _speed;
    public float Speed => _speed;

    [SerializeField] private int _damage;
    public int Damage => _damage;

    [SerializeField] private int _score;
    public int Score => _score;
}
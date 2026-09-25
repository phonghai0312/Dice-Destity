using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Dice & Destiny/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public int maxHP;
    public int startingShield;
    public Sprite enemySprite;
    public Sprite backgroundSprite;
}

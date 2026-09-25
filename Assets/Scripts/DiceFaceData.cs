using UnityEngine;

public enum DiceFaceType { Attack, Defend, Heal, Special, Status }
public enum DiceFaceRarity { Common, Rare, Epic }

[CreateAssetMenu(fileName = "NewDiceFace", menuName = "Dice & Destiny/Dice Face")]
public class DiceFaceData : ScriptableObject
{
    public string faceName;
    public Sprite icon;
    public DiceFaceType type;
    public int valueV;
    public DiceFaceRarity rarity;
}

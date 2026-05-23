using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "youpzdev/characters/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public Sprite icon;
    public GameObject prefab3D;
    public int price;
}
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "youpzdev/characters/Character Data")]
public class CharacterData : ScriptableObject
{
    [Serializable]
    public class ColorVariant
    {
        public string variantName;
        public Color tint = Color.white;
        public Material material;
    }

    public string characterName;
    public Sprite icon;
    public GameObject prefab3D;
    public float previewRotation;
    public ColorVariant[] colorVariants;

    public int VariantCount => colorVariants != null ? colorVariants.Length : 0;

    public int PickRandomVariantIndex()
    {
        int count = VariantCount;
        return count > 0 ? UnityEngine.Random.Range(0, count) : -1;
    }

    public ColorVariant GetVariant(int index)
    {
        if (index < 0 || index >= VariantCount) return null;
        return colorVariants[index];
    }
}

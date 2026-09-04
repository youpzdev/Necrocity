using UnityEngine;

public class PlacedObject : MonoBehaviour
{
    [SerializeField] private ItemType type;

    public ItemType Type => type;

    public void Init(ItemType itemType) => type = itemType;
}

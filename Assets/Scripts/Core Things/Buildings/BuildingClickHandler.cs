using UnityEngine;

[DisallowMultipleComponent]
public class BuildingClickHandler : MonoBehaviour
{
    private IClickableBuilding building;

    private void Awake()
    {
        building = GetComponentInParent<IClickableBuilding>();
    }

    public void HandleClick()
    {
        building?.OnClick();
    }
}

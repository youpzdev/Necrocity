using System;
using System.Collections.Generic;
using UnityEngine;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CraftingConfig craftingConfig;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform placedRoot;
    [SerializeField] private PlacementControlPanel controlPanel;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float maxRayDistance = 500f;

    [Header("Rotation")]
    [SerializeField] private float rotationStep = 45f;

    private readonly List<PlacedObject> placed = new();

    private PlacementDragInput drag;
    private PlacedObject active;

    public bool IsPlacing => active != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (worldCamera == null) worldCamera = Camera.main;
        drag = new PlacementDragInput(worldCamera, groundLayers, maxRayDistance);
    }

    private void Start()
    {
        LoadPlaced();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (active == null) return;
        drag.Tick(active.transform);
    }

    public bool BeginPlacement(ItemType type)
    {
        if (active != null) return false;
        if (InventoryManager.Instance == null || InventoryManager.Instance.GetItem(type) <= 0) return false;

        PlacedObject spawned = Spawn(type, FindStartPosition(), 0f);
        if (spawned == null) return false;

        active = spawned;
        drag.Reset();

        if (controlPanel != null) controlPanel.Open();
        return true;
    }

    public void RotateClockwise() => RotateActive(rotationStep);

    public void RotateCounterClockwise() => RotateActive(-rotationStep);

    public void SendActiveToStorage()
    {
        if (active == null) return;

        Pooling.Destroy(active.gameObject);
        active = null;
        drag.Reset();

        if (controlPanel != null) controlPanel.Close();
    }

    public void ConfirmActive()
    {
        if (active == null) return;

        ItemType type = active.Type;
        if (InventoryManager.Instance.GetItem(type) > 0) InventoryManager.Instance.AddItem(type, -1);

        placed.Add(active);
        active = null;
        drag.Reset();
        SavePlaced();

        if (controlPanel != null) controlPanel.Close();
    }

    private void RotateActive(float degrees)
    {
        if (active == null) return;
        active.transform.Rotate(0f, degrees, 0f, Space.World);
    }

    private PlacedObject Spawn(ItemType type, Vector3 position, float rotationY)
    {
        ItemData data = craftingConfig.GetItemData(type);
        if (data == null || data.prefab3D == null)
        {
            Debug.LogError($"Prefab not found for item {type}", this);
            return null;
        }

        GameObject instance = Pooling.Instantiate(data.prefab3D, placedRoot);
        instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotationY, 0f));

        StripColliders(instance);

        PlacedObject placedObject = instance.GetComponent<PlacedObject>();
        if (placedObject == null) placedObject = instance.AddComponent<PlacedObject>();
        placedObject.Init(type);

        return placedObject;
    }

    private Vector3 FindStartPosition()
    {
        if (worldCamera == null) return Vector3.zero;

        Ray ray = worldCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundLayers, QueryTriggerInteraction.Ignore))
            return hit.point;

        return Vector3.zero;
    }

    private static void StripColliders(GameObject instance)
    {
        var colliders = instance.GetComponentsInChildren<Collider>(true);
        foreach (var item in colliders) Destroy(item);
    }

    private void SavePlaced()
    {
        var states = new List<PlacedObjectState>(placed.Count);

        foreach (var item in placed)
        {
            if (item == null) continue;

            Vector3 position = item.transform.position;
            states.Add(new PlacedObjectState
            {
                Type = item.Type.ToString(),
                X = position.x,
                Y = position.y,
                Z = position.z,
                Rotation = item.transform.eulerAngles.y
            });
        }

        GameSave.Set(GameSave.Keys.PlacedObjects, states);
    }

    private void LoadPlaced()
    {
        var states = GameSave.Get<List<PlacedObjectState>>(GameSave.Keys.PlacedObjects, null);
        if (states == null) return;

        foreach (var state in states)
        {
            if (state == null) continue;
            if (!Enum.TryParse(state.Type, out ItemType type)) continue;
            if (!Enum.IsDefined(typeof(ItemType), type)) continue;

            PlacedObject spawned = Spawn(type, new Vector3(state.X, state.Y, state.Z), state.Rotation);
            if (spawned != null) placed.Add(spawned);
        }
    }
}

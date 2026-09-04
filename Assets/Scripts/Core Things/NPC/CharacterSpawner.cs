using System.Collections;
using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Dormitory dormitory;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform waypointsRoot;
    [SerializeField] private CameraController cameraController;

    [Header("Camera Focus")]
    [SerializeField] private float focusDuration = 0.6f;
    [SerializeField] private float focusHold = 2f;

    [Header("Coloring")]
    [SerializeField] private string colorProperty = "_BaseColor";

    private MaterialPropertyBlock _block;
    private int _colorId;
    private NPCWaypoint[] _waypoints;
    private Coroutine _focusRoutine;

    private void Awake()
    {
        _block = new MaterialPropertyBlock();
        _colorId = Shader.PropertyToID(string.IsNullOrEmpty(colorProperty) ? "_BaseColor" : colorProperty);
        _waypoints = CollectWaypoints();
    }

    private void OnEnable()
    {
        if (dormitory != null) dormitory.ResidentAdded += OnResidentAdded;
    }

    private void OnDisable()
    {
        if (dormitory != null) dormitory.ResidentAdded -= OnResidentAdded;
    }

    private void Start()
    {
        if (dormitory == null) return;

        foreach (var resident in dormitory.Residents) Spawn(resident);
    }

    private NPCWaypoint[] CollectWaypoints()
    {
        if (waypointsRoot == null) return System.Array.Empty<NPCWaypoint>();

        var found = waypointsRoot.GetComponentsInChildren<NPCWaypoint>(true);
        if (found == null || found.Length == 0)
        {
            Debug.LogWarning($"[CharacterSpawner] {name}: под {waypointsRoot.name} нет waypoints", this);
            return System.Array.Empty<NPCWaypoint>();
        }

        return found;
    }

    private void OnResidentAdded(Dormitory.Resident resident)
    {
        GameObject instance = Spawn(resident);
        if (instance != null) FocusCamera(instance.transform);
    }

    private GameObject Spawn(Dormitory.Resident resident)
    {
        if (resident == null || dormitory == null) return null;

        CharacterData data = dormitory.GetCharacterData(resident.CharacterIndex);
        if (data == null || data.prefab3D == null) return null;

        GetSpawnPose(out Vector3 position, out Quaternion rotation);

        GameObject instance = Pooling.Instantiate(data.prefab3D, position, rotation);
        if (instance == null) return null;

        ApplyVariant(instance, data.prefab3D, data.GetVariant(resident.VariantIndex));

        var brain = instance.GetComponentInChildren<NPCBrain>(true);
        if (brain != null) brain.SetWaypoints(_waypoints);

        return instance;
    }

    private void GetSpawnPose(out Vector3 position, out Quaternion rotation)
    {
        if (spawnPoint != null)
        {
            position = spawnPoint.position;
            rotation = spawnPoint.rotation;
            return;
        }

        if (_waypoints.Length > 0)
        {
            Transform waypoint = _waypoints[Random.Range(0, _waypoints.Length)].transform;
            position = waypoint.position;
            rotation = waypoint.rotation;
            return;
        }

        position = transform.position;
        rotation = transform.rotation;
    }

    private void ApplyVariant(GameObject instance, GameObject prefab, CharacterData.ColorVariant variant)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        var originals = prefab != null ? prefab.GetComponentsInChildren<Renderer>(true) : null;
        bool paired = originals != null && originals.Length == renderers.Length;

        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer renderer = renderers[r];
            if (renderer == null) continue;

            Material[] materials = paired && originals[r] != null
                ? originals[r].sharedMaterials
                : renderer.sharedMaterials;

            if (variant != null && variant.material != null)
            {
                for (int i = 0; i < materials.Length; i++) materials[i] = variant.material;
                renderer.sharedMaterials = materials;
                renderer.SetPropertyBlock(null);
                continue;
            }

            renderer.sharedMaterials = materials;

            renderer.GetPropertyBlock(_block);
            _block.SetColor(_colorId, variant != null ? variant.tint : Color.white);
            renderer.SetPropertyBlock(_block);
        }
    }

    private void FocusCamera(Transform target)
    {
        if (cameraController == null || target == null) return;

        if (_focusRoutine != null) StopCoroutine(_focusRoutine);
        _focusRoutine = StartCoroutine(FocusRoutine(target));
    }

    private IEnumerator FocusRoutine(Transform target)
    {
        cameraController.FocusOn(target, focusDuration);
        yield return new WaitForSeconds(focusDuration + focusHold);
        cameraController.ReleaseFocus();
        _focusRoutine = null;
    }
}

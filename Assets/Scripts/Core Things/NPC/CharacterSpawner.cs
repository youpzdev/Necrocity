using System.Collections;
using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Dormitory dormitory;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private CameraController cameraController;

    [Header("Camera Focus")]
    [SerializeField] private float focusDuration = 0.6f;
    [SerializeField] private float focusHold = 2f;

    [Header("Coloring")]
    [SerializeField] private string colorProperty = "_BaseColor";

    private MaterialPropertyBlock _block;
    private int _colorId;
    private Coroutine _focusRoutine;

    private void Awake()
    {
        _block = new MaterialPropertyBlock();
        _colorId = Shader.PropertyToID(colorProperty);
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

        Transform anchor = spawnPoint != null ? spawnPoint : transform;
        GameObject instance = Pooling.Instantiate(data.prefab3D, anchor.position, anchor.rotation);
        ApplyVariant(instance, data.GetVariant(resident.VariantIndex));

        return instance;
    }

    private void ApplyVariant(GameObject instance, CharacterData.ColorVariant variant)
    {
        if (variant == null) return;

        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (variant.material != null)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = variant.material;
                renderer.sharedMaterials = materials;
                continue;
            }

            renderer.GetPropertyBlock(_block);
            _block.SetColor(_colorId, variant.tint);
            renderer.SetPropertyBlock(_block);
        }
    }

    private void FocusCamera(Transform target)
    {
        if (cameraController == null) return;

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

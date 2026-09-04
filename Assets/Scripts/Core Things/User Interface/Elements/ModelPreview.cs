using UnityEngine;
using UnityEngine.UI;

public class ModelPreview : MonoBehaviour
{
    [Header("Stage")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Transform modelHolder;

    [Header("Output")]
    [SerializeField] private RawImage targetImage;
    [SerializeField] private Vector2Int textureSize = new Vector2Int(512, 512);
    [SerializeField] private GameObject emptyState;

    [Header("Framing")]
    [SerializeField] private float frameSize = 1f;
    [SerializeField] private Vector3 modelOffset = Vector3.zero;
    [SerializeField] private float startAngle = 0f;

    [Header("Rotation")]
    [SerializeField] private bool rotate = true;
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private bool useUnscaledTime = true;

    private RenderTexture _texture;
    private GameObject _instance;
    private GameObject _prefab;

    private void Awake()
    {
        EnsureTexture();
        if (_instance == null) Clear();
    }

    private void OnDestroy()
    {
        DestroyInstance();
        ReleaseTexture();
    }

    private void Update()
    {
        if (!rotate || _instance == null || modelHolder == null) return;

        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        modelHolder.Rotate(0f, rotationSpeed * delta, 0f, Space.Self);
    }

    public void Show(GameObject prefab) => Show(prefab, 0f);

    public void Show(GameObject prefab, float angleOffset)
    {
        if (prefab == null)
        {
            Clear();
            return;
        }

        if (modelHolder == null)
        {
            Debug.LogError("ModelPreview has no model holder", this);
            return;
        }

        if (prefab == _prefab && _instance != null) return;

        DestroyInstance();
        EnsureTexture();

        _prefab = prefab;
        _instance = Spawn(prefab);

        modelHolder.localRotation = Quaternion.Euler(0f, startAngle + angleOffset, 0f);
        Fit(_instance.transform);

        if (previewCamera != null) previewCamera.enabled = true;
        if (targetImage != null) targetImage.enabled = true;
        if (emptyState != null) emptyState.SetActive(false);
    }

    public void Clear()
    {
        DestroyInstance();
        ClearTexture();

        if (previewCamera != null) previewCamera.enabled = false;
        if (targetImage != null) targetImage.enabled = false;
        if (emptyState != null) emptyState.SetActive(true);
    }

    private GameObject Spawn(GameObject prefab)
    {
        bool holderWasActive = modelHolder.gameObject.activeSelf;
        modelHolder.gameObject.SetActive(false);

        GameObject instance = Instantiate(prefab, modelHolder);
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;
        Prepare(instance);

        modelHolder.gameObject.SetActive(holderWasActive);

        foreach (SkinnedMeshRenderer skinned in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            skinned.updateWhenOffscreen = true;

        return instance;
    }

    private void Prepare(GameObject instance)
    {
        int layer = modelHolder.gameObject.layer;
        foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;

        foreach (Behaviour behaviour in instance.GetComponentsInChildren<Behaviour>(true))
        {
            if (behaviour is Animator animator)
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (useUnscaledTime) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                continue;
            }

            behaviour.enabled = false;
        }

        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
            body.isKinematic = true;
    }

    private void Fit(Transform model)
    {
        if (!TryGetBounds(model, out Bounds bounds))
        {
            model.localScale = Vector3.one;
            model.localPosition = modelOffset;
            return;
        }

        Vector3 size = bounds.size;
        float width = Mathf.Sqrt(size.x * size.x + size.z * size.z);
        float extent = Mathf.Max(width, size.y);
        float scale = extent > 0.0001f ? frameSize / extent : 1f;

        model.localScale = Vector3.one * scale;
        model.localPosition = modelOffset - bounds.center * scale;
    }

    private bool TryGetBounds(Transform model, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;
        Matrix4x4 toModel = model.worldToLocalMatrix;

        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer)) continue;

            Bounds world = renderer.bounds;
            Vector3 center = world.center;
            Vector3 extents = world.extents;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = center + new Vector3(
                    (i & 1) == 0 ? -extents.x : extents.x,
                    (i & 2) == 0 ? -extents.y : extents.y,
                    (i & 4) == 0 ? -extents.z : extents.z);

                Vector3 point = toModel.MultiplyPoint3x4(corner);

                if (!found)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    found = true;
                }
                else bounds.Encapsulate(point);
            }
        }

        return found;
    }

    private void EnsureTexture()
    {
        int width = Mathf.Max(16, textureSize.x);
        int height = Mathf.Max(16, textureSize.y);

        if (_texture != null && _texture.width == width && _texture.height == height) return;

        ReleaseTexture();

        _texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        _texture.name = $"{name} Preview";
        _texture.Create();

        if (previewCamera != null) previewCamera.targetTexture = _texture;
        if (targetImage != null) targetImage.texture = _texture;
    }

    private void ReleaseTexture()
    {
        if (_texture == null) return;

        if (previewCamera != null) previewCamera.targetTexture = null;
        if (targetImage != null) targetImage.texture = null;

        _texture.Release();
        Destroy(_texture);
        _texture = null;
    }

    private void ClearTexture()
    {
        if (_texture == null) return;

        Color color = previewCamera != null ? previewCamera.backgroundColor : Color.clear;
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = _texture;
        GL.Clear(true, true, color);
        RenderTexture.active = active;
    }

    private void DestroyInstance()
    {
        if (_instance != null) Destroy(_instance);

        _instance = null;
        _prefab = null;
    }
}

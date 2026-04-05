using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
public class ResponsiveGridLayout : MonoBehaviour
{
    [Header("Колонки и строки")]
    [SerializeField] private int columns = 3;
    [SerializeField] private int rows = 0;

    [Header("Соотношение сторон ячейки")]
    [SerializeField] private float cellAspectRatio = 1f;

    [Header("Отступы")]
    [SerializeField] private bool respectPadding = true;
    [SerializeField] private bool updateOnResize = true;

    private GridLayoutGroup _grid;
    private RectTransform _rect;
    private Vector2 _lastSize;

    private void Awake()
    {
        _grid = GetComponent<GridLayoutGroup>();
        _rect = GetComponent<RectTransform>();
    }

    private void Start()
    {
        UpdateCellSize();
        _lastSize = _rect.rect.size;
    }

    private void LateUpdate()
    {
        if (Application.isPlaying && !updateOnResize) return;

        Vector2 currentSize = _rect.rect.size;
        if (currentSize != _lastSize)
        {
            _lastSize = currentSize;
            UpdateCellSize();
        }

        if (!Application.isPlaying)
            UpdateCellSize();
    }

    [ContextMenu("Update Cell Size Now")]
    public void UpdateCellSize()
    {
        if (_grid == null) _grid = GetComponent<GridLayoutGroup>();
        if (_rect == null) _rect = GetComponent<RectTransform>();

        float containerWidth = _rect.rect.width;
        float containerHeight = _rect.rect.height;

        if (containerWidth <= 0 || containerHeight <= 0) return;

        float paddingH = respectPadding ? _grid.padding.left + _grid.padding.right : 0;
        float paddingV = respectPadding ? _grid.padding.top + _grid.padding.bottom : 0;

        float usableWidth = containerWidth - paddingH;
        float usableHeight = containerHeight - paddingV;

        float cellWidth, cellHeight;

        if (columns > 0)
        {
            float totalSpacingX = _grid.spacing.x * (columns - 1);
            cellWidth = (usableWidth - totalSpacingX) / columns;
            cellHeight = cellWidth / cellAspectRatio;
        }
        else if (rows > 0)
        {
            float totalSpacingY = _grid.spacing.y * (rows - 1);
            cellHeight = (usableHeight - totalSpacingY) / rows;
            cellWidth = cellHeight * cellAspectRatio;
        }
        else
        {
            Debug.LogWarning("[ResponsiveGridLayout] Укажи columns или rows больше 0.");
            return;
        }

        _grid.cellSize = new Vector2(Mathf.Max(cellWidth, 1f), Mathf.Max(cellHeight, 1f));
    }

    private void OnValidate()
    {
        if (_grid == null) _grid = GetComponent<GridLayoutGroup>();
        if (_rect == null) _rect = GetComponent<RectTransform>();
        if (_grid != null && _rect != null && _rect.rect.width > 0)
            UpdateCellSize();
    }
}
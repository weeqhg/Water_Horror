using UnityEngine;
using UnityEngine.UI;

public class GridLayoutAutoSize : MonoBehaviour
{
    [SerializeField] private float minCellWidth = 80f;
    [SerializeField] private float maxCellWidth = 300f;

    private GridLayoutGroup gridLayout;
    private RectTransform rectTransform;

    void Start()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
        UpdateCellSize();
    }

    void Update()
    {
        // Автоматическое обновление при изменении количества детей
        if (transform.childCount != lastChildCount)
        {
            UpdateCellSize();
            lastChildCount = transform.childCount;
        }
    }

    private int lastChildCount = 0;

    private void UpdateCellSize()
    {
        if (gridLayout == null) return;

        int childCount = transform.childCount;
        if (childCount == 0) return;

        // Рассчитываем ширину контейнера
        float containerWidth = rectTransform.rect.width;
        float spacing = gridLayout.spacing.x;

        // Вычисляем оптимальную ширину ячейки
        float totalSpacing = spacing * (childCount - 1);
        float availableWidth = containerWidth - totalSpacing;
        float cellWidth = Mathf.Clamp(availableWidth / childCount, minCellWidth, maxCellWidth);

        // Применяем новую ширину
        gridLayout.cellSize = new Vector2(cellWidth, gridLayout.cellSize.y);

        // Принудительное обновление layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    [ContextMenu("Обновить UI")]
    public void RefreshLayout()
    {
        UpdateCellSize();
    }
}
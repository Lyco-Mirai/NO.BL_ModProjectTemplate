using UnityEngine;
using UnityEngine.UI;

public class FlexibleGridLayout : LayoutGroup
{
	public enum FitType
	{
		UNIFORM = 0,
		WIDTH = 1,
		HEIGHT = 2,
		FIXEDROWS = 3,
		FIXEDCOLUMNS = 4
	}

	[Header("Flexible Grid")]
	public FitType fitType;

	public int rows;

	public int columns;

	public Vector2 cellSize;

	public Vector2 spacing;

	public bool fitX;

	public bool fitY;

	public override void CalculateLayoutInputHorizontal()
	{
		base.CalculateLayoutInputHorizontal();
		if (fitType == FitType.WIDTH || fitType == FitType.HEIGHT || fitType == FitType.UNIFORM)
		{
			float f = Mathf.Sqrt(base.transform.childCount);
			rows = (columns = Mathf.CeilToInt(f));
			switch (fitType)
			{
			case FitType.WIDTH:
				fitX = true;
				fitY = false;
				break;
			case FitType.HEIGHT:
				fitX = false;
				fitY = true;
				break;
			case FitType.UNIFORM:
				fitX = (fitY = true);
				break;
			}
		}
		if (fitType == FitType.WIDTH || fitType == FitType.FIXEDCOLUMNS)
		{
			rows = Mathf.CeilToInt((float)base.transform.childCount / (float)columns);
		}
		if (fitType == FitType.HEIGHT || fitType == FitType.FIXEDROWS)
		{
			columns = Mathf.CeilToInt((float)base.transform.childCount / (float)rows);
		}
		float width = base.rectTransform.rect.width;
		float height = base.rectTransform.rect.height;
		float num = width / (float)columns - spacing.x / (float)columns * (float)(columns - 1) - (float)base.padding.left / (float)columns - (float)base.padding.right / (float)columns;
		float num2 = height / (float)rows - spacing.y / (float)rows * (float)(rows - 1) - (float)base.padding.top / (float)rows - (float)base.padding.bottom / (float)rows;
		cellSize.x = (fitX ? num : cellSize.x);
		cellSize.y = (fitY ? num2 : cellSize.y);
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < base.rectChildren.Count; i++)
		{
			num4 = i / columns;
			num3 = i % columns;
			RectTransform rect = base.rectChildren[i];
			float pos = cellSize.x * (float)num3 + spacing.x * (float)num3 + (float)base.padding.left;
			float pos2 = cellSize.y * (float)num4 + spacing.y * (float)num4 + (float)base.padding.top;
			SetChildAlongAxis(rect, 0, pos, cellSize.x);
			SetChildAlongAxis(rect, 1, pos2, cellSize.y);
		}
	}

	public override void CalculateLayoutInputVertical()
	{
	}

	public override void SetLayoutHorizontal()
	{
	}

	public override void SetLayoutVertical()
	{
	}
}

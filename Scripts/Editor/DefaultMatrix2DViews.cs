#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utility.SerializableCollection.Editor
{
	public class Matrix2DDefaultView : Matrix2DView<object>
	{
		public override string ViewName => "Default";
	}

	public class Matrix2DInCellEditView : Matrix2DView<object>
	{
		public override string ViewName => "In Cell Edit";

		protected override void DrawCell(Rect position, int x, int y, object element)
		{
			SerializedProperty property = GetCellProperty(x, y);
			EditorGUI.PropertyField(position, property, GUIContent.none);
		}
	}

	public class Matrix2DBoolView : Matrix2DView<bool>
	{
		public override string ViewName => "Bool Default";

		protected override string Text(int x, int y, bool element) => element ? "✓" : string.Empty;
	}

	public class Matrix2DBoolInCellEditView : Matrix2DView<bool>
	{
		public override string ViewName => "In Cell Edit";

		protected override void DrawCell(Rect position, int x, int y, bool element)
		{
			const float checkBoxWidth = 15;

			position.x += ((position.width - checkBoxWidth) / 2);
			position.width = checkBoxWidth;
			SerializedProperty property = GetCellProperty(x, y);
			EditorGUI.PropertyField(position, property, GUIContent.none);
		}
	}

	public static class HeatMapHelper
	{
		static readonly Color minColor = new Color(0.94f, 0.86f, 0.58f);
		static readonly Color middleColor = new Color(0.92f, 0.66f, 0.4f);
		static readonly Color maxColor = new Color(0.79f, 0.34f, 0.31f);

		public static Color GetColor(float value, float min, float max) =>
			GetColor(value, min, max, minColor, middleColor, maxColor);

		public static Color GetColor(float value, float min, float max, Color minColor, Color middleColor, Color maxColor)
		{
			float rate = (value - min) / (max - min);
			if (rate < 0.5f)
				return Color.Lerp(minColor, middleColor, rate * 2);
			else
				return Color.Lerp(middleColor, maxColor, (rate - 0.5f) * 2);
		}

		public static (T min, T max) GetMinMax<T>(IEnumerable matrix) where T : IComparable
		{
			bool isFirst = true;
			T min = default;
			T max = default;
			foreach (T n in matrix)
			{
				if (isFirst)
				{
					min = n;
					max = n;
					isFirst = false;
					continue;
				}

				if (n.CompareTo(min) == -1)
					min = n;
				if (n.CompareTo(max) == 1)
					max = n;
			}
			return (min, max);
		}
	}

	public class Matrix2DIntHeatmapView : Matrix2DView<int>
	{
		public override string ViewName => "Heatmap";

		protected int min, max;

		public override void InitializationsBeforeDrawing() =>
			(min, max) = HeatMapHelper.GetMinMax<int>(targetMatrix2D);


		protected override Color? CellColor(int x, int y, int element) =>
			HeatMapHelper.GetColor(element, min, max);

		protected override Color? TextColor(int x, int y, int element) => Color.black;
	}

	public class Matrix2DFloatHeatmapView : Matrix2DView<float>
	{
		public override string ViewName => "Heatmap";

		float _min, _max;

		public override void InitializationsBeforeDrawing() =>
			(_min, _max) = HeatMapHelper.GetMinMax<float>(targetMatrix2D);


		protected override Color? CellColor(int x, int y, float element) =>
			HeatMapHelper.GetColor(element, _min, _max);

		protected override Color? TextColor(int x, int y, float element) => Color.black;
	}


	public class Matrix2DObjectView : Matrix2DView<Object>
	{
		public override string ViewName => "Object Default";

		protected override string Text(int x, int y, Object element) =>
			element == null ? string.Empty : element.name;
	}

	public class Matrix2DTextureView : Matrix2DView<Texture>
	{
		Material _spriteMaterial;

		Material SpriteMaterial
		{
			get
			{
				if (_spriteMaterial == null)
					_spriteMaterial = new Material(Shader.Find("Sprites/Default"));
				return _spriteMaterial;
			}
		}


		const int cellSize = 50;
		public override string ViewName => "Texture Default";

		protected override void DrawCell(Rect position, int x, int y, Texture texture)
		{
			if (texture == null)
				return;

			var size = new Vector2(texture.width, texture.height);
			EditorGUI.DrawPreviewTexture(Crop(position, size), texture, SpriteMaterial);
		}

		Rect Crop(Rect original, Vector2 format)
		{
			float originalRation = original.width / original.height;
			float outputRatio = format.x / format.y;
			float ratioDifference = originalRation / outputRatio;
			Vector2 size = original.size;
			if (ratioDifference > 1)
				size.y /= ratioDifference;
			else
				size.x *= ratioDifference;
			return new Rect(original.center - (size / 2), size);
		}

		public override int? ForceCellHeight => cellSize;
		public override int? ForceCellWidth => cellSize;
	}
}
#endif
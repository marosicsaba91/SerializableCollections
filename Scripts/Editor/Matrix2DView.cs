#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Utility.SerializableCollection.Editor
{

	public abstract class Matrix2DViewBase : ICollectionView
	{
		public abstract void Init(Matrix2DDrawer drawer);
		public abstract void DrawCell(Rect position, int x, int y);
		public virtual string HorizontalHeaderText(int xIndex) => xIndex.ToString();
		public virtual string VerticalHeaderText(int yIndex) => yIndex.ToString();

		public virtual int? ForceCellWidth => null;
		public virtual int? ForceCellHeight => null;

		public virtual bool ShowHeaders => true;

		public abstract string ViewName { get; }

		public virtual void InitializationsBeforeDrawing() { }

	}

	public abstract class Matrix2DView<TContainingType> : Matrix2DViewBase
	{
		protected Matrix2DDrawer drawer;
		protected Matrix2DBase targetMatrix2D;
		protected SerializedProperty matrixProperty;
		protected SerializedObject targetSerializedObject;
		protected SerializedProperty fieldsProperty;

		public sealed override void Init(Matrix2DDrawer drawer)
		{
			this.drawer = drawer;
			targetMatrix2D = drawer.GetMatrix2D;
			matrixProperty = drawer.MatrixProperty;
			targetSerializedObject = drawer.SerializedUnityObject;
			fieldsProperty = drawer.FieldsProperty;
		}

		protected readonly GUIStyle centerAlignment = new GUIStyle(GUI.skin.label)
		{
			alignment = TextAnchor.MiddleCenter
		};

		public sealed override void DrawCell(Rect position, int x, int y)
		{
			var element = (TContainingType)targetMatrix2D.GetElement(x, y);
			DrawCell(position, x, y, element);
		}

		protected virtual void DrawCell(Rect position, int x, int y, TContainingType element)
		{
			Color? color = CellColor(x, y, element);
			if (color != null)
				EditorGUI.DrawRect(position, color.Value);

			// Label
			string label = Text(x, y, element);
			if (string.IsNullOrEmpty(label))
				return;

			Color? textColor = TextColor(x, y, element);
			if (textColor != null)
			{
				centerAlignment.normal.textColor = textColor.Value;
				GUI.Label(position, label, centerAlignment);
				centerAlignment.normal.textColor = Color.black;
			}
			else
				GUI.Label(position, label, centerAlignment);
		}

		protected virtual string Text(int x, int y, TContainingType element) => element?.ToString();

		protected virtual Color? CellColor(int x, int y, TContainingType element) => null;

		protected virtual Color? TextColor(int x, int y, TContainingType element) => null;

		protected SerializedProperty GetCellProperty(int x, int y) => drawer.GetCellProperty(new Vector2Int(x, y));
	}
}
#endif
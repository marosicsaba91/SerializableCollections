﻿#if UNITY_EDITOR
using System.Collections.Generic;
using EasyEditor;
using MUtility;
using UnityEditor;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Utility.SerializableCollection.Editor
{
	[CustomPropertyDrawer(typeof(Matrix3DBase), true)]
	public class Matrix3DDrawer : MatrixDrawer<
		Matrix3DBase,
		BoundsInt,
		Vector3Int,
		Matrix2DViewBase>
	{
		const float sizePanelWidth = 210;
		const int matrixPanelSize = 100;

		int _matrixWidth;
		int _matrixHeight;
		int _matrixDepth;

		SerializedProperty GetFieldProperty(int x, int y, int z) =>
			fieldsProperty.GetArrayElementAtIndex(x + (y * _matrixWidth) + (z * _matrixWidth * _matrixHeight));

		SerializedProperty PropertyToDraw => SelectedOneCell
			? GetFieldProperty(selectedArea.x, selectedArea.y, selectedArea.z)
			: tempElementProperty;

		float TemplateHeight => EditorGUI.GetPropertyHeight(PropertyToDraw);

		protected override IEnumerable<SerializedProperty> GetEnumeratorToArea(BoundsInt area)
		{
			for (int x = area.xMin; x < area.xMax; x++)
				for (int y = area.yMin; y < area.yMax; y++)
					for (int z = area.zMin; z < area.zMax; z++)
						yield return GetFieldProperty(x, y, z);
		}

		protected override BoundsInt FullArea => new(
			0, 0, 0,
			_matrixWidth, _matrixHeight, _matrixDepth);

		protected override void UpdateState_BeforeGetPropertyHeight()
		{
			if (fieldsProperty == null)
				return;
			_matrixWidth = collectionProperty.FindPropertyRelative("width").intValue;
			_matrixHeight = collectionProperty.FindPropertyRelative("height").intValue;
			_matrixDepth = fieldsProperty.arraySize / (_matrixWidth * _matrixHeight);
		}

		protected override void SelectAll()
		{
			BoundsInt all = new(0, 0, 0, _matrixWidth, _matrixHeight, _matrixDepth);
			selectedArea = selectedArea.Equals(all) ? new BoundsInt(0, 0, 0, 0, 0, 0) : all;
		}

		protected override void PasteCopiedArea()
		{
			if (!CopiedAny || !SelectedAny)
				return;

			bool selectedOneCell = selectedArea.size == Vector3Int.one;

			BoundsInt area = selectedOneCell
				? new BoundsInt(selectedArea.position, copiedArea.size)
				: selectedArea;

			CopyArea(copiedArea, area);

			selectedArea = new BoundsInt(
				area.x,
				area.y,
				area.z,
				Mathf.Min(area.size.x, _matrixWidth - area.x),
				Mathf.Min(area.size.y, _matrixHeight - area.y),
				Mathf.Min(area.size.z, _matrixHeight - area.y)
			);
		}

		void CopyArea(BoundsInt source, BoundsInt destination)
		{
			for (int x = 0; x < destination.size.x; x++)
				for (int y = 0; y < destination.size.y; y++)
					for (int z = 0; z < destination.size.y; z++)
					{
						Vector3Int sourceCoordinate = new(x, y, z);
						sourceCoordinate = MathHelper.ModuloPositive(sourceCoordinate, source.size.x, source.size.y, source.size.z);
						sourceCoordinate.x += source.x;
						sourceCoordinate.y += source.y;
						sourceCoordinate.z += source.z;

						Vector3Int destinationCoordinate = new(
							destination.position.x + x,
							destination.position.y + y,
							destination.position.y + z);

						if (destinationCoordinate.x < 0 ||
							destinationCoordinate.x >= _matrixWidth ||
							destinationCoordinate.y < 0 ||
							destinationCoordinate.y >= _matrixHeight ||
							destinationCoordinate.z < 0 ||
							destinationCoordinate.z >= _matrixDepth)
							continue;

						SerializedProperty sourceProp =
							GetCellProperty(new Vector3Int(sourceCoordinate.x, sourceCoordinate.y, sourceCoordinate.z));
						SerializedProperty destinationProp =
							GetCellProperty(new Vector3Int(destinationCoordinate.x, destinationCoordinate.y, destinationCoordinate.z));

						sourceProp.CopyPropertyValueTo(destinationProp);
					}
		}


		protected sealed override float AdditionalHeaderWidth => 226;

		protected override void DrawCollection()
		{
			selectedArea = EditorGUI.BoundsIntField(collectionRect, selectedArea);
			selectedArea.min = Vector3Int.Max(selectedArea.min, Vector3Int.zero);
			selectedArea.min = Vector3Int.Min(
				selectedArea.min,
				new Vector3Int(_matrixWidth - 1, _matrixHeight - 1, _matrixDepth - 1));
			selectedArea.max = Vector3Int.Min(selectedArea.max, MatrixSize);

			//Debug.Log($"{selectedArea.min}  {selectedArea.max}  {selectedArea.size}");
		}

		protected override void UpdateState_BeforeDrawingCollection(Rect contentRect)
		{
			collectionRect = new Rect(
				contentRect.x,
				contentRect.y,
				contentRect.width,
				matrixPanelSize);
		}

		protected sealed override Vector3Int MatrixSize
		{
			get => targetObject.Size;
			set => targetObject.Size = value;
		}

		protected sealed override Vector3Int DrawIndexVectorUI(Rect position, Vector3Int size) =>
			EditorGUI.Vector3IntField(position, GUIContent.none, size);

		public override SerializedProperty GetCellProperty(Vector3Int index) =>
			fieldsProperty.GetArrayElementAtIndex(
				index.x + (index.y * _matrixWidth) + (index.z * _matrixWidth * _matrixHeight));

		// Properties
		protected override bool SelectedAny => selectedArea.size != Vector3.zero;

		protected override bool CopiedAny => copiedArea.size != Vector3.zero;
		protected override Vector3Int FirstSelectedIndex => selectedArea.min;
		protected override Vector3Int MinimumIndex => Vector3Int.zero;
		protected override string SelectedIndexText => "TODO";
		protected override float CollectionHeight => matrixPanelSize;

		bool CopiedOneCell => copiedArea.size == Vector3.one;
		bool SelectedOneCell => selectedArea.size == Vector3.one;
	}
}
#endif
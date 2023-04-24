#if UNITY_EDITOR
using MUtility;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Utility.SerializableCollection.Editor
{
	public abstract class CollectionDrawerBase : PropertyDrawer
	{
		protected const int selectionBorderWidth = 2;
		protected const int indentWidth = 15;
		protected const int extraSpacingWhenOpen = 5;
		protected const float headerHeight = 20;

		protected static readonly float singleLineHeight = EditorGUIUtility.singleLineHeight;
		protected static readonly Color insertColor = Color.white;
		protected static readonly Color selectionColor = Color.white;
		protected static readonly Color copyColor = Color.black;

		protected static readonly GUIStyle foldoutButtonStyle = new GUIStyle();

		protected static readonly GUIStyle centerAlignment = new GUIStyle(GUI.skin.label)
		{
			alignment = TextAnchor.MiddleCenter
		};

		protected static readonly GUIStyle rightAlignment = new GUIStyle(GUI.skin.label)
		{
			alignment = TextAnchor.MiddleRight
		};

		protected static readonly GUIStyle errorTextStyle = new GUIStyle(GUI.skin.label)
		{
			fontSize = 11,
			alignment = TextAnchor.MiddleRight,
			normal = new GUIStyleState { textColor = EditorHelper.ErrorRedColor }
		};

		protected static readonly GUIStyle infoTextStyle = new GUIStyle(GUI.skin.label)
		{
			fontSize = 11,
			alignment = TextAnchor.MiddleRight,
		};

		static MethodInfo _methodeInfo;
		protected static void RepaintAllInspectors()
		{
			// NOT SEAMS TO WORK
			if (_methodeInfo == null)
			{
				Type inspectorWindowType = typeof(EditorApplication).Assembly.GetType("UnityEditor.InspectorWindow");
				_methodeInfo = inspectorWindowType.GetMethod(
					"RepaintAllInspectors",
					BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			_methodeInfo?.Invoke(obj: null, parameters: null);
		}

		protected static void DrawSelection(Rect rect, Color color, bool inside)
		{
			if (rect.width == 0 || rect.height == 0)
				return;

			Rect rectL = inside
				? new Rect(rect.x, rect.y, selectionBorderWidth, rect.height)
				: new Rect(rect.x - selectionBorderWidth, rect.y - selectionBorderWidth, selectionBorderWidth,
					rect.height + (2 * selectionBorderWidth));
			EditorGUI.DrawRect(rectL, color);

			Rect rectR = inside
				? new Rect(rect.x + rect.width - selectionBorderWidth, rect.y, selectionBorderWidth, rect.height)
				: new Rect(rect.x + rect.width, rect.y, selectionBorderWidth, rect.height + selectionBorderWidth);
			EditorGUI.DrawRect(rectR, color);

			Rect rectT = inside
				? new Rect(rect.x, rect.y, rect.width, selectionBorderWidth)
				: new Rect(rect.x - selectionBorderWidth, rect.y - selectionBorderWidth,
					rect.width + (2 * selectionBorderWidth), selectionBorderWidth);
			EditorGUI.DrawRect(rectT, color);

			Rect rectB = inside
				? new Rect(rect.x, rect.y + rect.height - selectionBorderWidth, rect.width, selectionBorderWidth)
				: new Rect(rect.x, rect.y + rect.height, rect.width + selectionBorderWidth, selectionBorderWidth);
			EditorGUI.DrawRect(rectB, color);
		}

	}

	public abstract class CollectionDrawer<TCollectionType> :
		CollectionDrawerBase where TCollectionType : IGenericCollection
	{
		protected TCollectionType targetObject;
		protected SerializedProperty collectionProperty;
		protected SerializedProperty tempElementProperty;
		protected SerializedObject serializedUnityObject;

		Rect _fullRect;
		Rect _headerRect;
		Rect _foldoutRect;
		Rect _additionalHeaderRect;
		Rect _contentRect;

		protected float templateHeightFull;
		protected bool isTypeFolding;
		float _contentHeight;

		protected void UpdateState_References(SerializedProperty property)
		{
			collectionProperty = property;
			serializedUnityObject = property.serializedObject;
			targetObject = (TCollectionType)property.GetObjectOfProperty();
			UpdateReferences(property);
		}

		protected abstract void UpdateReferences(SerializedProperty property);

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			UpdateState_BeforeDrawing(position, property);

			if (!TryDrawHeader(_headerRect, property, label))
				return;

			if (IsExpanded)
				DrawContent(_contentRect);
		}

		bool TryDrawHeader(Rect headerRect, SerializedProperty property, GUIContent label)
		{
			EditorHelper.DrawBox(headerRect, borderInside: false);

			if (IsExpandable)
			{
				property.isExpanded = EditorGUI.Foldout(_foldoutRect, property.isExpanded, label);
				if (GUI.Button(_foldoutRect, string.Empty, foldoutButtonStyle))
					property.isExpanded = !property.isExpanded;
			}
			else
				EditorGUI.LabelField(_foldoutRect, label);

			DrawAdditionalHeader(_additionalHeaderRect);
			return true;
		}

		protected abstract void DrawAdditionalHeader(Rect additionalHeaderRect);

		protected abstract void DrawContent(Rect contentRect);

		void UpdateState_BeforeDrawing(Rect position, SerializedProperty property)
		{
			_fullRect = new Rect(
				position.x + 1,
				position.y + 1,
				position.width - 2,
				position.height - 1);

			_headerRect = new Rect(
				_fullRect.x,
				_fullRect.y,
				_fullRect.width,
				headerHeight);

			_contentRect = new Rect(_fullRect.x, _fullRect.y + headerHeight + 1, _fullRect.width, _contentHeight);

			_foldoutRect = new Rect(
				_headerRect.x + indentWidth,
				_headerRect.y,
				_headerRect.width - AdditionalHeaderWidth - indentWidth,
				headerHeight);

			_additionalHeaderRect = new Rect(
				_headerRect.x + _headerRect.width - AdditionalHeaderWidth + 1,
				_headerRect.y - 1,
				AdditionalHeaderWidth,
				headerHeight + 2);
		}

		public sealed override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			UpdateState_References(property);

			const float headerH = headerHeight + 2;
			if (!IsExpanded)
				return headerH;

			UpdateState_References(property);

			_contentHeight = GetContentHeight(property);

			return _contentHeight + headerH;
		}

		protected abstract float GetContentHeight(SerializedProperty property);

		protected abstract float AdditionalHeaderWidth { get; }


		protected void DrawSerializationErrorMessage()
		{
			EditorGUI.LabelField(
				_headerRect,
				$"Can't Serialize:   {targetObject.ContainingType.Name} ",
				errorTextStyle);
		}

		protected void DrawMultipleSelectMessage()
		{
			EditorGUI.LabelField(_headerRect, "Multiple Object Is Selected ", infoTextStyle);
		}

		protected abstract bool IsExpandable { get; }
		bool IsExpanded => IsExpandable && collectionProperty.isExpanded;

		protected bool IsMultipleObjectSelected => serializedUnityObject.isEditingMultipleObjects;



	}
}
#endif
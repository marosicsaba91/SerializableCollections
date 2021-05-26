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
        protected const int headerHeight = 20;
        protected const int indentWidth = 15; 
        protected const int extraSpacingWhenOpen = 5; 
        
        protected static readonly float singleLineHeight = EditorGUIUtility.singleLineHeight;
        protected static readonly Color insertColor = Color.white;
        protected static readonly Color selectionColor = Color.white;
        protected static readonly Color copyColor = Color.black; 
        
        protected static readonly GUIStyle foldoutButtonStyle = new GUIStyle();

        protected static readonly GUIStyle centerAlignment = new GUIStyle(GUI.skin.label)
        { 
            alignment = TextAnchor.MiddleCenter
        };
 
        protected static readonly GUIStyle errorTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleRight,
            normal = new GUIStyleState() {textColor = EditorHelper.ErrorRedColor}
        };
        
        protected static readonly GUIStyle infoTextStyle= new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleRight,
        };
        
        static MethodInfo _methodeInfo;
        protected static void RepaintAllInspectors()
        {
            if (_methodeInfo == null)
            {
                Type inspectorWindowType = typeof(EditorApplication).Assembly.GetType("UnityEditor.InspectorWindow");
                _methodeInfo = inspectorWindowType.GetMethod(
                    "RepaintAllInspectors", 
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic );
            }
            _methodeInfo?.Invoke(null, null);
        }
    }

    public abstract class CollectionDrawer<TCollectionType> : 
        CollectionDrawerBase where TCollectionType : IGenericCollection
    {
        protected TCollectionType targetObject;
        protected SerializedProperty collectionProperty;
        protected SerializedProperty tempElementProperty;
        protected SerializedProperty fieldsProperty;
        protected SerializedObject serializedUnityObject; 
        
        protected Rect fullRect;
        protected Rect headerRect;
        protected Rect foldoutRect;
        protected Rect collectionRect;
        protected Rect selectedPropertyContentRect;
        protected Rect selectedPropertyRect;
        protected float templateHeightFull;
        protected bool isTypeFolding;

        protected void UpdateState_References(SerializedProperty property)
        {
            collectionProperty = property;
            serializedUnityObject = property.serializedObject;
            targetObject = (TCollectionType) property.GetObjectOfProperty();
            fieldsProperty = collectionProperty.FindPropertyRelative("fields");
            if (fieldsProperty != null && !IsMultipleObjectSelected)
            {
                SerializedProperty firstProperty = FirstProperty;
                SerializedProperty firstSelectedProperty = FirstSelectedProperty;
                templateHeightFull = firstSelectedProperty == null ? 0 : EditorGUI.GetPropertyHeight(firstSelectedProperty);
                isTypeFolding = IsTypeFolding(firstProperty);
            }
        }
         
        protected void DrawSerializationErrorMessage()
        {
            EditorGUI.LabelField(
                headerRect,
                $"Can't Serialize:   {targetObject.ContainingType.Name} ",
                errorTextStyle);
        }
        
        protected void DrawMultipleSelectMessage()
        {
            EditorGUI.LabelField(headerRect, "Multiple Object Is Selected ", infoTextStyle);
        }

        public bool IsExpandable => fieldsProperty != null && !IsMultipleObjectSelected && targetObject.Count > 0;
        public bool IsExpanded => IsExpandable && collectionProperty.isExpanded;
        
        public bool IsMultipleObjectSelected => serializedUnityObject.isEditingMultipleObjects;


        static bool IsTypeFolding(SerializedProperty property)
        {
            if (property == null) return false;
            
            SerializedPropertyType propertyType = property.propertyType;
            return propertyType == SerializedPropertyType.Generic;
        }
        
        protected abstract SerializedProperty FirstProperty { get; }
        protected abstract SerializedProperty FirstSelectedProperty { get; }
    }
}
#endif
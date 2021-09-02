using System.Collections.Generic;
using MUtility;
using UnityEditor;
using UnityEngine;
using Utility.SerializableCollection.Editor; 

namespace Utility.SerializableCollection
{

[CustomPropertyDrawer(typeof(SerializableDictionary), useForChildren: true)]
public class SerializableDictionaryDrawer : CollectionDrawer<SerializableDictionary>  
{ 
    protected SerializedProperty keysProperty;
    protected SerializedProperty valuesProperty;

    int _selectedIndex = -1;
    int _count = 1;
    
    protected override void UpdateReferences(SerializedProperty property)
    { 
        keysProperty = collectionProperty.FindPropertyRelative("keys");
        valuesProperty = collectionProperty.FindPropertyRelative("values");
        _count = targetObject.Count;
    }
    
    protected override float AdditionalHeaderWidth => 180;

    protected override void DrawAdditionalHeader(Rect additionalHeaderRect)
    {  
        const float actionButtonWidth = 25;
        float y = additionalHeaderRect.y+2;
        float height = additionalHeaderRect.height-4;

        var addRect = new Rect(additionalHeaderRect.xMax - actionButtonWidth-2, y, actionButtonWidth, height);
        var deleteRect = new Rect(addRect.x - actionButtonWidth - 1, y, actionButtonWidth, height);
        var moveDownRect = new Rect(deleteRect.x - actionButtonWidth - 1, y, actionButtonWidth, height);
        var moveUpRect = new Rect(moveDownRect.x - actionButtonWidth - 1, y, actionButtonWidth, height);
        var labelRect = new Rect(additionalHeaderRect.x, y, moveUpRect.x - 5 - additionalHeaderRect.x, height);

        bool tempEnable = GUI.enabled;

        if (GUI.Button(addRect, EditorGUIUtility.IconContent("CreateAddNew")))
            AddElement();
        GUI.enabled = _selectedIndex >= 0 && _selectedIndex < _count;
        if (GUI.Button(deleteRect, EditorGUIUtility.IconContent("winbtn_win_close_a")))
            RemoveElement();
        GUI.enabled = _selectedIndex >= 0 && _selectedIndex < _count-1;
        if (GUI.Button(moveDownRect, EditorGUIUtility.IconContent("scrolldown")))
            MoveElement(up: false);
        GUI.enabled = _selectedIndex >= 1 && _selectedIndex < _count; 
        if (GUI.Button(moveUpRect, EditorGUIUtility.IconContent("scrollup")))
            MoveElement(up: true);
        
        
        GUI.enabled = tempEnable;
        EditorGUI.LabelField(labelRect, $"Count: {targetObject.Count}" , rightAlignment);
    }

    void RemoveElement()
    { 
        keysProperty.DeleteArrayElementAtIndex(_selectedIndex);
        valuesProperty.DeleteArrayElementAtIndex(_selectedIndex);
    }

    void AddElement()
    {
        int index = _selectedIndex >= 0 ? _selectedIndex : _count;
        keysProperty.InsertArrayElementAtIndex(index);
        valuesProperty.InsertArrayElementAtIndex(index);
        if(_selectedIndex >= 0)
            _selectedIndex++; 
    }
     
    void MoveElement(bool up)
    {
        int from = _selectedIndex >= 0 ? _selectedIndex : _count;
        int to = up ? from - 1 : from + 1; 
        keysProperty.MoveArrayElement(from, to);
        valuesProperty.MoveArrayElement(from, to);
        _selectedIndex = to;
    }
    
    const float n = 2;

    protected override void DrawContent(Rect contentRect)
    {
        float y = contentRect.y;
        contentRect.height = Mathf.Ceil(contentRect.height - extraSpacingWhenOpen);
        EditorHelper.DrawBox(contentRect, borderInside: false);
        
        const float indexWidth = 35;
        float w = (contentRect.width - indexWidth) / 2f;
        float indexX = contentRect.x;
        float keyX = contentRect.x + indexWidth; 

        float tempLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth *= 0.5f;

        var index = 0; 
        
        foreach (PropertyKeyValuePair element in GetElementProperties())
        {
            float elementHeight = element.height + n;


            Color? color = index%2 == 0 ?EditorHelper.tableEvenLineColor : (Color?) null;
            if (_selectedIndex == index)
            { 
                color = EditorHelper.tableSelectedColor;
            }

            var indexRect = new Rect(indexX, y, indexWidth, elementHeight);
            var keyRect = new Rect(keyX, y, w, elementHeight);
            var valueRect = new Rect(keyRect.xMax, y, contentRect.xMax-keyRect.xMax, elementHeight);
            EditorHelper.DrawBox(indexRect, color,borderColor: null,  borderInside: false);
            EditorHelper.DrawBox(keyRect,color,borderColor: null,  borderInside: false);
            EditorHelper.DrawBox(valueRect, color,borderColor: null, borderInside: false);
 
            if (targetObject.ContainsKeyMoreThanOnce(index))
            {
                var rowRect = new Rect(contentRect.x, y, contentRect.width, elementHeight);
                EditorHelper.DrawBox(rowRect, EditorHelper.ErrorBackgroundColor, borderColor: null,  borderInside: false);
            }
 
            if (GUI.Button(indexRect, index.ToString(), centerAlignment))
                _selectedIndex = index == _selectedIndex ? -1 : index;

            keyRect = Margin(keyRect, margin: 1, element.keyHeight, element.keyProperty.IsExpandable());
            valueRect = Margin(valueRect, margin: 1, element.valueHeight, element.valueProperty.IsExpandable());
            GUIContent keyContent = element.keyProperty.IsExpandable() ? new GUIContent(element.key.ToString()) : GUIContent.none;
            GUIContent valueContent = element.valueProperty.IsExpandable() ? new GUIContent(element.value.ToString()) : GUIContent.none;
            EditorGUI.PropertyField(keyRect, element.keyProperty, keyContent, includeChildren: true);
            EditorGUI.PropertyField(valueRect, element.valueProperty, valueContent, includeChildren: true);

            y += elementHeight;
            index++; 
        }

        EditorGUIUtility.labelWidth = tempLabelWidth;

        Rect Margin(Rect rect, float margin,  float maxHeight, bool isExpandable)
        {
            float height = Mathf.Min(maxHeight, rect.height - (2 * margin));
            float centeredY = rect.center.y - (height / 2);
            
            var result = new Rect(rect.x + margin, centeredY, rect.width - 2 * margin, height);

            if (isExpandable)
            {
                const float foldoutW = 12;
                result.x += foldoutW;
                result.width -= foldoutW;
            }

            return result;
        }
    }

    protected override float GetContentHeight(SerializedProperty property)
    {
        if (!property.isExpanded) return 0;
        
        float fullHeight = 0;

        foreach (PropertyKeyValuePair element in GetElementProperties())
        { 
            fullHeight += element.height;
            fullHeight += n;
        }

        return fullHeight + extraSpacingWhenOpen;
    }

    IEnumerable<PropertyKeyValuePair> GetElementProperties()
    {
        int count = Mathf.Min(keysProperty.arraySize, valuesProperty.arraySize);
        for (var i = 0; i < count; i++)
            yield return GetPropertyAt(i);
    }

    PropertyKeyValuePair GetPropertyAt(int index)
    {
        SerializedProperty keyP = keysProperty.GetArrayElementAtIndex(index);
        SerializedProperty valueP = valuesProperty.GetArrayElementAtIndex(index);
        KeyValuePair<object, object> kvp = targetObject.GetKeyValuePairAt(index);
        float keyHeight = EditorGUI.GetPropertyHeight(keyP, includeChildren: true);
        float valueHeight = EditorGUI.GetPropertyHeight(valueP, includeChildren: true);
        float elementHeight = Mathf.Max(keyHeight, valueHeight);
        return new PropertyKeyValuePair
        {
            keyProperty = keyP, valueProperty = valueP,
            key = kvp.Key, value = kvp.Value,
            keyHeight = keyHeight, valueHeight = valueHeight, height = elementHeight
        };
    }

    struct PropertyKeyValuePair
    {
        public SerializedProperty keyProperty;
        public SerializedProperty valueProperty;
        public object key;
        public object value;
        public float keyHeight;
        public float valueHeight;
        public float height;
    }

    protected override bool IsExpandable => targetObject != null && targetObject.Count > 0;
}
}

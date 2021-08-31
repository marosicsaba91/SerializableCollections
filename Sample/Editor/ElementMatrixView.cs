#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Utility.SerializableCollection.Editor;

public class ElementMatrixView : Matrix2DView<Element>
{
    public override string ViewName => "Element Default";
    protected override Color? CellColor(int x, int y, Element element) => element.ToEditorColor();
    public override int? ForceCellWidth => 40;
    public override int? ForceCellHeight => 25;
    protected override Color? TextColor(int x, int y, Element element) => Color.black;
}

public class ElementMatrixPixelView : Matrix2DView<Element>
{
    public override string ViewName => "Pixel";
    protected override Color? CellColor(int x, int y, Element element) => element.ToEditorColor();

    public override int? ForceCellWidth => 5;
    public override int? ForceCellHeight => 5;
    protected override string Text(int x, int y, Element element) => null;
    public override bool ShowHeaders => false;
}


public class ElementMatrixInCellView : Matrix2DView<Element>
{
    public override string ViewName => "In Cell Edit";

    protected override void DrawCell(Rect position, int x, int y, Element element)
    {
        SerializedProperty property = GetCellProperty(x, y); 
        GUI.color = element.ToEditorColor();
        
        EditorGUI.PropertyField(position, property, GUIContent.none);
        GUI.color = Color.white;
    }   
    public override int? ForceCellWidth => 50;
}

static class ElementExtensions
{
    static readonly Color earthColor = new Color(0.8f, 0.59f, 0.45f);
    static readonly Color windColor = new Color(0.61f, 0.84f, 0.85f);
    static readonly Color fireColor = new Color(0.91f, 0.37f, 0.38f);
    static readonly Color waterColor = new Color(0.48f, 0.65f, 0.87f);

    public static Color ToEditorColor(this Element element)
    {
        switch (element)
        {
            case Element.Earth:
                return earthColor;
            case Element.Air:
                return windColor;
            case Element.Fire:
                return fireColor;
            case Element.Water:
                return waterColor;
            default:
                throw new ArgumentOutOfRangeException(nameof(element), element, null);
        }
    }
}
#endif
#if UNITY_EDITOR
using System; 
using System.Collections.Generic;
using MUtility;
using UnityEditor;
using UnityEngine;

namespace Utility.SerializableCollection.Editor
{
    public abstract class MatrixDrawer
            <TCollection,
            TArea,
            TIndexVector,
            TView> 
        : CollectionDrawer<TCollection> 
            where TCollection : IGenericCollection
            where TView: ICollectionView
    {
        protected bool selecting;
        protected bool aaa;

        protected TArea selectedArea;
        protected TArea copiedArea;
        
        // Context 
        protected enum Context
        {
            Edit,
            Paint,
            Resize
        }

        protected Context context = Context.Edit;
        protected TIndexVector tempSize;
        protected Vector2Int mouseDownCoord;
        
        
        protected TView defaultView;
        protected readonly List<TView> matrix2DViews = new List<TView>();
        protected int selectedViewIndex = 0;

        // Properties
        protected abstract bool SelectedAny { get; }
        protected abstract bool CopiedAny { get; }


        bool _isSelectedExpanded;

        public bool IsSelectedExpanded
        {
            get => _isSelectedExpanded;
            set
            {
                if (_isSelectedExpanded == value) return;
                _isSelectedExpanded = value;
                ApplyToMultipleCells(FullArea, property => property.isExpanded = value);
            }
        }

        protected void UpdateState_BeforeHeader(Rect position)
        {
            fullRect = new Rect(
                position.x + 1,
                position.y + 1,
                position.width - 2,
                position.height - 1);

            headerRect = new Rect(
                fullRect.x,
                fullRect.y,
                fullRect.width,
                headerHeight);

            foldoutRect = new Rect(
                headerRect.x + indentWidth,
                headerRect.y,
                headerRect.width - AdditionalHeaderWidth - indentWidth,
                headerHeight);
        }

        protected void ApplyToMultipleCells(TArea area, Action<SerializedProperty> action)
        {
            foreach (SerializedProperty cell in GetEnumeratorToArea(area))
                action(cell);

            serializedUnityObject.ApplyModifiedProperties();
        }

        protected abstract IEnumerable<SerializedProperty> GetEnumeratorToArea(TArea area);
        protected abstract TArea FullArea { get; }

        protected abstract void UpdateState_BeforeGetPropertyHeight();

        void UpdateState_BeforeDrawingSelected()
        {
            selectedPropertyRect = new Rect(
                fullRect.x,
                collectionRect.y + collectionRect.height + 1,
                fullRect.width,
                templateHeightFull + 2);

            int shift = (isTypeFolding ? indentWidth : 0);
            selectedPropertyContentRect = new Rect(
                selectedPropertyRect.x + shift + 1,
                selectedPropertyRect.y + 1,
                selectedPropertyRect.width - shift - 2,
                templateHeightFull);
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            UpdateState_BeforeHeader(position);

            // Header
            if (!TryDrawHeader(property, label)) return;

            if (!property.isExpanded) return;

            // Collection
            DrawCollection();

            if (collectionRect.Contains(Event.current.mousePosition))
                HandleActionKeys();

            if (!SelectedAny) return;

            // Selected
            UpdateState_BeforeDrawingSelected();
            DrawSelectedProperty();

        }

        bool TryDrawHeader(SerializedProperty property, GUIContent label)
        {
            EditorHelper.DrawBox(headerRect, false);

            if (IsExpandable)
            {
                property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);
                if (GUI.Button(foldoutRect, string.Empty, foldoutButtonStyle))
                    property.isExpanded = !property.isExpanded;
            }
            else
                EditorGUI.LabelField(foldoutRect, label);
            


            if (fieldsProperty == null)
            {
                DrawSerializationErrorMessage();
                return false;
            }

            if (IsMultipleObjectSelected)
            {
                DrawMultipleSelectMessage();
                return false;
            }

            DrawAdditionalHeader();
            UpdateState_BeforeDrawingCollection();
            return true;
        }

        protected virtual int AdditionalHeaderWidth => 0;

        protected void DrawAdditionalHeader()
        {
            const int buttonsFieldsWidth = 76;
            int sizeFieldWidth = AdditionalHeaderWidth - buttonsFieldsWidth;
            
            const int okButtonWidth = 30;
            const int horizontalSpacing = 2;

            // Matrix Size
            bool resize = this.context == Context.Resize;

            var additionalHeaderRect = new Rect(
                headerRect.x + headerRect.width - AdditionalHeaderWidth + 1,
                headerRect.y - 1,
                AdditionalHeaderWidth,
                headerHeight + 2);

            var sizeLabelPosition = new Rect(
                additionalHeaderRect.x,
                additionalHeaderRect.y + 2,
                resize ? sizeFieldWidth - okButtonWidth : sizeFieldWidth,
                additionalHeaderRect.height - 4);


            GUI.enabled = resize;
                
            TIndexVector size = this.context == Context.Resize ? tempSize : MatrixSize;
            tempSize = DrawIndexVectorUI(sizeLabelPosition, size);

            if (resize)
            {
                var okButtonRect = new Rect(
                    sizeLabelPosition.x + sizeLabelPosition.width + horizontalSpacing,
                    sizeLabelPosition.y,
                    okButtonWidth - horizontalSpacing,
                    sizeLabelPosition.height);
                if (GUI.Button(okButtonRect, "OK"))
                {
                    RecordForUndo("ResizeMatrix");
                    MatrixSize = tempSize;
                    this.context = Context.Edit;
                    Event.current.Use();
                }
            }

            GUI.enabled = true;

            // Menu
            
            var contextButtonsPosition = new Rect(
                additionalHeaderRect.x + sizeFieldWidth + horizontalSpacing,
                additionalHeaderRect.y,
                AdditionalHeaderWidth - sizeFieldWidth - horizontalSpacing,
                additionalHeaderRect.height);

            
            GUIContent contextIcon;
            switch (this.context)
            {
                case Context.Edit:
                    contextIcon = EditorGUIUtility.IconContent("editicon.sml");
                    break;
                case Context.Paint:
                    contextIcon = EditorGUIUtility.IconContent("ClothInspector.PaintTool");
                    break;
                case Context.Resize:
                    contextIcon = EditorGUIUtility.IconContent("ScaleTool");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (matrix2DViews.Count > 0)
            {
                contextButtonsPosition.width = (contextButtonsPosition.width - horizontalSpacing) / 2;
                var viewButtonsPosition = new Rect(
                    contextButtonsPosition.x + contextButtonsPosition.width + horizontalSpacing,
                    contextButtonsPosition.y,
                    contextButtonsPosition.width,
                    contextButtonsPosition.height);
 
                var viewNames = new GUIContent[matrix2DViews.Count];
                for (var i = 0; i < viewNames.Length; i++)
                    viewNames[i] = new GUIContent(matrix2DViews[i].ViewName);

                selectedViewIndex = EditorGUI.Popup(
                    viewButtonsPosition, GUIContent.none, selectedViewIndex, viewNames);
                 var viewIcon = new GUIContent(
                     EditorGUIUtility.IconContent("ClothInspector.ViewValue").image,
                     $"View: {matrix2DViews[selectedViewIndex].ViewName}"); 
                GUI.Button(viewButtonsPosition, viewIcon);
                contextIcon.text = null;
            }
            else
                contextIcon.text = $"   {this.context}";
            

            var context = (Context) EditorGUI.EnumPopup(contextButtonsPosition, GUIContent.none, this.context);
            GUI.Button(contextButtonsPosition, contextIcon);
            if (context != this.context)
            {
                DeselectAll();
                this.context = context;
                tempSize = MatrixSize;
                Event.current.Use();
            }
        }

        protected abstract TIndexVector DrawIndexVectorUI(Rect position, TIndexVector size);

        void HandleActionKeys()
        {
            if (Event.current.type != EventType.KeyDown) return;

            bool mod = Event.current.modifiers == EventModifiers.Control;
            KeyCode key = Event.current.keyCode;

            bool selectedAny = SelectedAny;

            bool copyCommand = mod && key == KeyCode.C;
            bool pasteCommand = mod && key == KeyCode.V;
            bool deselectCommand = mod && key == KeyCode.D;
            bool selectAllCommand = mod && key == KeyCode.A;
            bool flipHorizontalCommand = mod && key == KeyCode.Q;
            bool flipVerticalCommand = mod && key == KeyCode.W;
            bool upCommand = selectedAny && key == KeyCode.UpArrow;
            bool rightCommand = !mod && selectedAny && key == KeyCode.RightArrow;
            bool downCommand = !mod && selectedAny && key == KeyCode.DownArrow;
            bool leftCommand = !mod && selectedAny && key == KeyCode.LeftArrow;

            bool anyCommand = copyCommand || pasteCommand || deselectCommand || selectAllCommand ||
                              upCommand || rightCommand || downCommand || leftCommand;


            if (copyCommand)
                CopySelectedArea();
            else if (pasteCommand)
                PasteCopiedArea();
            else if (deselectCommand)
                DeselectAll();
            else if (selectAllCommand)
                SelectAll();
            else if (flipHorizontalCommand)
                FlipHorizontal();
            else if (flipVerticalCommand)
                FlipSelectedVertical();
            else if (upCommand)
                MoveSelectedArea(GeneralDirection2D.Up);
            else if (rightCommand)
                MoveSelectedArea(GeneralDirection2D.Right);
            else if (downCommand)
                MoveSelectedArea(GeneralDirection2D.Down);
            else if (leftCommand)
                MoveSelectedArea(GeneralDirection2D.Left);

            if (anyCommand)
                Event.current.Use();
        }

        protected abstract void SelectAll();

        protected abstract TIndexVector MatrixSize { get; set; }

        protected virtual void MoveSelectedArea(GeneralDirection2D up) { }

        protected abstract void PasteCopiedArea();

        protected void CopySelectedArea()
        {          
            if (!SelectedAny) return;

            copiedArea = selectedArea;
            selectedArea = default;
        }

        protected void RecordForUndo(string name) =>
            Undo.RecordObject(collectionProperty.serializedObject.targetObject, name);

        protected void DeselectAll()
        {
            selectedArea = default;
            copiedArea = default;
        }

        protected virtual void FlipHorizontal(){}
        protected virtual void FlipSelectedVertical(){}

        protected abstract void DrawCollection();

        protected abstract void UpdateState_BeforeDrawingCollection();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            UpdateState_References(property);
            UpdateState_Views(property);
            InitializeViewBeforeDrawing(property);
            
            fieldsProperty = collectionProperty.FindPropertyRelative("fields");
            UpdateState_BeforeGetPropertyHeight();
            return GetFullHeight(property);
        }

        protected virtual void InitializeViewBeforeDrawing(SerializedProperty property){ }

        protected virtual void UpdateState_Views(SerializedProperty property) { }

        void CopyFirstElementToOthers(TArea area)
        {
            ApplyToMultipleCells(area, (cell) => FirstSelectedProperty.CopyPropertyValueTo(cell));
            serializedUnityObject.ApplyModifiedProperties();
        }
        protected sealed override SerializedProperty FirstSelectedProperty => GetCellProperty(FirstSelectedIndex);

        protected sealed override SerializedProperty FirstProperty => GetCellProperty(MinimumIndex); 
        public abstract SerializedProperty GetCellProperty(TIndexVector index);
        protected abstract TIndexVector FirstSelectedIndex { get; }
        protected abstract TIndexVector MinimumIndex { get; }

        void DrawSelectedProperty()
        {
            if (!SelectedAny) return;
            EditorHelper.DrawBox(selectedPropertyRect, false);

            SerializedProperty propertyToDraw = FirstSelectedProperty;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(
                selectedPropertyContentRect, 
                propertyToDraw, 
                new GUIContent(SelectedIndexText),
                true);
            
            IsSelectedExpanded = propertyToDraw.isExpanded;
            if (EditorGUI.EndChangeCheck())
                CopyFirstElementToOthers(selectedArea);
        }

        protected abstract string SelectedIndexText { get; }

        float GetFullHeight(SerializedProperty property)
        {
            const float headerH = (headerHeight + 2);
            if (!IsExpanded) return headerH;

            float collectionH = CollectionHeight + 1;
            float selectedH = SelectedAny ? (selectedPropertyRect.height + 1) : 0;

            return headerH + collectionH + selectedH + extraSpacingWhenOpen;
        }

        protected abstract float CollectionHeight { get; }
    }
}
#endif
#if UNITY_EDITOR
using System.Linq;
using MUtility;
using System;
using System.Collections.Generic;
using System.Reflection; 
using UnityEditor;
using UnityEngine;

namespace Utility.SerializableCollection.Editor
{
    [CustomPropertyDrawer(typeof(SetMatrix2DViewAttribute), useForChildren: true)]
    [CustomPropertyDrawer(typeof(Matrix2DBase), useForChildren: true)]
    public class Matrix2DDrawer : MatrixDrawer<Matrix2DBase, RectInt, Vector2Int, Matrix2DViewBase>
    {
        struct MouseInfo
        {
            public bool inRect;
            public Vector2Int cellIndex;
            public Vector2 inCellPosition;
            public bool right;
            public bool left;
        }
        

        // Constants
        const float marginSize = 30; 
        const float minimumSizeToDrawNumber = 12;

        // Updatable Data 
        int _matrixWidth;
        int _matrixHeight;
        float _cellWidth;
        float _cellHeight;
        float _marginLeftWidth;
        float _marginTopHeight; 


        Matrix2DViewBase CurrentView => matrix2DViews.IsNullOrEmpty()
            ? defaultView
            : matrix2DViews[Mathf.Clamp(selectedViewIndex, min: 0, matrix2DViews.Count - 1)];

        protected sealed override RectInt FullArea => new RectInt(xMin: 0, yMin: 0, _matrixWidth, _matrixHeight);
        protected sealed override Vector2Int MinimumIndex => Vector2Int.zero;
        protected sealed override Vector2Int FirstSelectedIndex => selectedArea.min;
        protected sealed override bool SelectedAny => selectedArea.width > 0 && selectedArea.height > 0;
        protected sealed override bool CopiedAny => copiedArea.width > 0 && copiedArea.height > 0;
        protected sealed override float CollectionHeight => _marginTopHeight + (_cellHeight * _matrixHeight);

        protected sealed override Vector2Int MatrixSize
        {
            get => targetObject.Size;
            set => targetObject.Size = value;
        }

        public Matrix2DBase GetMatrix2D => targetObject;
        public SerializedObject SerializedUnityObject => serializedUnityObject;
        public SerializedProperty MatrixProperty => collectionProperty;
        public SerializedProperty FieldsProperty => fieldsProperty;
        
        protected override void InitializeViewBeforeDrawing(SerializedProperty property)
        {
            CurrentView.InitializationsBeforeDrawing(); 
        }

        protected sealed override void UpdateState_Views(SerializedProperty property)
        {
            if (defaultView != null)
                return;

            defaultView = new Matrix2DDefaultView();
            defaultView.Init(this);

            matrix2DViews.Clear();
            var viewClassNames = new HashSet<string>();

            // Search for views based on Field attributes
            var fieldAttribute = attribute as SetMatrix2DViewAttribute;
            string[] fieldViewNames = fieldAttribute == null ? new string[0] : fieldAttribute.DrawerTypeNames;

            foreach (string fieldViewName in fieldViewNames)
                viewClassNames.Add(fieldViewName);
            
            // Search for views based on Type attributes
            if (viewClassNames.IsNullOrEmpty())
            {
                Type collectionType = targetObject.GetType();
                object[] typeAttributes = collectionType.GetCustomAttributes(typeof(SetMatrix2DViewAttribute), inherit: true);
                string[] typeViewNames = typeAttributes.IsNullOrEmpty()
                    ? new string[0]
                    : ((SetMatrix2DViewAttribute) typeAttributes[0]).DrawerTypeNames;
                foreach (string typeViewName in typeViewNames)
                    viewClassNames.Add(typeViewName);
            }

            // Creating instances of Views
            foreach (string viewClassName in viewClassNames)
            {
                Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                IEnumerable<Type> allTypes = allAssemblies.SelectMany(x => x.GetTypes());
                Type type = allTypes.FirstOrDefault(x => x.Name == viewClassName);
                if (type == null)
                {
                    Debug.LogWarning($"Type Not Found: {viewClassName} \nTry Use Namespace.TypeName format");
                    continue;
                }

                if (!type.IsSubclassOf(typeof(Matrix2DViewBase)))
                {
                    Debug.LogWarning($"Type {viewClassName} is Not a Matrix2DView!");
                    continue;
                }

                var viewInstance = (Matrix2DViewBase) Activator.CreateInstance(type);
                if (viewInstance == null)
                {
                    Debug.LogWarning($"Can't Instantiate Type {viewClassName}");
                    continue;
                }

                viewInstance.Init(this);
                matrix2DViews.Add(viewInstance);
            }

            if (matrix2DViews.Count == 1)
            {
                defaultView = matrix2DViews[index: 0];
                matrix2DViews.Clear();
            }
        }

        protected sealed override int AdditionalHeaderWidth => 186;

        protected sealed override Vector2Int DrawIndexVectorUI(Rect position, Vector2Int size) =>
            EditorGUI.Vector2IntField(position, GUIContent.none, size);

        protected sealed override void FlipHorizontal()
        {
            RecordForUndo("Horizontal Flip");
            targetObject.FlipHorizontal(selectedArea.size != Vector2Int.zero ? selectedArea : FullArea);
        }

        protected sealed override void FlipSelectedVertical()
        {
            RecordForUndo("Vertical Flip");
            targetObject.FlipVertical(selectedArea.size != Vector2Int.zero ? selectedArea : FullArea);
        }

        protected sealed override string SelectedIndexText
        {
            get
            {
                string xText = selectedArea.size.x == 1
                    ? selectedArea.position.x.ToString()
                    : $"{selectedArea.xMin}-{selectedArea.xMax - 1}";
                string yText = selectedArea.size.y == 1
                    ? selectedArea.position.y.ToString()
                    : $"{selectedArea.yMin}-{selectedArea.yMax - 1}";
                var title = $"X: {xText} , Y: {yText}";
                return title;
            }
        }

        protected sealed override IEnumerable<SerializedProperty> GetEnumeratorToArea(RectInt area)
        {
            for (int x = area.xMin; x < area.xMax; x++)
            for (int y = area.yMin; y < area.yMax; y++)
                yield return GetCellProperty(new Vector2Int(x, y));
        }

        protected sealed override void UpdateState_BeforeGetPropertyHeight()
        {
            if (fieldsProperty == null) return;
            _matrixWidth = collectionProperty.FindPropertyRelative("width").intValue;
            _matrixHeight = fieldsProperty.arraySize / _matrixWidth;

            Matrix2DViewBase currentView = CurrentView;
            _cellHeight = currentView.ForceCellHeight ?? singleLineHeight;

            bool resizing = context == Context.Resize;
            _marginLeftWidth = currentView.ShowHeaders || resizing ? marginSize : 0;
            _marginTopHeight = currentView.ShowHeaders || resizing ? marginSize : 0;
        }

        protected sealed override void UpdateState_BeforeDrawingCollection()
        {
            _cellWidth = CurrentView.ForceCellWidth ?? (fullRect.width - _marginLeftWidth) / _matrixWidth;
            float w = _marginLeftWidth + (_cellWidth * _matrixWidth);
            collectionRect = new Rect(
                fullRect.x + (fullRect.width / 2) - (w / 2),
                fullRect.y + headerHeight + 1,
                w,
                _marginTopHeight + (_cellHeight * _matrixHeight));
        }
        
        public sealed override SerializedProperty GetCellProperty(Vector2Int index) =>
            fieldsProperty.GetArrayElementAtIndex(index.x + (index.y * _matrixWidth));

        protected sealed override void DrawCollection()
        {
            EditorHelper.DrawBox(collectionRect, borderInside: false);
            DrawMargin();
            DrawBackgroundLines();
            DrawMarginText();
            DrawCells();
            
            MouseInfo mouseInfo = ProcessMouse();
            
            DrawCellSelection(copyColor, copiedArea, inside: false);
            DrawCellSelection(selectionColor, selectedArea, inside: true);

            HandleMouse(mouseInfo);
        }

        void DrawBackgroundLines()
        {
            for (var y = 0; y < _matrixHeight; y++)
                if (y % 2 == 0)
                    EditorGUI.DrawRect(
                        new Rect(
                            collectionRect.x,
                            collectionRect.y + _marginTopHeight + (y * _cellHeight),
                            collectionRect.width,
                            _cellHeight),
                        EditorHelper.tableEvenLineColor);
            for (var x = 0; x < _matrixWidth; x++)
                if (x % 2 == 0)
                    EditorGUI.DrawRect(
                        new Rect(
                            collectionRect.x + _marginLeftWidth + (x * _cellWidth),
                            collectionRect.y,
                            _cellWidth,
                            collectionRect.height),
                        EditorHelper.tableEvenLineColor);
        }

        void DrawMargin()
        {
            if (!CurrentView.ShowHeaders && context != Context.Resize)
                return;

            EditorGUI.DrawRect(
                new Rect(collectionRect.x, collectionRect.y, collectionRect.width, _marginTopHeight),
                EditorHelper.tableMarginColor);
            EditorGUI.DrawRect(
                new Rect(collectionRect.x, collectionRect.y, _marginLeftWidth, collectionRect.height),
                EditorHelper.tableMarginColor);
        }

        void DrawMarginText()
        {
            if (!CurrentView.ShowHeaders && context != Context.Resize)
                return;
            for (var x = 0; x < _matrixWidth; x++)
                DrawMarginHorizontal(x);
            for (var y = 0; y < _matrixHeight; y++)
                DrawMarginVertical(y);
        }

        MouseInfo ProcessMouse()
        {
            Vector2 position = Event.current.mousePosition;
            
            if(!collectionRect.Contains(position))
                return new MouseInfo();
            
            var inGridRelativePos =
                new Vector2(
                    (position.x - collectionRect.x - _marginLeftWidth) / _cellWidth,
                    (position.y - collectionRect.y - _marginTopHeight) / _cellHeight
                );

            var cellIndex = new Vector2Int(
                inGridRelativePos.x < 0 ? -1 : (int) inGridRelativePos.x,
                inGridRelativePos.y < 0 ? -1 : (int) inGridRelativePos.y);

            var inCellRelativePos = new Vector2(
                inGridRelativePos.x < 0
                    ? (position.x - collectionRect.x) / _marginLeftWidth
                    : inGridRelativePos.x % 1,
                inGridRelativePos.y < 0
                    ? (position.y - collectionRect.y) / _marginTopHeight
                    : inGridRelativePos.y % 1);
            
            return new MouseInfo()
            {
                inRect = true, 
                cellIndex = cellIndex, 
                inCellPosition = inCellRelativePos, 
                right = Event.current.button == 1, 
                left = Event.current.button == 0,
            };
        }
        
        void HandleMouse(MouseInfo mouseInfo)
        {
            if (mouseInfo.inRect)
            {
                if (context == Context.Resize)
                    HandleResizing(mouseInfo);
                else if (context == Context.Paint)
                    HandlePaint(mouseInfo);
                else
                   HandleSelecting(mouseInfo);
            } 
            if (Event.current.type == EventType.MouseUp)
                selecting = false;
        }

        void HandlePaint(MouseInfo mouseInfo)
        {
            if (mouseInfo.right)
                copiedArea = HandleSelecting(mouseInfo.cellIndex, copiedArea);

            var minPos = new Vector2Int(
                Mathf.Max(a: 0,mouseInfo.cellIndex.x - copiedArea.size.x + 1),
                Mathf.Max(a: 0,mouseInfo.cellIndex.y - copiedArea.size.y + 1));
            var maxPos = new Vector2Int(
                mouseInfo.cellIndex.x + 1,
                mouseInfo.cellIndex.y + 1);
            var paintArea = new RectInt(minPos, maxPos - minPos);
            
            DrawCellSelection(selectionColor, paintArea, inside: true);

            bool paint = mouseInfo.left &&
                         (Event.current.type == EventType.MouseDrag ||
                          Event.current.type == EventType.MouseDown);

            if (Event.current.type == EventType.MouseDrag || 
                Event.current.type == EventType.MouseMove)
            {
                RepaintAllInspectors();
            }

            if (paint)
                CopyArea(copiedArea, paintArea);
        }
        
        void HandleResizing(MouseInfo mouseInfo)
        {
            Vector2Int cellIndexAtMouse = mouseInfo.cellIndex; 
            Vector2 insideMarginPosition = mouseInfo.inCellPosition; 
            
            DrawResizeButtons(cellIndexAtMouse, mouseInfo.inCellPosition);
            if (Event.current.type == EventType.MouseMove)
                Event.current.Use();
            

            if (!(cellIndexAtMouse.x < 0 ^ cellIndexAtMouse.y < 0)) return;

            if (cellIndexAtMouse.x < 0)
            {
                if (insideMarginPosition.x < 0.5)
                    DrawCellSelection(
                        EditorHelper.ErrorRedColor, 
                        new RectInt(xMin: 0, cellIndexAtMouse.y, _matrixWidth, height: 1),
                        inside: true);
                else if (insideMarginPosition.y < 0.5)
                    DrawHorizontalSeparator(insertColor, startX: 0, _matrixWidth - 1, cellIndexAtMouse.y);
                else
                    DrawHorizontalSeparator(insertColor, startX: 0, _matrixWidth - 1, cellIndexAtMouse.y + 1);
            }
            else
            {
                if (insideMarginPosition.y < 0.5)
                    DrawCellSelection(
                        EditorHelper.ErrorRedColor, 
                        new RectInt(cellIndexAtMouse.x, yMin: 0, width: 1, _matrixHeight),
                        inside: true);
                else if (insideMarginPosition.x < 0.5)
                    DrawVerticalSeparator(insertColor, startY: 0, _matrixHeight - 1, cellIndexAtMouse.x);
                else
                    DrawVerticalSeparator(insertColor, startY: 0, _matrixHeight - 1, cellIndexAtMouse.x + 1);
            }
        }

        void DrawResizeButtons(Vector2Int cellIndex, Vector2 inCellPosition)
        {
            if (cellIndex.y >= 0)
                DrawRowActionButtons(cellIndex.y, inCellPosition.y, GetLeftMarginCellRect(cellIndex.y));
            if (cellIndex.x >= 0)
                DrawColumnActionButtons(cellIndex.x, inCellPosition.x, GetTopMarginCellRect(cellIndex.x));
        }

        void HandleSelecting(MouseInfo mouseInfo)
        {
            if (mouseInfo.left)
                selectedArea = HandleSelecting(mouseInfo.cellIndex, selectedArea);
            else if (mouseInfo.right)
                copiedArea = HandleSelecting(mouseInfo.cellIndex, copiedArea);
        }

        protected sealed override void SelectAll()
        {
            var all = new RectInt(xMin: 0, yMin: 0, _matrixWidth, _matrixHeight);
            selectedArea = selectedArea.Equals(all) ? new RectInt(xMin: 0, yMin: 0, width: 0, height: 0) : all;
        }

        protected sealed override void MoveSelectedArea(GeneralDirection2D direction)
        {
            RecordForUndo($"Move Area {direction}");

            if (!SelectedAny) return;
            if (!targetObject.MoveArea(selectedArea, direction)) return;

            switch (direction)
            {
                case GeneralDirection2D.Up:
                    selectedArea.position = new Vector2Int(selectedArea.position.x, selectedArea.position.y - 1);
                    break;
                case GeneralDirection2D.Down:
                    selectedArea.position = new Vector2Int(selectedArea.position.x, selectedArea.position.y + 1);
                    break;
                case GeneralDirection2D.Right:
                    selectedArea.position = new Vector2Int(selectedArea.position.x + 1, selectedArea.position.y);
                    break;
                case GeneralDirection2D.Left:
                    selectedArea.position = new Vector2Int(selectedArea.position.x - 1, selectedArea.position.y);
                    break;
                default:
                    selectedArea.position = selectedArea.position;
                    break;
            }
        }
 
        protected sealed override void PasteCopiedArea()
        {
            if (!CopiedAny || !SelectedAny)
                return;

            bool selectedOneCell = selectedArea.size == Vector2Int.one;

            RectInt area = selectedOneCell
                ? new RectInt(selectedArea.position, copiedArea.size)
                : selectedArea;

            CopyArea(copiedArea, area);
            
            selectedArea = new RectInt(
                area.x, area.y,
                Mathf.Min(area.width, _matrixWidth - area.x),
                Mathf.Min(area.height, _matrixHeight - area.y)
            );
        }

        void CopyArea(RectInt source, RectInt destination)
        {
            for (var x = 0; x < destination.size.x; x++)
            for (var y = 0; y < destination.size.y; y++)
            {
                var sourceCoordinate = new Vector2Int(x, y);
                sourceCoordinate = MathHelper.Mod(sourceCoordinate, source.size.x, source.size.y);
                sourceCoordinate.x += source.x;
                sourceCoordinate.y += source.y;

                var destinationCoordinate = new Vector2Int(
                    destination.position.x + x,
                    destination.position.y + y);

                if (destinationCoordinate.x < 0 ||
                    destinationCoordinate.x >= _matrixWidth ||
                    destinationCoordinate.y < 0 ||
                    destinationCoordinate.y >= _matrixHeight)
                    continue;

                SerializedProperty sourceProp =
                    GetCellProperty(new Vector2Int(sourceCoordinate.x, sourceCoordinate.y));
                SerializedProperty destinationProp =
                    GetCellProperty(new Vector2Int(destinationCoordinate.x, destinationCoordinate.y));

                sourceProp.CopyPropertyValueTo(destinationProp);
            }
        }

        RectInt HandleSelecting(Vector2Int mousePositionInt, RectInt area)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                mouseDownCoord = mousePositionInt;
                selecting = true;
                RectInt newArea = GatNewArea();

                if (newArea.position == area.position && newArea.size == area.size)
                    area.size = Vector2Int.zero;
                else
                    area = newArea;

                GUI.FocusControl(name: null);
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseDrag)
            {
                if (!selecting)
                    return area;
                area = GatNewArea();
                GUI.FocusControl(name: null);
                Event.current.Use();
            }

            return area;

            RectInt GatNewArea()
            {
                if (mouseDownCoord.x < 0 && mouseDownCoord.y < 0)
                    return new RectInt(xMin: 0, yMin: 0, _matrixWidth, _matrixHeight);
                if (mouseDownCoord.x < 0)
                    return PointsToRectInt(new Vector2Int(x: 0, mouseDownCoord.y),
                        new Vector2Int(_matrixWidth - 1, mousePositionInt.y));
                if (mouseDownCoord.y < 0)
                    return PointsToRectInt(new Vector2Int(mouseDownCoord.x, y: 0),
                        new Vector2Int(mousePositionInt.x, _matrixHeight - 1));
                return PointsToRectInt(mouseDownCoord, mousePositionInt);
            }
        }
        
        static RectInt PointsToRectInt(Vector2Int a, Vector2Int b) => PointsToRectInt(a.x, b.x, a.y, b.y);

        static RectInt PointsToRectInt(int x1, int x2, int y1, int y2)
        {
            x1 = Mathf.Max(x1, b: 0);
            x2 = Mathf.Max(x2, b: 0);
            y1 = Mathf.Max(y1, b: 0);
            y2 = Mathf.Max(y2, b: 0);
            int xMin = Mathf.Min(x1, x2);
            int xMax = Mathf.Max(x1, x2);
            int yMin = Mathf.Min(y1, y2);
            int yMax = Mathf.Max(y1, y2);
            return new RectInt(xMin, yMin, 1 + xMax - xMin, 1 + yMax - yMin);
        }

        Rect AreaToRect(RectInt area)
        {
            return new Rect(
                collectionRect.x + _marginLeftWidth + (area.xMin * _cellWidth),
                collectionRect.y + _marginTopHeight + (area.yMin * _cellHeight),
                _cellWidth * area.width,
                _cellHeight * area.height);
        }

        void DrawCellSelection(Color color, RectInt area, bool inside)
        {
            if (area.width == 0 || area.height == 0)
                return;
            DrawCellBorder(AreaToRect(area), color, inside);
        }

        static void DrawCellBorder(Rect rect, Color color, bool inside)
        {
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

        void DrawHorizontalSeparator(Color color, int startX, int endX, int afterY)
        {
            int minX = Mathf.Min(startX, endX);
            int maxX = Mathf.Max(startX, endX);

            const float width = selectionBorderWidth;

            var rect = new Rect(
                collectionRect.x + _marginLeftWidth + (minX * _cellWidth),
                collectionRect.y + _marginTopHeight + (afterY * _cellHeight) - (width / 2),
                _cellWidth * (1 + maxX - minX), width);

            EditorGUI.DrawRect(rect, color);
        }

        void DrawVerticalSeparator(Color color, int startY, int endY, int afterX)
        {
            int minY = Mathf.Min(startY, endY);
            int maxY = Mathf.Max(startY, endY);

            const float width = selectionBorderWidth;

            var rect = new Rect(
                collectionRect.x + _marginLeftWidth + (afterX * _cellWidth) - (width / 2),
                collectionRect.y + _marginTopHeight + (minY * _cellHeight),
                width, _cellHeight * (1 + maxY - minY));

            EditorGUI.DrawRect(rect, color);
        }

        void DrawMarginHorizontal(int x)
        {  
            Rect rect = GetTopMarginCellRect(x);
            if (rect.width >= minimumSizeToDrawNumber && rect.height >= minimumSizeToDrawNumber)
                EditorGUI.LabelField(rect, CurrentView.HorizontalHeaderText(x), centerAlignment);
        }

        Rect GetTopMarginCellRect(int x) => new Rect(
            collectionRect.x + _marginLeftWidth + (x * _cellWidth),
            collectionRect.y,
            _cellWidth,
            _marginTopHeight);

        readonly GUIContent _insertGuiContent = EditorGUIUtility.IconContent("Toolbar Plus");
        readonly GUIContent _removeGuiContent = EditorGUIUtility.IconContent("Toolbar Minus"); 
        readonly Color _removeColor = new Color(r: 1f, g: 0.47f, b: 0.41f);
        void DrawColumnActionButtons(int x, float inCellX,  Rect rect)
        {        
            float size = rect.height / 2; 
 
            bool left = inCellX < 0.5f;
            int insertX = left ? x : x + 1;
            var insertRect = new Rect(
                (left ? rect.x : rect.xMax) - (size / 2),
                rect.y + size ,
                size, 
                size);

  
            if (GUI.Button(insertRect, GUIContent.none))
            {
                RecordForUndo("Insert Column");
                targetObject.InsertColumnTo(insertX);
                tempSize = targetObject.Size;
            }
            GUI.Label(insertRect, _insertGuiContent);

            GUI.color = _removeColor;
            var removeRect = new Rect( rect.center.x-(size/2), rect.y, size, size);
            if (GUI.Button(removeRect, GUIContent.none ))
            {
                RecordForUndo("Remove Column");
                targetObject.RemoveColumnAt(x);
                tempSize = targetObject.Size;
            } 
            GUI.Label(removeRect, _removeGuiContent);
            GUI.color = Color.white;
        }

        void DrawMarginVertical(int y)
        {
            Rect rect = GetLeftMarginCellRect(y);
            if (rect.width >= minimumSizeToDrawNumber && rect.height >= minimumSizeToDrawNumber)
            {
                EditorGUI.LabelField(rect, CurrentView.VerticalHeaderText(y), centerAlignment);
            }
        }

        Rect GetLeftMarginCellRect(int y) => new Rect(
            collectionRect.x,
            collectionRect.y + _marginTopHeight + (y * _cellHeight),
            _marginLeftWidth,
            _cellHeight);

        void DrawRowActionButtons(int y, float inCellY, Rect rect)
        {
            float size = rect.width / 2; 
 
            bool top = inCellY < 0.5f;
            int insertY = top ? y : y + 1;
            var insertRect = new Rect(
                rect.x + size, 
                (top ? rect.y : rect.yMax) - (size / 2),
                size, 
                size);
 
            if (GUI.Button(insertRect,GUIContent.none))
            {
                DeselectAll();
                RecordForUndo("Insert Row");
                targetObject.InsertRowTo(insertY);
                tempSize = targetObject.Size;
            }
            GUI.Label(insertRect, _insertGuiContent);

            GUI.color = _removeColor;
            var removeRect = new Rect(rect.x, rect.center.y-(size/2), size, size);
            if (GUI.Button(removeRect, GUIContent.none))
            {
                DeselectAll();
                RecordForUndo("Remove Row");
                targetObject.RemoveRowAt(y);
                tempSize = targetObject.Size;
            } 
            GUI.Label(removeRect, _removeGuiContent);
            GUI.color = Color.white;
        }

        void DrawCells()
        {
            for (var x = 0; x < _matrixWidth; x++)
            for (var y = 0; y < _matrixHeight; y++)
                if (targetObject.InRange(x, y))
                    CurrentView.DrawCell(GetCellRect(collectionRect, x, y), x, y);

        }

        Rect GetCellRect(Rect matrixRect, int x, int y)
        { 
            float xPos = matrixRect.x + _marginLeftWidth + (x * _cellWidth);
            float yPos = matrixRect.y + _marginTopHeight + (y * _cellHeight);

            return new Rect(xPos, yPos, _cellWidth, _cellHeight); 
        }
    } 
}
#endif
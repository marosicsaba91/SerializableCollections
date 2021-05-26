using System;
using UnityEngine; 

namespace Utility.SerializableCollection
{
    [SetMatrix2DView( "Matrix2DBoolView","Matrix2DBoolInCellEditView")]
    [Serializable] public class Matrix2DBool : Matrix2D<bool> {}
    
    [SetMatrix2DView("Matrix2DDefaultView","Matrix2DIntHeatmapView","Matrix2DInCellEditView")]
    [Serializable] public class Matrix2DInt : Matrix2D<int> {}
    
    [SetMatrix2DView("Matrix2DDefaultView","Matrix2DFloatHeatmapView","Matrix2DInCellEditView")]
    [Serializable] public class Matrix2DFloat : Matrix2D<float> {}
    
    [SetMatrix2DView("Matrix2DDefaultView", "Matrix2DInCellEditView")]
    [Serializable] public class Matrix2DString : Matrix2D<string> {}
    
    [SetMatrix2DView("Matrix2DDefaultView", "Matrix2DInCellEditView")]
    [Serializable] public class Matrix2DVector2 : Matrix2D<Vector2> {}
    
    [SetMatrix2DView("Matrix2DDefaultView", "Matrix2DInCellEditView")]
    [Serializable] public class Matrix2DVector3 : Matrix2D<Vector3> {} 
    
    [SetMatrix2DView("Matrix2DDefaultView", "Matrix2DInCellEditView")]
    [Serializable] public class Matrix2DVector2Int : Matrix2D<Vector2Int> {}
    
    [SetMatrix2DView("Matrix2DDefaultView", "Matrix2DInCellEditView")]
    [Serializable] public class Matrix2DVector3Int : Matrix2D<Vector3Int> {} 
    
    [SetMatrix2DView("Matrix2DObjectView", "Matrix2DInCellEditView")]
    [Serializable] public class Matrix2DGameObject : Matrix2D<GameObject> {}   
    
    [SetMatrix2DView("Matrix2DObjectView", "Matrix2DInCellEditView")]
    [Serializable] public class Matrix2DTransform : Matrix2D<Transform> {} 
    
    [SetMatrix2DView("Matrix2DTextureView", "Matrix2DObjectView", "Matrix2DInCellEditView")]
    [Serializable] public class Matrix2DTexture : Matrix2D<Texture> {} 
}
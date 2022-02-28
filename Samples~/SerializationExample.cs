using System;
using System.Collections.Generic; 
using UnityEngine; 
using Utility.SerializableCollection; 

public enum Element
{
    Earth,
    Air,
    Fire,
    Water
}

public class SerializationExample : MonoBehaviour
{
    # pragma warning disable 414, 649
    
    // Default Type
    [SerializeField] Matrix2DBool matrix2DBool = default;
    [SerializeField] Matrix2DInt matrix2DInt = default;
    [SerializeField] Matrix2DFloat matrix2DFloat = default;
    [SerializeField] Matrix2DString matrix2DString = default;
    [SerializeField] Matrix2DVector2 matrix2DVector2 = default;
    [SerializeField] Matrix2DVector3 matrix2DVector3 = default;
    [SerializeField] Matrix2DVector2Int matrix2DVector2Int = default;
    [SerializeField] Matrix2DVector3Int matrix2DVector3Int = default;
    [SerializeField] Matrix2DGameObject matrix2DGameObject = default;
    [SerializeField] Matrix2DTransform matrix2DTransform = default;
    [SerializeField] Matrix2DTexture matrix2DTexture = default; 
    
    
    // Enum
    [SetMatrix2DView(
        "ElementMatrixView",
        "ElementMatrixPixelView",
        "ElementMatrixInCellView")]
    [Serializable] class Matrix2DElement : Matrix2D<Element> { }
    [SerializeField] Matrix2DElement matrix2DElement = default;
    [SetMatrix2DView("ElementMatrixPixelView")]
    [SerializeField] Matrix2DElement matrix2DElementBig = default;
    
    // Custom Type
    [Serializable] struct SomeCustomType
    {
        public bool someBool;
        public string someString;
        public AnimationCurve someAnimCurve ;
        public List<int> someList;
        public override string ToString() => someBool ? someString : "-";
    }
    [Serializable] class MatrixSomeCustomType : Matrix2D<SomeCustomType> {}
    [SerializeField] MatrixSomeCustomType matrix2DSomeCustomType = default;
    
    // Wrong Type Error
    struct SomeWrongType
    {
        public bool someBool ;
        public string someString ;
        public float someFloat ;
    }
    [Serializable] class MatrixSomeWrongType : Matrix2D<SomeWrongType> {}
    [SerializeField] MatrixSomeWrongType matrix2DSomeWrongType = default;

    void Start()
    {
        float f = matrix2DFloat[2, 3];
    }

# pragma warning restore 414, 649
}

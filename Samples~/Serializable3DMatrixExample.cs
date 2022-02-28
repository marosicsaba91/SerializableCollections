
using UnityEngine;
using Utility.SerializableCollection;

public class Serializable3DMatrixExample : MonoBehaviour
{
# pragma warning disable 414, 649
    [SerializeField] Matrix3DBool matrix3DBool = default;
    [SerializeField] Matrix3DInt matrix3DInt = default; 
# pragma warning restore 414, 649
}

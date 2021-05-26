﻿using System;
using UnityEngine; 

namespace Utility.SerializableCollection
{ 
    [Serializable] public class Matrix3DBool : Matrix3D<bool> {} 
    [Serializable] public class Matrix3DInt : Matrix3D<int> {}
     
    [Serializable] public class Matrix3DFloat : Matrix3D<float> {}
     
    [Serializable] public class Matrix3DString : Matrix3D<string> {}
     
    [Serializable] public class Matrix3DVector2 : Matrix3D<Vector2> {}
     
    [Serializable] public class Matrix3DVector3 : Matrix3D<Vector3> {} 
     
    [Serializable] public class Matrix3DVector2Int : Matrix3D<Vector2Int> {}
     
    [Serializable] public class Matrix3DVector3Int : Matrix3D<Vector3Int> {} 
     
    [Serializable] public class Matrix3DGameObject : Matrix3D<GameObject> {}   
     
    [Serializable] public class Matrix3DTransform : Matrix3D<Transform> {} 
     
    [Serializable] public class Matrix3DTexture : Matrix3D<Texture> {} 
}
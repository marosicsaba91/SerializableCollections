using System; 
using UnityEngine;
using Utility.SerializableCollection;

public class SerializedDictionaryExamples : MonoBehaviour
{
    [Serializable] class StringIntDictionary :SerializableDictionary<string, int> { }

    [SerializeField] StringIntDictionary stringIntDictionary;
}

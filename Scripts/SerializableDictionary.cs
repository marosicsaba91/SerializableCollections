using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility.SerializableCollection
{
	[Serializable] 
	public abstract class SerializableDictionary : IGenericCollection
	{
		public abstract Type ContainingType { get; }

		public abstract int Count { get; }

		internal abstract bool ContainsKeyMoreThanOnce(int index);

		internal abstract KeyValuePair<object, object> GetKeyValuePairAt(int index);
	}

	[Serializable] 
	public class SerializableDictionary<TKey, TValue> : SerializableDictionary, IDictionary<TKey, TValue>
	{ 
		[SerializeField] List<TKey> keys = new List<TKey>();
		[SerializeField] List<TValue> values = new List<TValue>();
 
		public ICollection<TKey> Keys => keys;
		public ICollection<TValue> Values => values;
		  
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			for (var i = 0; i < keys.Count; i++)
				yield return new KeyValuePair<TKey, TValue>(keys[i], values[i]);
		}

		static List<TKey> _sortList;
		
		public void SortByKey()
		{
			Comparison<TKey> comparison = Comparer<TKey>.Default.Compare;
			SortByKey(comparison);
		}
		
		public void SortByKey(Comparison<TKey> comparison)
		{
			if (_sortList == null)
				_sortList = new List<TKey>();
			else
				_sortList.Clear();
			
			// Copy
			foreach (TKey t in keys)
				_sortList.Add(t);

			// Sort
			_sortList.Sort(comparison);

			
			for (int i = 0; i < keys.Count; i++)
			{
				TKey key = _sortList[i];
				int oldIndex = keys.IndexOf(key);
				if (oldIndex != i)
				{
					TValue val = values[oldIndex];
					keys.RemoveAt(oldIndex);
					values.RemoveAt(oldIndex);
					keys.Insert(i, key);
					values.Insert(i, val);
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			if (keys.Contains(item.Key))
				throw new ArgumentException("Key is already in the dictionary");
			keys.Add(item.Key);
			values.Add(item.Value);
		}

		public void Add(TKey key, TValue value) => 
			Add(new KeyValuePair<TKey, TValue>(key, value));
		
		public void Clear()
		{
			keys.Clear();
			values.Clear();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			int index = keys.IndexOf(item.Key);
			if (index < 0) return false;
			return values[index].Equals(item.Value);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			for (int i = 0; i < array.Length && i+arrayIndex< keys.Count; i++) 
				array[i] = new KeyValuePair<TKey, TValue>(keys[i+arrayIndex], values[i+arrayIndex]);
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			int index = keys.IndexOf(item.Key);
			if (index < 0) return false;
			if (values[index].Equals(item.Value)) return false;
			
			keys.RemoveAt(index);
			values.RemoveAt(index);
			return true;
		}

		public sealed override Type ContainingType => typeof(KeyValuePair<TKey, TValue>);

		public sealed override int Count
		{
			get
			{
				if (keys == null || values == null || keys.Count != values.Count)
				{
					if (keys == null)
						keys = new List<TKey>();
					if (values == null)
						values = new List<TValue>();
					while (keys.Count > values.Count)
						keys.RemoveAt(keys.Count-1);
					while (keys.Count < values.Count)
						values.RemoveAt(values.Count-1);
				}

				return keys.Count;
			}
		}

		internal override bool ContainsKeyMoreThanOnce(int index)
		{
			if (index < 0 || index >= keys.Count) return false;
			TKey test = keys[index];
			for (var i = 0; i < keys.Count; i++)
			{
				if(i == index)
					continue;
				if (Equals(keys[i], test))
					return true;
			}

			return false;
		}

		internal override KeyValuePair<object, object> GetKeyValuePairAt(int index) => 
			new KeyValuePair<object, object>(keys[index], values[index]);

		public bool IsReadOnly => false;

		public bool ContainsKey(TKey key) => keys.Contains(key); 
		
		public bool Remove(TKey key)
		{
			int index = keys.IndexOf(key);
			if (index < 0) return false; 
			
			keys.RemoveAt(index);
			values.RemoveAt(index);
			return true;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			int index = keys.IndexOf(key);
			bool contains = index >= 0;
			value = contains ? values[index] : default;
			return contains;
		}

		public TValue this[TKey key]
		{
			get => values[keys.IndexOf(key)];
			set => values[keys.IndexOf(key)] = value;
		} 
	}
}
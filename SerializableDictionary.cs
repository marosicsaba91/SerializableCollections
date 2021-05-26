﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility.SerializableCollection
{
	public abstract class SerializableDictionaryBase :IGenericCollection
	{
		public abstract Type ContainingType { get; }

		public abstract int Count { get; }
	}

	public class SerializableDictionary<TKey, TValue> : SerializableDictionaryBase,
		IDictionary<TKey, TValue>
	{
		[SerializeField] List<TKey> _keys = default;
		[SerializeField] List<TValue> _values = default;

		public ICollection<TKey> Keys => _keys;
		public ICollection<TValue> Values => _values;

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			for (var i = 0; i < _keys.Count; i++)
				yield return new KeyValuePair<TKey, TValue>(_keys[i], _values[i]);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			if (_keys.Contains(item.Key))
				throw new ArgumentException("Key is already in the dictionary");
			_keys.Add(item.Key);
			_values.Add(item.Value);
		}

		public void Add(TKey key, TValue value) => 
			Add(new KeyValuePair<TKey, TValue>(key, value));
		
		public void Clear()
		{
			_keys.Clear();
			_values.Clear();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			int index = _keys.IndexOf(item.Key);
			if (index < 0) return false;
			return _values[index].Equals(item.Value);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			for (int i = 0; i < array.Length && i+arrayIndex< _keys.Count; i++) 
				array[i] = new KeyValuePair<TKey, TValue>(_keys[i+arrayIndex], _values[i+arrayIndex]);
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			int index = _keys.IndexOf(item.Key);
			if (index < 0) return false;
			if (_values[index].Equals(item.Value)) return false;
			
			_keys.RemoveAt(index);
			_values.RemoveAt(index);
			return true;
		}

		public override Type ContainingType => typeof(KeyValuePair<TKey, TValue>);
		public override int Count => _keys.Count;
		
		public bool IsReadOnly => false;

		public bool ContainsKey(TKey key) => _keys.Contains(key); 
		
		public bool Remove(TKey key)
		{
			int index = _keys.IndexOf(key);
			if (index < 0) return false; 
			
			_keys.RemoveAt(index);
			_values.RemoveAt(index);
			return true;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			int index = _keys.IndexOf(key);
			bool contains = index >= 0;
			value = contains ? _values[index] : default;
			return contains;
		}

		public TValue this[TKey key]
		{
			get => _values[_keys.IndexOf(key)];
			set => _values[_keys.IndexOf(key)] = value;
		} 
	}
}
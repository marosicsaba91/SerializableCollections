using System;
using System.Collections;
using System.Collections.Generic;
using MUtility;
using UnityEngine;

namespace Utility.SerializableCollection
{
	public abstract class Matrix3DBase : IGenericCollection
	{
		public abstract int Width { get; set; }
		public abstract int Height { get; set; }
		public abstract int Depth { get; set; }
		public abstract int Count { get; }

		public Vector3Int Size
		{
			get => new Vector3Int(Width, Height, Depth);
			set
			{
				Width = value.x;
				Height = value.y;
				Depth = value.z;
			}
		}

		public abstract void AddPlane(Axis3D axis);
		public abstract void RemovePlane(Axis3D axis);
		public abstract void AddPlane(Axis3D axis, int planeIndex);
		public abstract void RemovePlane(Axis3D axis, int planeIndex);
		public abstract Color? CellColor(int x, int y, int z);
		public abstract bool InRange(int x, int y, int z);
		public abstract Type ContainingType { get; }
	}

	[Serializable]
	public abstract class Matrix3D<T> : Matrix3DBase, IEnumerable<T>
	{
		// Used only in Inspector, DON'T MODIFY!!! 
		[SerializeField] List<T> fields = new List<T>() { default };
		[SerializeField] int width = 1;
		[SerializeField] int height = 1;
		// Used only in Inspector, DON'T DELETE!!!

		public sealed override int Count => fields.Count;

		public sealed override Type ContainingType => typeof(T);

		public sealed override Color? CellColor(int x, int y, int z) =>
			CellColor(new Vector3Int(x, y, z), this[x, y, z]);

		public virtual Color? CellColor(Vector3Int coordinate, T element) => null;
		public virtual Color? TextColor(Vector3Int coordinate, T element) => null;

		public virtual string Text(Vector3Int coordinate, T element) => element.ToString();

		public sealed override bool InRange(int x, int y, int z) =>
			x >= 0 && y >= 0 && z >= 0 &&
			x < Width && y < Height && z < Depth;

		public sealed override int Width
		{
			get => width;
			set => SetSizeOfAxis(value, Axis3D.X, () => Width);
		}

		public sealed override int Height
		{
			get => height;
			set => SetSizeOfAxis(value, Axis3D.Y, () => Height);
		}

		public override int Depth
		{
			get => fields.Count / (height * width);
			set => SetSizeOfAxis(value, Axis3D.Z, () => Depth);
		}

		void SetSizeOfAxis(int value, Axis3D axis, Func<int> getter)
		{
			if (getter() == value)
				return;
			if (value < 1)
				value = 1;

			while (value < getter())
				RemovePlane(axis);

			while (value > getter())
				AddPlane(axis);
		}


		public T this[int x, int y, int z]
		{
			get => fields[x + (y * width) + (z * width * height)];
			set => fields[x + (y * width) + (z * width * height)] = value;
		}

		public sealed override void AddPlane(Axis3D axis)
		{
			switch (axis)
			{
				case Axis3D.X:
					AddPlaneX(Width);
					break;
				case Axis3D.Y:
					AddPlaneY(Height);
					break;
				case Axis3D.Z:
					AddPlaneZ(Depth);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
			}
		}

		public sealed override void RemovePlane(Axis3D axis)
		{
			switch (axis)
			{
				case Axis3D.X:
					RemovePlaneX(Width);
					break;
				case Axis3D.Y:
					RemovePlaneY(Height);
					break;
				case Axis3D.Z:
					RemovePlaneZ(Depth);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
			}
		}

		public sealed override void AddPlane(Axis3D axis, int index)
		{
			switch (axis)
			{
				case Axis3D.X:
					AddPlaneX(index);
					break;
				case Axis3D.Y:
					AddPlaneY(index);
					break;
				case Axis3D.Z:
					AddPlaneZ(index);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
			}
		}

		public sealed override void RemovePlane(Axis3D axis, int index)
		{
			switch (axis)
			{
				case Axis3D.X:
					RemovePlaneX(index);
					break;
				case Axis3D.Y:
					RemovePlaneY(index);
					break;
				case Axis3D.Z:
					RemovePlaneZ(index);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
			}
		}


		public void AddPlaneX(int x)
		{
			if (x > width)
				return;

			for (int y = height - 1; y >= 0; y--)
				for (int z = Depth - 1; z >= 0; z--)
				{
					int index = x + (y * width) + (z * width * height);
					fields.Insert(index, default);
				}

			width++;
		}

		public void RemovePlaneX(int x)
		{
			if (width <= 1)
				return;

			for (int y = height - 1; y >= 0; y--)
				for (int z = Depth - 1; z >= 0; z--)
				{
					int index = x + (y * width) + (z * width * height);
					fields.RemoveAt(index);
				}

			width--;
		}

		public void AddPlaneY(int y)
		{
			if (y > height)
				return;

			for (int x = 0; x < width; x++)
				for (int z = Depth - 1; z >= 0; z--)
				{
					int index = x + (y * width) + (z * width * height);
					fields.Insert(index, default);
				}

			height++;
		}

		public void RemovePlaneY(int y)
		{
			if (height <= 1)
				return;

			for (int x = 0; x < width; x++)
				for (int z = Depth - 1; z >= 0; z--)
				{
					int index = x + (y * width) + (z * width * height);
					fields.RemoveAt(index);
				}

			height--;
		}

		public void AddPlaneZ(int z)
		{
			if (z > Depth)
				return;

			int cellsInPlainCount = width * height;
			int indexToInsert = z * cellsInPlainCount;
			for (int x = 0; x < cellsInPlainCount; x++)
				fields.Insert(indexToInsert, default);

		}

		public void RemovePlaneZ(int z)
		{
			if (Depth <= 1)
				return;

			int cellsInPlainCount = Width * Height;
			int indexToRemoveFrom = (z - 1) * cellsInPlainCount;
			fields.RemoveRange(indexToRemoveFrom, cellsInPlainCount);
		}





		public IEnumerator<T> GetEnumerator() => fields.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
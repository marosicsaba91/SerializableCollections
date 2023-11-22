using System;
using System.Collections;
using System.Collections.Generic;
using MUtility;
using UnityEngine;

namespace Utility.SerializableCollection
{
	public abstract class Matrix2DBase : IGenericCollection, IEnumerable
	{
		public abstract int Width { get; set; }
		public abstract int Height { get; set; }
		public abstract int Count { get; }

		public Vector2Int Size
		{
			get => new(Width, Height);
			set
			{
				Width = value.x;
				Height = value.y;
			}
		}

		public abstract void InsertRowTo(int rowIndex);
		public abstract void InsertColumnTo(int columnIndex);
		public abstract void AddRow();
		public abstract void RemoveRowAt(int rowIndex);
		public abstract void RemoveColumnAt(int columnIndex);
		public abstract void AddColumn();
		public abstract object GetElement(int x, int y);
		public abstract bool InRange(int x, int y);
		public abstract bool MoveArea(RectInt area, GeneralDirection2D direction);

		public bool MoveArea(RectInt area, GeneralDirection2D direction, int steps)
		{
			for (int i = 0; i < steps; i++)
				if (!MoveArea(area, direction))
					return false;
			return true;
		}

		public abstract Type ContainingType { get; }
		public abstract void FlipHorizontal(RectInt range);
		public abstract void FlipVertical(RectInt range);
		public abstract IEnumerator GetEnumerator();
	}

	[Serializable]
	public abstract class Matrix2D<T> : Matrix2DBase, IEnumerable<T>
	{
		// Used only in Inspector, DON'T RENAME, DON'T MODIFY !!! 
		[SerializeField] List<T> fields = new() { default };
		[SerializeField] int width = 1;
		// Warning ended

		public sealed override int Count => fields.Count;

		public sealed override Type ContainingType => typeof(T);

		public sealed override bool InRange(int x, int y) =>
			x >= 0 &&
			y >= 0 &&
			x < Width &&
			y < Height;

		public sealed override int Width
		{
			get => width;
			set
			{
				if (width == value)
					return;
				if (value < 1)
					value = 1;


				while (value < width)
					RemoveColumnAt(width - 1);

				while (value > width)
					AddColumn();
			}
		}

		public sealed override int Height
		{
			get => fields.Count / width;
			set
			{
				if (Height == value)
					return;
				if (value < 1)
					value = 1;

				while (value < Height)
					RemoveRowAt(Height - 1);

				while (value > Height)
					AddRow();
			}
		}

		public sealed override object GetElement(int x, int y) => this[x, y];

		public T this[int x, int y]
		{
			get => fields[x + (y * width)];
			set => fields[x + (y * width)] = value;
		}

		public sealed override void InsertRowTo(int rowIndex)
		{
			int indexToInsert = rowIndex * width;
			for (int x = 0; x < width; x++)
				fields.Insert(indexToInsert, default);
		}

		public sealed override void InsertColumnTo(int columnIndex)
		{
			for (int y = Height - 1; y >= 0; y--)
			{
				int indexToInsert = (y * width) + columnIndex;
				fields.Insert(indexToInsert, default);
			}

			width++;
		}

		public sealed override void AddColumn() => InsertColumnTo(width);

		public sealed override void AddRow() => InsertRowTo(Height);

		public sealed override void FlipHorizontal(RectInt range)
		{
			for (int x = 0; x < (range.width / 2); x++)
				for (int y = range.yMin; y < range.yMax; y++)
				{
					int x1 = range.xMin + x;
					int x2 = range.xMax - x - 1;
					T temp = this[x1, y];
					this[x1, y] = this[x2, y];
					this[x2, y] = temp;
				}
		}

		public sealed override void FlipVertical(RectInt range)
		{
			for (int x = range.xMin; x < range.xMax; x++)
				for (int y = 0; y < (range.height / 2); y++)
				{
					int y1 = range.yMin + y;
					int y2 = range.yMax - y - 1;
					T temp = this[x, y1];
					this[x, y1] = this[x, y2];
					this[x, y2] = temp;
				}
		}

		public sealed override void RemoveRowAt(int rowIndex)
		{
			if (Height <= 1)
				return;

			int indexToRemoveFrom = rowIndex * Width;
			fields.RemoveRange(indexToRemoveFrom, Width);
		}

		public sealed override void RemoveColumnAt(int columnIndex)
		{
			if (Width <= 1)
				return;

			for (int y = Height - 1; y >= 0; y--)
			{
				int indexToRemoveFrom = (y * Width) + columnIndex;
				fields.RemoveAt(indexToRemoveFrom);
			}

			width--;
		}

		public sealed override bool MoveArea(RectInt area, GeneralDirection2D direction)
		{
			switch (direction)
			{
				case GeneralDirection2D.Up:
					return MoveAreaUp(area);
				case GeneralDirection2D.Down:
					return MoveAreaDown(area);
				case GeneralDirection2D.Right:
					return MoveAreaRight(area);
				case GeneralDirection2D.Left:
					return MoveAreaLeft(area);
			}

			throw new Exception("Unreachable Code");
		}

		bool MoveAreaLeft(RectInt area)
		{
			if (area.xMin <= 0)
				return false;

			for (int y = area.yMin; y < area.yMax; y++)
			{
				T temp = this[area.xMin - 1, y];
				for (int x = area.xMin; x < area.xMax; x++)
					this[x - 1, y] = this[x, y];
				this[area.xMax - 1, y] = temp;
			}

			return true;
		}

		bool MoveAreaRight(RectInt area)
		{
			if (area.xMax >= Width)
				return false;

			for (int y = area.yMin; y < area.yMax; y++)
			{
				T temp = this[area.xMax, y];
				for (int x = area.xMax - 1; x >= area.xMin; x--)
					this[x + 1, y] = this[x, y];
				this[area.xMin, y] = temp;
			}

			return true;
		}

		bool MoveAreaUp(RectInt area)
		{
			if (area.yMin <= 0)
				return false;

			for (int x = area.xMin; x < area.xMax; x++)
			{
				T temp = this[x, area.yMin - 1];
				for (int y = area.yMin; y < area.yMax; y++)
					this[x, y - 1] = this[x, y];
				this[x, area.yMax - 1] = temp;
			}

			return true;
		}

		bool MoveAreaDown(RectInt area)
		{
			if (area.yMax >= Height)
				return false;

			for (int x = area.xMin; x < area.xMax; x++)
			{
				T temp = this[x, area.yMax];
				for (int y = area.yMax - 1; y >= area.yMin; y--)
					this[x, y + 1] = this[x, y];
				this[x, area.yMin] = temp;
			}

			return true;
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => fields.GetEnumerator();

		public sealed override IEnumerator GetEnumerator() => fields.GetEnumerator();
	}
}
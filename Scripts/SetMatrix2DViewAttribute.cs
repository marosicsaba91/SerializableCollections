using System;
using UnityEngine;

namespace Utility.SerializableCollection
{
	[AttributeUsage(
		AttributeTargets.Class |
		AttributeTargets.Struct |
		AttributeTargets.Enum |
		AttributeTargets.Field)]

	public sealed class SetMatrix2DViewAttribute : PropertyAttribute
	{
		public string[] DrawerTypeNames { get; }

		public SetMatrix2DViewAttribute(params string[] drawerTypeNames)
		{
			DrawerTypeNames = drawerTypeNames;
		}
	}
}
﻿using System;

namespace Utility.SerializableCollection
{
    public interface IGenericCollection
    {
        Type ContainingType { get; }
        int Count { get; }
    }
}
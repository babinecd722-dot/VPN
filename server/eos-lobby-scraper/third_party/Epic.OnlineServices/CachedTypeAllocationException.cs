using System;

namespace Epic.OnlineServices;

internal class CachedTypeAllocationException : AllocationException
{
	public CachedTypeAllocationException(IntPtr pointer, Type foundType, Type expectedType)
		: base(string.Format("Cached allocation is '{0}' but expected '{1}' at {2}", foundType, expectedType, pointer.ToString("X")))
	{
	}
}

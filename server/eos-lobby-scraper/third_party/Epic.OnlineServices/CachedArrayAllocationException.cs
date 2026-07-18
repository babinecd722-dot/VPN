using System;

namespace Epic.OnlineServices;

internal class CachedArrayAllocationException : AllocationException
{
	public CachedArrayAllocationException(IntPtr pointer, int foundLength, int expectedLength)
		: base(string.Format("Cached array allocation has length {0} but expected {1} at {2}", foundLength, expectedLength, pointer.ToString("X")))
	{
	}
}

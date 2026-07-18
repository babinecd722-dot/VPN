using System;

namespace Epic.OnlineServices;

internal class ExternalAllocationException : AllocationException
{
	public ExternalAllocationException(IntPtr pointer, Type type)
		: base(string.Format("Attempting to allocate '{0}' over externally allocated memory at {1}", type, pointer.ToString("X")))
	{
	}
}

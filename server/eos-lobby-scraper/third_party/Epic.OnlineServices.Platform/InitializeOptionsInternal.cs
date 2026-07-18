using System;

namespace Epic.OnlineServices.Platform;

internal struct InitializeOptionsInternal : ISettable<InitializeOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_AllocateMemoryFunction;

	private IntPtr m_ReallocateMemoryFunction;

	private IntPtr m_ReleaseMemoryFunction;

	private IntPtr m_ProductName;

	private IntPtr m_ProductVersion;

	private IntPtr m_Reserved;

	private IntPtr m_SystemInitializeOptions;

	private IntPtr m_OverrideThreadAffinity;

	public void Set(ref InitializeOptions other)
	{
		Dispose();
		m_ApiVersion = 4; // match libEOSSDK-Linux-Shipping 1.15.x
		m_AllocateMemoryFunction = other.AllocateMemoryFunction;
		m_ReallocateMemoryFunction = other.ReallocateMemoryFunction;
		m_ReleaseMemoryFunction = other.ReleaseMemoryFunction;
		Helper.Set(other.ProductName, ref m_ProductName);
		Helper.Set(other.ProductVersion, ref m_ProductVersion);
		m_Reserved = other.Reserved;
		if (m_Reserved == IntPtr.Zero)
		{
			Helper.Set(new int[2] { 1, 1 }, ref m_Reserved, isArrayItemAllocated: false);
		}
		m_SystemInitializeOptions = other.SystemInitializeOptions;
		Helper.Set<InitializeThreadAffinity, InitializeThreadAffinityInternal>(other.OverrideThreadAffinity, ref m_OverrideThreadAffinity);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ProductName);
		Helper.Dispose(ref m_ProductVersion);
		Helper.Dispose(ref m_Reserved);
		Helper.Dispose(ref m_SystemInitializeOptions);
		Helper.Dispose(ref m_OverrideThreadAffinity);
	}
}

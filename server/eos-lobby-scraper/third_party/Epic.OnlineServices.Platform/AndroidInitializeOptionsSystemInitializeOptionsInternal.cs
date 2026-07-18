using System;

namespace Epic.OnlineServices.Platform;

internal struct AndroidInitializeOptionsSystemInitializeOptionsInternal : ISettable<AndroidInitializeOptionsSystemInitializeOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Reserved;

	private IntPtr m_OptionalInternalDirectory;

	private IntPtr m_OptionalExternalDirectory;

	public void Set(ref AndroidInitializeOptionsSystemInitializeOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		m_Reserved = other.Reserved;
		Helper.Set(other.OptionalInternalDirectory, ref m_OptionalInternalDirectory);
		Helper.Set(other.OptionalExternalDirectory, ref m_OptionalExternalDirectory);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Reserved);
		Helper.Dispose(ref m_OptionalInternalDirectory);
		Helper.Dispose(ref m_OptionalExternalDirectory);
	}
}

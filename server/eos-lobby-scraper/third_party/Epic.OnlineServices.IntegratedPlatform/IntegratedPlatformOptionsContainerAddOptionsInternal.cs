using System;

namespace Epic.OnlineServices.IntegratedPlatform;

internal struct IntegratedPlatformOptionsContainerAddOptionsInternal : ISettable<IntegratedPlatformOptionsContainerAddOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Options;

	public void Set(ref IntegratedPlatformOptionsContainerAddOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set<Options, OptionsInternal>(other.Options, ref m_Options);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Options);
	}
}

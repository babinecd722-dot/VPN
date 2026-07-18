using System;

namespace Epic.OnlineServices.IntegratedPlatform;

internal struct OptionsInternal : ISettable<Options>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Type;

	private IntegratedPlatformManagementFlags m_Flags;

	private IntPtr m_InitOptions;

	public void Set(ref Options other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.Type, ref m_Type);
		m_Flags = other.Flags;
		m_InitOptions = other.InitOptions;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Type);
		Helper.Dispose(ref m_InitOptions);
	}
}

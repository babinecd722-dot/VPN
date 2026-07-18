using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct AddExternalIntegrityCatalogOptionsInternal : ISettable<AddExternalIntegrityCatalogOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_PathToBinFile;

	public void Set(ref AddExternalIntegrityCatalogOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.PathToBinFile, ref m_PathToBinFile);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_PathToBinFile);
	}
}

using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbySearchRemoveParameterOptionsInternal : ISettable<LobbySearchRemoveParameterOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Key;

	private ComparisonOp m_ComparisonOp;

	public void Set(ref LobbySearchRemoveParameterOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.Key, ref m_Key);
		m_ComparisonOp = other.ComparisonOp;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Key);
	}
}

using System;

namespace Epic.OnlineServices.Sessions;

internal struct UnregisterPlayersOptionsInternal : ISettable<UnregisterPlayersOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SessionName;

	private IntPtr m_PlayersToUnregister;

	private uint m_PlayersToUnregisterCount;

	public void Set(ref UnregisterPlayersOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set(other.SessionName, ref m_SessionName);
		Helper.Set(other.PlayersToUnregister, ref m_PlayersToUnregister, out m_PlayersToUnregisterCount, isArrayItemAllocated: false);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_SessionName);
		Helper.Dispose(ref m_PlayersToUnregister);
	}
}

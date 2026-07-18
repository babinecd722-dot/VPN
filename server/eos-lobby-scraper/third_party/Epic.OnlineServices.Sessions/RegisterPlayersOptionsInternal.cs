using System;

namespace Epic.OnlineServices.Sessions;

internal struct RegisterPlayersOptionsInternal : ISettable<RegisterPlayersOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SessionName;

	private IntPtr m_PlayersToRegister;

	private uint m_PlayersToRegisterCount;

	public void Set(ref RegisterPlayersOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		Helper.Set(other.SessionName, ref m_SessionName);
		Helper.Set(other.PlayersToRegister, ref m_PlayersToRegister, out m_PlayersToRegisterCount, isArrayItemAllocated: false);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_SessionName);
		Helper.Dispose(ref m_PlayersToRegister);
	}
}

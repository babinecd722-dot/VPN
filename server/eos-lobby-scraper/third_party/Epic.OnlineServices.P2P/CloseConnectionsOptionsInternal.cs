using System;

namespace Epic.OnlineServices.P2P;

internal struct CloseConnectionsOptionsInternal : ISettable<CloseConnectionsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_SocketId;

	public void Set(ref CloseConnectionsOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set<SocketId, SocketIdInternal>(other.SocketId, ref m_SocketId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_SocketId);
	}
}

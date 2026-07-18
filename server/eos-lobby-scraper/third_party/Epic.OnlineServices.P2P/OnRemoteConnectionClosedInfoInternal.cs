using System;

namespace Epic.OnlineServices.P2P;

internal struct OnRemoteConnectionClosedInfoInternal : ICallbackInfoInternal, IGettable<OnRemoteConnectionClosedInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RemoteUserId;

	private IntPtr m_SocketId;

	private ConnectionClosedReason m_Reason;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnRemoteConnectionClosedInfo other)
	{
		other = default(OnRemoteConnectionClosedInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_RemoteUserId, out ProductUserId to3);
		other.RemoteUserId = to3;
		Helper.Get<SocketIdInternal, SocketId>(m_SocketId, out SocketId? to4);
		other.SocketId = to4;
		other.Reason = m_Reason;
	}
}

using System;
using Epic.OnlineServices.AntiCheatCommon;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct RegisterPeerOptionsInternal : ISettable<RegisterPeerOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_PeerHandle;

	private AntiCheatCommonClientType m_ClientType;

	private AntiCheatCommonClientPlatform m_ClientPlatform;

	private uint m_AuthenticationTimeout;

	private IntPtr m_AccountId_DEPRECATED;

	private IntPtr m_IpAddress;

	private IntPtr m_PeerProductUserId;

	public void Set(ref RegisterPeerOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		m_PeerHandle = other.PeerHandle;
		m_ClientType = other.ClientType;
		m_ClientPlatform = other.ClientPlatform;
		m_AuthenticationTimeout = other.AuthenticationTimeout;
		Helper.Set(other.AccountId_DEPRECATED, ref m_AccountId_DEPRECATED);
		Helper.Set(other.IpAddress, ref m_IpAddress);
		Helper.Set((Handle)other.PeerProductUserId, ref m_PeerProductUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_AccountId_DEPRECATED);
		Helper.Dispose(ref m_IpAddress);
		Helper.Dispose(ref m_PeerProductUserId);
	}
}

using System;
using Epic.OnlineServices.AntiCheatCommon;

namespace Epic.OnlineServices.AntiCheatServer;

internal struct RegisterClientOptionsInternal : ISettable<RegisterClientOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_ClientHandle;

	private AntiCheatCommonClientType m_ClientType;

	private AntiCheatCommonClientPlatform m_ClientPlatform;

	private IntPtr m_AccountId_DEPRECATED;

	private IntPtr m_IpAddress;

	private IntPtr m_UserId;

	private int m_Reserved01;

	public void Set(ref RegisterClientOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		m_ClientHandle = other.ClientHandle;
		m_ClientType = other.ClientType;
		m_ClientPlatform = other.ClientPlatform;
		Helper.Set(other.AccountId_DEPRECATED, ref m_AccountId_DEPRECATED);
		Helper.Set(other.IpAddress, ref m_IpAddress);
		Helper.Set((Handle)other.UserId, ref m_UserId);
		m_Reserved01 = other.Reserved01;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_AccountId_DEPRECATED);
		Helper.Dispose(ref m_IpAddress);
		Helper.Dispose(ref m_UserId);
	}
}

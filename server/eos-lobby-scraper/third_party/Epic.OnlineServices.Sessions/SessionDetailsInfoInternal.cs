using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionDetailsInfoInternal : IGettable<SessionDetailsInfo>
{
	private int m_ApiVersion;

	private IntPtr m_SessionId;

	private IntPtr m_HostAddress;

	private uint m_NumOpenPublicConnections;

	private IntPtr m_Settings;

	private IntPtr m_OwnerUserId;

	private IntPtr m_OwnerServerClientId;

	public void Get(out SessionDetailsInfo other)
	{
		other = default(SessionDetailsInfo);
		Helper.Get(m_SessionId, out Utf8String to);
		other.SessionId = to;
		Helper.Get(m_HostAddress, out Utf8String to2);
		other.HostAddress = to2;
		other.NumOpenPublicConnections = m_NumOpenPublicConnections;
		Helper.Get<SessionDetailsSettingsInternal, SessionDetailsSettings>(m_Settings, out SessionDetailsSettings? to3);
		other.Settings = to3;
		Helper.Get(m_OwnerUserId, out ProductUserId to4);
		other.OwnerUserId = to4;
		Helper.Get(m_OwnerServerClientId, out Utf8String to5);
		other.OwnerServerClientId = to5;
	}
}

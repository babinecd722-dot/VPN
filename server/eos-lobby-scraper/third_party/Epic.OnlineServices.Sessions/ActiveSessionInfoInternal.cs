using System;

namespace Epic.OnlineServices.Sessions;

internal struct ActiveSessionInfoInternal : IGettable<ActiveSessionInfo>
{
	private int m_ApiVersion;

	private IntPtr m_SessionName;

	private IntPtr m_LocalUserId;

	private OnlineSessionState m_State;

	private IntPtr m_SessionDetails;

	public void Get(out ActiveSessionInfo other)
	{
		other = default(ActiveSessionInfo);
		Helper.Get(m_SessionName, out Utf8String to);
		other.SessionName = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		other.State = m_State;
		Helper.Get<SessionDetailsInfoInternal, SessionDetailsInfo>(m_SessionDetails, out SessionDetailsInfo? to3);
		other.SessionDetails = to3;
	}
}

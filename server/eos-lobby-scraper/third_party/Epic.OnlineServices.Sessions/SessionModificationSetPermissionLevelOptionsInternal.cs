using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionModificationSetPermissionLevelOptionsInternal : ISettable<SessionModificationSetPermissionLevelOptions>, IDisposable
{
	private int m_ApiVersion;

	private OnlineSessionPermissionLevel m_PermissionLevel;

	public void Set(ref SessionModificationSetPermissionLevelOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_PermissionLevel = other.PermissionLevel;
	}

	public void Dispose()
	{
	}
}

using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionModificationSetJoinInProgressAllowedOptionsInternal : ISettable<SessionModificationSetJoinInProgressAllowedOptions>, IDisposable
{
	private int m_ApiVersion;

	private int m_AllowJoinInProgress;

	public void Set(ref SessionModificationSetJoinInProgressAllowedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.AllowJoinInProgress, ref m_AllowJoinInProgress);
	}

	public void Dispose()
	{
	}
}

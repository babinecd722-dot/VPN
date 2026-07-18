using System;

namespace Epic.OnlineServices.Sessions;

internal struct AddNotifyLeaveSessionRequestedOptionsInternal : ISettable<AddNotifyLeaveSessionRequestedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyLeaveSessionRequestedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}

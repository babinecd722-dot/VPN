using System;

namespace Epic.OnlineServices.Sessions;

internal struct AddNotifyJoinSessionAcceptedOptionsInternal : ISettable<AddNotifyJoinSessionAcceptedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyJoinSessionAcceptedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}

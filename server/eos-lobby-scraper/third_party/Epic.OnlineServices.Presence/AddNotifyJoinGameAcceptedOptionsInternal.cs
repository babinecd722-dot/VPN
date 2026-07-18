using System;

namespace Epic.OnlineServices.Presence;

internal struct AddNotifyJoinGameAcceptedOptionsInternal : ISettable<AddNotifyJoinGameAcceptedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyJoinGameAcceptedOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
	}

	public void Dispose()
	{
	}
}

using System;

namespace Epic.OnlineServices.UI;

internal struct SetToggleFriendsKeyOptionsInternal : ISettable<SetToggleFriendsKeyOptions>, IDisposable
{
	private int m_ApiVersion;

	private KeyCombination m_KeyCombination;

	public void Set(ref SetToggleFriendsKeyOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_KeyCombination = other.KeyCombination;
	}

	public void Dispose()
	{
	}
}

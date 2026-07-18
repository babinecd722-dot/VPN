using System;

namespace Epic.OnlineServices.UI;

internal struct SetToggleFriendsButtonOptionsInternal : ISettable<SetToggleFriendsButtonOptions>, IDisposable
{
	private int m_ApiVersion;

	private InputStateButtonFlags m_ButtonCombination;

	public void Set(ref SetToggleFriendsButtonOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_ButtonCombination = other.ButtonCombination;
	}

	public void Dispose()
	{
	}
}

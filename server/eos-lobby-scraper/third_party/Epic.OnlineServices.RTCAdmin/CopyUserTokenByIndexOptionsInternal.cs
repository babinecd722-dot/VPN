using System;

namespace Epic.OnlineServices.RTCAdmin;

internal struct CopyUserTokenByIndexOptionsInternal : ISettable<CopyUserTokenByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_UserTokenIndex;

	private uint m_QueryId;

	public void Set(ref CopyUserTokenByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		m_UserTokenIndex = other.UserTokenIndex;
		m_QueryId = other.QueryId;
	}

	public void Dispose()
	{
	}
}

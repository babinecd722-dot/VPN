using System;

namespace Epic.OnlineServices.UserInfo;

internal struct QueryUserInfoByDisplayNameOptionsInternal : ISettable<QueryUserInfoByDisplayNameOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_DisplayName;

	public void Set(ref QueryUserInfoByDisplayNameOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.DisplayName, ref m_DisplayName);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_DisplayName);
	}
}

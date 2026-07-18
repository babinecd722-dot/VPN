using System;

namespace Epic.OnlineServices.RTCAdmin;

internal struct UserTokenInternal : IGettable<UserToken>
{
	private int m_ApiVersion;

	private IntPtr m_ProductUserId;

	private IntPtr m_Token;

	public void Get(out UserToken other)
	{
		other = default(UserToken);
		Helper.Get(m_ProductUserId, out ProductUserId to);
		other.ProductUserId = to;
		Helper.Get(m_Token, out Utf8String to2);
		other.Token = to2;
	}
}

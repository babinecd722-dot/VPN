using System;

namespace Epic.OnlineServices.UserInfo;

internal struct BestDisplayNameInternal : IGettable<BestDisplayName>
{
	private int m_ApiVersion;

	private IntPtr m_UserId;

	private IntPtr m_DisplayName;

	private IntPtr m_DisplayNameSanitized;

	private IntPtr m_Nickname;

	private uint m_PlatformType;

	public void Get(out BestDisplayName other)
	{
		other = default(BestDisplayName);
		Helper.Get(m_UserId, out EpicAccountId to);
		other.UserId = to;
		Helper.Get(m_DisplayName, out Utf8String to2);
		other.DisplayName = to2;
		Helper.Get(m_DisplayNameSanitized, out Utf8String to3);
		other.DisplayNameSanitized = to3;
		Helper.Get(m_Nickname, out Utf8String to4);
		other.Nickname = to4;
		other.PlatformType = m_PlatformType;
	}
}

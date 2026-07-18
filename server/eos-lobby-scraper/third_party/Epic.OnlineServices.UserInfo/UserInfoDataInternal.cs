using System;

namespace Epic.OnlineServices.UserInfo;

internal struct UserInfoDataInternal : IGettable<UserInfoData>
{
	private int m_ApiVersion;

	private IntPtr m_UserId;

	private IntPtr m_Country;

	private IntPtr m_DisplayName;

	private IntPtr m_PreferredLanguage;

	private IntPtr m_Nickname;

	private IntPtr m_DisplayNameSanitized;

	public void Get(out UserInfoData other)
	{
		other = default(UserInfoData);
		Helper.Get(m_UserId, out EpicAccountId to);
		other.UserId = to;
		Helper.Get(m_Country, out Utf8String to2);
		other.Country = to2;
		Helper.Get(m_DisplayName, out Utf8String to3);
		other.DisplayName = to3;
		Helper.Get(m_PreferredLanguage, out Utf8String to4);
		other.PreferredLanguage = to4;
		Helper.Get(m_Nickname, out Utf8String to5);
		other.Nickname = to5;
		Helper.Get(m_DisplayNameSanitized, out Utf8String to6);
		other.DisplayNameSanitized = to6;
	}
}

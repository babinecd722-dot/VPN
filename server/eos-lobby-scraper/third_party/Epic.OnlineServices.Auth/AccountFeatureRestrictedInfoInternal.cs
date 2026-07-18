using System;

namespace Epic.OnlineServices.Auth;

internal struct AccountFeatureRestrictedInfoInternal : IGettable<AccountFeatureRestrictedInfo>
{
	private int m_ApiVersion;

	private IntPtr m_VerificationURI;

	public void Get(out AccountFeatureRestrictedInfo other)
	{
		other = default(AccountFeatureRestrictedInfo);
		Helper.Get(m_VerificationURI, out Utf8String to);
		other.VerificationURI = to;
	}
}

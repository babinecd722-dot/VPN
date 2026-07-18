using System;

namespace Epic.OnlineServices.Auth;

internal struct PinGrantInfoInternal : IGettable<PinGrantInfo>
{
	private int m_ApiVersion;

	private IntPtr m_UserCode;

	private IntPtr m_VerificationURI;

	private int m_ExpiresIn;

	private IntPtr m_VerificationURIComplete;

	public void Get(out PinGrantInfo other)
	{
		other = default(PinGrantInfo);
		Helper.Get(m_UserCode, out Utf8String to);
		other.UserCode = to;
		Helper.Get(m_VerificationURI, out Utf8String to2);
		other.VerificationURI = to2;
		other.ExpiresIn = m_ExpiresIn;
		Helper.Get(m_VerificationURIComplete, out Utf8String to3);
		other.VerificationURIComplete = to3;
	}
}

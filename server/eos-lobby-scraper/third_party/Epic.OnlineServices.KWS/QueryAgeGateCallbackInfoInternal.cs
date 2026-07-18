using System;

namespace Epic.OnlineServices.KWS;

internal struct QueryAgeGateCallbackInfoInternal : ICallbackInfoInternal, IGettable<QueryAgeGateCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_CountryCode;

	private uint m_AgeOfConsent;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out QueryAgeGateCallbackInfo other)
	{
		other = default(QueryAgeGateCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_CountryCode, out Utf8String to2);
		other.CountryCode = to2;
		other.AgeOfConsent = m_AgeOfConsent;
	}
}

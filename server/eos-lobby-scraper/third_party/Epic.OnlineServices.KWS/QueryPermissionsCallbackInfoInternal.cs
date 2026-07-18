using System;

namespace Epic.OnlineServices.KWS;

internal struct QueryPermissionsCallbackInfoInternal : ICallbackInfoInternal, IGettable<QueryPermissionsCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_KWSUserId;

	private IntPtr m_DateOfBirth;

	private int m_IsMinor;

	private IntPtr m_ParentEmail;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out QueryPermissionsCallbackInfo other)
	{
		other = default(QueryPermissionsCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_KWSUserId, out Utf8String to3);
		other.KWSUserId = to3;
		Helper.Get(m_DateOfBirth, out Utf8String to4);
		other.DateOfBirth = to4;
		Helper.Get(m_IsMinor, out bool to5);
		other.IsMinor = to5;
		Helper.Get(m_ParentEmail, out Utf8String to6);
		other.ParentEmail = to6;
	}
}

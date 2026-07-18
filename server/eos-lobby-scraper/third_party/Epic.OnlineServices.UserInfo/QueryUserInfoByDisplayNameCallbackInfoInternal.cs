using System;

namespace Epic.OnlineServices.UserInfo;

internal struct QueryUserInfoByDisplayNameCallbackInfoInternal : ICallbackInfoInternal, IGettable<QueryUserInfoByDisplayNameCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	private IntPtr m_DisplayName;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out QueryUserInfoByDisplayNameCallbackInfo other)
	{
		other = default(QueryUserInfoByDisplayNameCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		Helper.Get(m_TargetUserId, out EpicAccountId to3);
		other.TargetUserId = to3;
		Helper.Get(m_DisplayName, out Utf8String to4);
		other.DisplayName = to4;
	}
}

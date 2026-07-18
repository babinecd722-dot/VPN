using System;

namespace Epic.OnlineServices.Sessions;

internal struct SendSessionNativeInviteRequestedCallbackInfoInternal : ICallbackInfoInternal, IGettable<SendSessionNativeInviteRequestedCallbackInfo>
{
	private IntPtr m_ClientData;

	private ulong m_UiEventId;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetNativeAccountType;

	private IntPtr m_TargetUserNativeAccountId;

	private IntPtr m_SessionId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out SendSessionNativeInviteRequestedCallbackInfo other)
	{
		other = default(SendSessionNativeInviteRequestedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		other.UiEventId = m_UiEventId;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_TargetNativeAccountType, out Utf8String to3);
		other.TargetNativeAccountType = to3;
		Helper.Get(m_TargetUserNativeAccountId, out Utf8String to4);
		other.TargetUserNativeAccountId = to4;
		Helper.Get(m_SessionId, out Utf8String to5);
		other.SessionId = to5;
	}
}

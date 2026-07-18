using System;

namespace Epic.OnlineServices.CustomInvites;

internal struct OnCustomInviteAcceptedCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnCustomInviteAcceptedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_TargetUserId;

	private IntPtr m_LocalUserId;

	private IntPtr m_CustomInviteId;

	private IntPtr m_Payload;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnCustomInviteAcceptedCallbackInfo other)
	{
		other = default(OnCustomInviteAcceptedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_TargetUserId, out ProductUserId to2);
		other.TargetUserId = to2;
		Helper.Get(m_LocalUserId, out ProductUserId to3);
		other.LocalUserId = to3;
		Helper.Get(m_CustomInviteId, out Utf8String to4);
		other.CustomInviteId = to4;
		Helper.Get(m_Payload, out Utf8String to5);
		other.Payload = to5;
	}
}

using System;

namespace Epic.OnlineServices.Presence;

internal struct JoinGameAcceptedCallbackInfoInternal : ICallbackInfoInternal, IGettable<JoinGameAcceptedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_JoinInfo;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	private ulong m_UiEventId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out JoinGameAcceptedCallbackInfo other)
	{
		other = default(JoinGameAcceptedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_JoinInfo, out Utf8String to2);
		other.JoinInfo = to2;
		Helper.Get(m_LocalUserId, out EpicAccountId to3);
		other.LocalUserId = to3;
		Helper.Get(m_TargetUserId, out EpicAccountId to4);
		other.TargetUserId = to4;
		other.UiEventId = m_UiEventId;
	}
}

using System;

namespace Epic.OnlineServices.Sanctions;

internal struct CreatePlayerSanctionAppealCallbackInfoInternal : ICallbackInfoInternal, IGettable<CreatePlayerSanctionAppealCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_ReferenceId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out CreatePlayerSanctionAppealCallbackInfo other)
	{
		other = default(CreatePlayerSanctionAppealCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_ReferenceId, out Utf8String to2);
		other.ReferenceId = to2;
	}
}

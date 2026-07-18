using System;

namespace Epic.OnlineServices.Sessions;

internal struct UpdateSessionCallbackInfoInternal : ICallbackInfoInternal, IGettable<UpdateSessionCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_SessionName;

	private IntPtr m_SessionId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out UpdateSessionCallbackInfo other)
	{
		other = default(UpdateSessionCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_SessionName, out Utf8String to2);
		other.SessionName = to2;
		Helper.Get(m_SessionId, out Utf8String to3);
		other.SessionId = to3;
	}
}

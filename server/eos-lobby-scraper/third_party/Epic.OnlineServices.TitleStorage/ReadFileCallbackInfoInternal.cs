using System;

namespace Epic.OnlineServices.TitleStorage;

internal struct ReadFileCallbackInfoInternal : ICallbackInfoInternal, IGettable<ReadFileCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_Filename;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out ReadFileCallbackInfo other)
	{
		other = default(ReadFileCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_Filename, out Utf8String to3);
		other.Filename = to3;
	}
}

using System;

namespace Epic.OnlineServices.PlayerDataStorage;

internal struct WriteFileDataCallbackInfoInternal : ICallbackInfoInternal, IGettable<WriteFileDataCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_Filename;

	private uint m_DataBufferLengthBytes;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out WriteFileDataCallbackInfo other)
	{
		other = default(WriteFileDataCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_Filename, out Utf8String to3);
		other.Filename = to3;
		other.DataBufferLengthBytes = m_DataBufferLengthBytes;
	}
}

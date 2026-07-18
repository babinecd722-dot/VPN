using System;

namespace Epic.OnlineServices.PlayerDataStorage;

internal struct FileTransferProgressCallbackInfoInternal : ICallbackInfoInternal, IGettable<FileTransferProgressCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_Filename;

	private uint m_BytesTransferred;

	private uint m_TotalFileSizeBytes;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out FileTransferProgressCallbackInfo other)
	{
		other = default(FileTransferProgressCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_Filename, out Utf8String to3);
		other.Filename = to3;
		other.BytesTransferred = m_BytesTransferred;
		other.TotalFileSizeBytes = m_TotalFileSizeBytes;
	}
}

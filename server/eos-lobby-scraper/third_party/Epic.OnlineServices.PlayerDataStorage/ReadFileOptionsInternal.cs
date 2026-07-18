using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.PlayerDataStorage;

internal struct ReadFileOptionsInternal : ISettable<ReadFileOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_Filename;

	private uint m_ReadChunkLengthBytes;

	private IntPtr m_ReadFileDataCallback;

	private IntPtr m_FileTransferProgressCallback;

	public void Set(ref ReadFileOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.Filename, ref m_Filename);
		m_ReadChunkLengthBytes = other.ReadChunkLengthBytes;
		m_ReadFileDataCallback = ((other.ReadFileDataCallback != null) ? Marshal.GetFunctionPointerForDelegate(OnReadFileDataCallbackInternalImplementation.Delegate) : IntPtr.Zero);
		m_FileTransferProgressCallback = ((other.FileTransferProgressCallback != null) ? Marshal.GetFunctionPointerForDelegate(OnFileTransferProgressCallbackInternalImplementation.Delegate) : IntPtr.Zero);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_Filename);
	}
}

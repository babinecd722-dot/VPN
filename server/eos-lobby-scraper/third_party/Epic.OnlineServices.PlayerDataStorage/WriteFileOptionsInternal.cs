using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.PlayerDataStorage;

internal struct WriteFileOptionsInternal : ISettable<WriteFileOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_Filename;

	private uint m_ChunkLengthBytes;

	private IntPtr m_WriteFileDataCallback;

	private IntPtr m_FileTransferProgressCallback;

	public void Set(ref WriteFileOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.Filename, ref m_Filename);
		m_ChunkLengthBytes = other.ChunkLengthBytes;
		m_WriteFileDataCallback = ((other.WriteFileDataCallback != null) ? Marshal.GetFunctionPointerForDelegate(OnWriteFileDataCallbackInternalImplementation.Delegate) : IntPtr.Zero);
		m_FileTransferProgressCallback = ((other.FileTransferProgressCallback != null) ? Marshal.GetFunctionPointerForDelegate(OnFileTransferProgressCallbackInternalImplementation.Delegate) : IntPtr.Zero);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_Filename);
	}
}

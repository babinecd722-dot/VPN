using System;

namespace Epic.OnlineServices.PlayerDataStorage;

internal struct DuplicateFileOptionsInternal : ISettable<DuplicateFileOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_SourceFilename;

	private IntPtr m_DestinationFilename;

	public void Set(ref DuplicateFileOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.SourceFilename, ref m_SourceFilename);
		Helper.Set(other.DestinationFilename, ref m_DestinationFilename);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_SourceFilename);
		Helper.Dispose(ref m_DestinationFilename);
	}
}

using System;

namespace Epic.OnlineServices.Sessions;

internal struct UpdateSessionOptionsInternal : ISettable<UpdateSessionOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SessionModificationHandle;

	public void Set(ref UpdateSessionOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.SessionModificationHandle, ref m_SessionModificationHandle);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_SessionModificationHandle);
	}
}

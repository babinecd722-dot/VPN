using System;

namespace Epic.OnlineServices.Sessions;

internal struct UpdateSessionModificationOptionsInternal : ISettable<UpdateSessionModificationOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SessionName;

	public void Set(ref UpdateSessionModificationOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.SessionName, ref m_SessionName);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_SessionName);
	}
}

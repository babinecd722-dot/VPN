using System;

namespace Epic.OnlineServices.KWS;

internal struct UpdateParentEmailOptionsInternal : ISettable<UpdateParentEmailOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_ParentEmail;

	public void Set(ref UpdateParentEmailOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.ParentEmail, ref m_ParentEmail);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_ParentEmail);
	}
}

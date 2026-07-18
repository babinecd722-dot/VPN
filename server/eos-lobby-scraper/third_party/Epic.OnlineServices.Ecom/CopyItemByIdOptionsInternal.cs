using System;

namespace Epic.OnlineServices.Ecom;

internal struct CopyItemByIdOptionsInternal : ISettable<CopyItemByIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_ItemId;

	public void Set(ref CopyItemByIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.ItemId, ref m_ItemId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_ItemId);
	}
}

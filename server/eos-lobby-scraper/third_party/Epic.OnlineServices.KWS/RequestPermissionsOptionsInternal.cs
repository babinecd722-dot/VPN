using System;

namespace Epic.OnlineServices.KWS;

internal struct RequestPermissionsOptionsInternal : ISettable<RequestPermissionsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private uint m_PermissionKeyCount;

	private IntPtr m_PermissionKeys;

	public void Set(ref RequestPermissionsOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.PermissionKeys, ref m_PermissionKeys, out m_PermissionKeyCount, isArrayItemAllocated: true);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_PermissionKeys);
	}
}

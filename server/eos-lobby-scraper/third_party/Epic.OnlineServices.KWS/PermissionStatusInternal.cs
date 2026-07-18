using System;

namespace Epic.OnlineServices.KWS;

internal struct PermissionStatusInternal : IGettable<PermissionStatus>
{
	private int m_ApiVersion;

	private IntPtr m_Name;

	private KWSPermissionStatus m_Status;

	public void Get(out PermissionStatus other)
	{
		other = default(PermissionStatus);
		Helper.Get(m_Name, out Utf8String to);
		other.Name = to;
		other.Status = m_Status;
	}
}

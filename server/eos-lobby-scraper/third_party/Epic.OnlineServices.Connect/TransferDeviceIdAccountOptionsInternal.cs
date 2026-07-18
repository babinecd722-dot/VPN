using System;

namespace Epic.OnlineServices.Connect;

internal struct TransferDeviceIdAccountOptionsInternal : ISettable<TransferDeviceIdAccountOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_PrimaryLocalUserId;

	private IntPtr m_LocalDeviceUserId;

	private IntPtr m_ProductUserIdToPreserve;

	public void Set(ref TransferDeviceIdAccountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.PrimaryLocalUserId, ref m_PrimaryLocalUserId);
		Helper.Set((Handle)other.LocalDeviceUserId, ref m_LocalDeviceUserId);
		Helper.Set((Handle)other.ProductUserIdToPreserve, ref m_ProductUserIdToPreserve);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_PrimaryLocalUserId);
		Helper.Dispose(ref m_LocalDeviceUserId);
		Helper.Dispose(ref m_ProductUserIdToPreserve);
	}
}

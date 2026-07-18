using System;

namespace Epic.OnlineServices.Connect;

internal struct IdTokenInternal : IGettable<IdToken>, ISettable<IdToken>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_ProductUserId;

	private IntPtr m_JsonWebToken;

	public void Get(out IdToken other)
	{
		other = default(IdToken);
		Helper.Get(m_ProductUserId, out ProductUserId to);
		other.ProductUserId = to;
		Helper.Get(m_JsonWebToken, out Utf8String to2);
		other.JsonWebToken = to2;
	}

	public void Set(ref IdToken other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.ProductUserId, ref m_ProductUserId);
		Helper.Set(other.JsonWebToken, ref m_JsonWebToken);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ProductUserId);
		Helper.Dispose(ref m_JsonWebToken);
	}
}

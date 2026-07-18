using System;

namespace Epic.OnlineServices.Metrics;

internal struct BeginPlayerSessionOptionsInternal : ISettable<BeginPlayerSessionOptions>, IDisposable
{
	private int m_ApiVersion;

	private MetricsAccountIdType m_AccountIdType;

	private BeginPlayerSessionOptionsAccountIdInternal m_AccountId;

	private IntPtr m_DisplayName;

	private UserControllerType m_ControllerType;

	private IntPtr m_ServerIp;

	private IntPtr m_GameSessionId;

	public void Set(ref BeginPlayerSessionOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set<BeginPlayerSessionOptionsAccountId, BeginPlayerSessionOptionsAccountIdInternal>(other.AccountId, ref m_AccountId);
		m_AccountIdType = other.AccountId.AccountIdType;
		Helper.Set(other.DisplayName, ref m_DisplayName);
		m_ControllerType = other.ControllerType;
		Helper.Set(other.ServerIp, ref m_ServerIp);
		Helper.Set(other.GameSessionId, ref m_GameSessionId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_AccountId);
		Helper.Dispose(ref m_DisplayName);
		Helper.Dispose(ref m_ServerIp);
		Helper.Dispose(ref m_GameSessionId);
	}
}

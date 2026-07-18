using System;

namespace Epic.OnlineServices.Platform;

internal struct OptionsInternal : ISettable<Options>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Reserved;

	private IntPtr m_ProductId;

	private IntPtr m_SandboxId;

	private ClientCredentialsInternal m_ClientCredentials;

	private int m_IsServer;

	private IntPtr m_EncryptionKey;

	private IntPtr m_OverrideCountryCode;

	private IntPtr m_OverrideLocaleCode;

	private IntPtr m_DeploymentId;

	private PlatformFlags m_Flags;

	private IntPtr m_CacheDirectory;

	private uint m_TickBudgetInMilliseconds;

	private IntPtr m_RTCOptions;

	private IntPtr m_IntegratedPlatformOptionsContainerHandle;

	private IntPtr m_SystemSpecificOptions;

	private IntPtr m_TaskNetworkTimeoutSeconds;

	public void Set(ref Options other)
	{
		Dispose();
		m_ApiVersion = 11; // match libEOSSDK-Linux-Shipping 1.15.x
		m_Reserved = other.Reserved;
		Helper.Set(other.ProductId, ref m_ProductId);
		Helper.Set(other.SandboxId, ref m_SandboxId);
		Helper.Set<ClientCredentials, ClientCredentialsInternal>(other.ClientCredentials, ref m_ClientCredentials);
		Helper.Set(other.IsServer, ref m_IsServer);
		Helper.Set(other.EncryptionKey, ref m_EncryptionKey);
		Helper.Set(other.OverrideCountryCode, ref m_OverrideCountryCode);
		Helper.Set(other.OverrideLocaleCode, ref m_OverrideLocaleCode);
		Helper.Set(other.DeploymentId, ref m_DeploymentId);
		m_Flags = other.Flags;
		Helper.Set(other.CacheDirectory, ref m_CacheDirectory);
		m_TickBudgetInMilliseconds = other.TickBudgetInMilliseconds;
		Helper.Set<RTCOptions, RTCOptionsInternal>(other.RTCOptions, ref m_RTCOptions);
		Helper.Set((Handle)other.IntegratedPlatformOptionsContainerHandle, ref m_IntegratedPlatformOptionsContainerHandle);
		m_SystemSpecificOptions = other.SystemSpecificOptions;
		Helper.Set(other.TaskNetworkTimeoutSeconds, ref m_TaskNetworkTimeoutSeconds);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Reserved);
		Helper.Dispose(ref m_ProductId);
		Helper.Dispose(ref m_SandboxId);
		Helper.Dispose(ref m_ClientCredentials);
		Helper.Dispose(ref m_EncryptionKey);
		Helper.Dispose(ref m_OverrideCountryCode);
		Helper.Dispose(ref m_OverrideLocaleCode);
		Helper.Dispose(ref m_DeploymentId);
		Helper.Dispose(ref m_CacheDirectory);
		Helper.Dispose(ref m_RTCOptions);
		Helper.Dispose(ref m_IntegratedPlatformOptionsContainerHandle);
		Helper.Dispose(ref m_SystemSpecificOptions);
		Helper.Dispose(ref m_TaskNetworkTimeoutSeconds);
	}
}

using System;

namespace Epic.OnlineServices.Ecom;

internal struct SandboxIdItemOwnershipInternal : IGettable<SandboxIdItemOwnership>
{
	private IntPtr m_SandboxId;

	private IntPtr m_OwnedCatalogItemIds;

	private uint m_OwnedCatalogItemIdsCount;

	public void Get(out SandboxIdItemOwnership other)
	{
		other = default(SandboxIdItemOwnership);
		Helper.Get(m_SandboxId, out Utf8String to);
		other.SandboxId = to;
		Helper.Get(m_OwnedCatalogItemIds, out var to2, m_OwnedCatalogItemIdsCount, isArrayItemAllocated: true);
		other.OwnedCatalogItemIds = to2;
	}
}

using System;

namespace Epic.OnlineServices.Ecom;

public sealed class EcomInterface : Handle
{
	public const int CATALOGITEMID_MAX_LENGTH = 32;

	public const int CATALOGITEM_API_LATEST = 1;

	public const int CATALOGITEM_ENTITLEMENTENDTIMESTAMP_UNDEFINED = -1;

	public const int CATALOGOFFERID_MAX_LENGTH = 32;

	public const int CATALOGOFFER_API_LATEST = 5;

	public const int CATALOGOFFER_EFFECTIVEDATETIMESTAMP_UNDEFINED = -1;

	public const int CATALOGOFFER_EXPIRATIONTIMESTAMP_UNDEFINED = -1;

	public const int CATALOGOFFER_RELEASEDATETIMESTAMP_UNDEFINED = -1;

	public const int CATALOGRELEASE_API_LATEST = 1;

	public const int CHECKOUTENTRY_API_LATEST = 1;

	public const int CHECKOUT_API_LATEST = 2;

	public const int CHECKOUT_MAX_ENTRIES = 10;

	public const int COPYENTITLEMENTBYID_API_LATEST = 2;

	public const int COPYENTITLEMENTBYINDEX_API_LATEST = 1;

	public const int COPYENTITLEMENTBYNAMEANDINDEX_API_LATEST = 1;

	public const int COPYITEMBYID_API_LATEST = 1;

	public const int COPYITEMIMAGEINFOBYINDEX_API_LATEST = 1;

	public const int COPYITEMRELEASEBYINDEX_API_LATEST = 1;

	public const int COPYLASTREDEEMEDENTITLEMENTBYINDEX_API_LATEST = 1;

	public const int COPYLASTREDEEMENTITLEMENTSRESULTBYINDEX_API_LATEST = 1;

	public const int COPYOFFERBYID_API_LATEST = 3;

	public const int COPYOFFERBYINDEX_API_LATEST = 3;

	public const int COPYOFFERIMAGEINFOBYINDEX_API_LATEST = 1;

	public const int COPYOFFERITEMBYINDEX_API_LATEST = 1;

	public const int COPYTRANSACTIONBYID_API_LATEST = 1;

	public const int COPYTRANSACTIONBYINDEX_API_LATEST = 1;

	public const int ENTITLEMENTID_MAX_LENGTH = 32;

	public const int ENTITLEMENT_API_LATEST = 2;

	public const int ENTITLEMENT_ENDTIMESTAMP_UNDEFINED = -1;

	public const int GETENTITLEMENTSBYNAMECOUNT_API_LATEST = 1;

	public const int GETENTITLEMENTSCOUNT_API_LATEST = 1;

	public const int GETITEMIMAGEINFOCOUNT_API_LATEST = 1;

	public const int GETITEMRELEASECOUNT_API_LATEST = 1;

	public const int GETLASTREDEEMEDENTITLEMENTSCOUNT_API_LATEST = 1;

	public const int GETLASTREDEEMENTITLEMENTSRESULTCOUNT_API_LATEST = 1;

	public const int GETOFFERCOUNT_API_LATEST = 1;

	public const int GETOFFERIMAGEINFOCOUNT_API_LATEST = 1;

	public const int GETOFFERITEMCOUNT_API_LATEST = 1;

	public const int GETTRANSACTIONCOUNT_API_LATEST = 1;

	public const int ITEMOWNERSHIP_API_LATEST = 1;

	public const int KEYIMAGEINFO_API_LATEST = 1;

	public const int QUERYENTITLEMENTS_API_LATEST = 3;

	public const int QUERYENTITLEMENTS_MAX_ENTITLEMENT_IDS = 256;

	public const int QUERYENTITLEMENTTOKEN_API_LATEST = 1;

	public const int QUERYENTITLEMENTTOKEN_MAX_ENTITLEMENT_IDS = 32;

	public const int QUERYOFFERS_API_LATEST = 1;

	public const int QUERYOWNERSHIPBYSANDBOXIDSOPTIONS_API_LATEST = 1;

	public const int QUERYOWNERSHIPTOKEN_API_LATEST = 2;

	public const int QUERYOWNERSHIPTOKEN_MAX_CATALOGITEM_IDS = 32;

	public const int QUERYOWNERSHIP_API_LATEST = 2;

	public const int QUERYOWNERSHIP_MAX_CATALOG_IDS = 400;

	public const int QUERYOWNERSHIP_MAX_SANDBOX_IDS = 10;

	public const int REDEEMENTITLEMENTS_API_LATEST = 2;

	public const int REDEEMENTITLEMENTS_MAX_IDS = 32;

	public const int TRANSACTIONID_MAXIMUM_LENGTH = 64;

	public const int TRANSACTIONID_MAX_LENGTH = 64;

	public const int TRANSACTION_COPYENTITLEMENTBYINDEX_API_LATEST = 1;

	public const int TRANSACTION_GETENTITLEMENTSCOUNT_API_LATEST = 1;

	public EcomInterface()
	{
	}

	public EcomInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public void Checkout(ref CheckoutOptions options, object clientData, OnCheckoutCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		CheckoutOptionsInternal options2 = default(CheckoutOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Ecom_Checkout(base.InnerHandle, ref options2, clientDataPointer, OnCheckoutCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public Result CopyEntitlementById(ref CopyEntitlementByIdOptions options, out Entitlement? outEntitlement)
	{
		CopyEntitlementByIdOptionsInternal options2 = default(CopyEntitlementByIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outEntitlement2 = IntPtr.Zero;
		Result result = Bindings.EOS_Ecom_CopyEntitlementById(base.InnerHandle, ref options2, out outEntitlement2);
		Helper.Dispose(ref options2);
		Helper.Get<EntitlementInternal, Entitlement>(outEntitlement2, out outEntitlement);
		if (outEntitlement2 != IntPtr.Zero)
		{
			Bindings.EOS_Ecom_Entitlement_Release(outEntitlement2);
		}
		return result;
	}

	public Result CopyEntitlementByIndex(ref CopyEntitlementByIndexOptions options, out Entitlement? outEntitlement)
	{
		CopyEntitlementByIndexOptionsInternal options2 = default(CopyEntitlementByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outEntitlement2 = IntPtr.Zero;
		Result result = Bindings.EOS_Ecom_CopyEntitlementByIndex(base.InnerHandle, ref options2, out outEntitlement2);
		Helper.Dispose(ref options2);
		Helper.Get<EntitlementInternal, Entitlement>(outEntitlement2, out outEntitlement);
		if (outEntitlement2 != IntPtr.Zero)
		{
			Bindings.EOS_Ecom_Entitlement_Release(outEntitlement2);
		}
		return result;
	}

	public Result CopyEntitlementByNameAndIndex(ref CopyEntitlementByNameAndIndexOptions options, out Entitlement? outEntitlement)
	{
		CopyEntitlementByNameAndIndexOptionsInternal options2 = default(CopyEntitlementByNameAndIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outEntitlement2 = IntPtr.Zero;
		Result result = Bindings.EOS_Ecom_CopyEntitlementByNameAndIndex(base.InnerHandle, ref options2, out outEntitlement2);
		Helper.Dispose(ref options2);
		Helper.Get<EntitlementInternal, Entitlement>(outEntitlement2, out outEntitlement);
		if (outEntitlement2 != IntPtr.Zero)
		{
			Bindings.EOS_Ecom_Entitlement_Release(outEntitlement2);
		}
		return result;
	}

	public Result CopyItemById(ref CopyItemByIdOptions options, out CatalogItem? outItem)
	{
		CopyItemByIdOptionsInternal options2 = default(CopyItemByIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outItem2 = IntPtr.Zero;
		Result result = Bindings.EOS_Ecom_CopyItemById(base.InnerHandle, ref options2, out outItem2);
		Helper.Dispose(ref options2);
		Helper.Get<CatalogItemInternal, CatalogItem>(outItem2, out outItem);
		if (outItem2 != IntPtr.Zero)
		{
			Bindings.EOS_Ecom_CatalogItem_Release(outItem2);
		}
		return result;
	}

	public Result CopyItemImageInfoByIndex(ref CopyItemImageInfoByIndexOptions options, out KeyImageInfo? outImageInfo)
	{
		CopyItemImageInfoByIndexOptionsInternal options2 = default(CopyItemImageInfoByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outImageInfo2 = IntPtr.Zero;
		Result result = Bindings.EOS_Ecom_CopyItemImageInfoByIndex(base.InnerHandle, ref options2, out outImageInfo2);
		Helper.Dispose(ref options2);
		Helper.Get<KeyImageInfoInternal, KeyImageInfo>(outImageInfo2, out outImageInfo);
		if (outImageInfo2 != IntPtr.Zero)
		{
			Bindings.EOS_Ecom_KeyImageInfo_Release(outImageInfo2);
		}
		return result;
	}

	public Result CopyItemReleaseByIndex(ref CopyItemReleaseByIndexOptions options, out CatalogRelease? outRelease)
	{
		CopyItemReleaseByIndexOptionsInternal options2 = default(CopyItemReleaseByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outRelease2 = IntPtr.Zero;
		Result result = Bindings.EOS_Ecom_CopyItemReleaseByIndex(base.InnerHandle, ref options2, out outRelease2);
		Helper.Dispose(ref options2);
		Helper.Get<CatalogReleaseInternal, CatalogRelease>(outRelease2, out outRelease);
		if (outRelease2 != IntPtr.Zero)
		{
			Bindings.EOS_Ecom_CatalogRelease_Release(outRelease2);
		}
		return result;
	}

	public Result CopyLastRedeemEntitlementsResultByIndex(ref CopyLastRedeemEntitlementsResultByIndexOptions options, out Utf8String outEntitlementId)
	{
		CopyLastRedeemEntitlementsResultByIndexOptionsInternal options2 = default(CopyLastRedeemEntitlementsResultByIndexOptionsInternal);
		options2.Set(ref options);
		int inOutEntitlementIdLength = 33;
		IntPtr value = Helper.AddAllocation(inOutEntitlementIdLength);
		Result result = Bindings.EOS_Ecom_CopyLastRedeemEntitlementsResultByIndex(base.InnerHandle, ref options2, value, ref inOutEntitlementIdLength);
		Helper.Dispose(ref options2);
		Helper.Get(value, out outEntitlementId);
		Helper.Dispose(ref value);
		return result;
	}

	public Result CopyLastRedeemedEntitlementByIndex(ref CopyLastRedeemedEntitlementByIndexOptions options, out Utf8String outRedeemedEntitlementId)
	{
		CopyLastRedeemedEntitlementByIndexOptionsInternal options2 = default(CopyLastRedeemedEntitlementByIndexOptionsInternal);
		options2.Set(ref options);
		int inOutRedeemedEntitlementIdLength = 33;
		IntPtr value = Helper.AddAllocation(inOutRedeemedEntitlementIdLength);
		Result result = Bindings.EOS_Ecom_CopyLastRedeemedEntitlementByIndex(base.InnerHandle, ref options2, value, ref inOutRedeemedEntitlementIdLength);
		Helper.Dispose(ref options2);
		Helper.Get(value, out outRedeemedEntitlementId);
		Helper.Dispose(ref value);
		return result;
	}

	public Result CopyOfferById(ref CopyOfferByIdOptions options, out CatalogOffer? outOffer)
	{
		CopyOfferByIdOptionsInternal options2 = default(CopyOfferByIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outOffer2 = IntPtr.Zero;
		Result result = Bindings.EOS_Ecom_CopyOfferById(base.InnerHandle, ref options2, out outOffer2);
		Helper.Dispose(ref options2);
		Helper.Get<CatalogOfferInternal, CatalogOffer>(outOffer2, out outOffer);
		if (outOffer2 != IntPtr.Zero)
		{
			Bindings.EOS_Ecom_CatalogOffer_Release(outOffer2);
		}
		return result;
	}

	public Result CopyOfferByIndex(ref CopyOfferByIndexOptions options, out CatalogOffer? outOffer)
	{
		CopyOfferByIndexOptionsInternal options2 = default(CopyOfferByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outOffer2 = IntPtr.Zero;
		Result result = Bindings.EOS_Ecom_CopyOfferByIndex(base.InnerHandle, ref options2, out outOffer2);
		Helper.Dispose(ref options2);
		Helper.Get<CatalogOfferInternal, CatalogOffer>(outOffer2, out outOffer);
		if (outOffer2 != IntPtr.Zero)
		{
			Bindings.EOS_Ecom_CatalogOffer_Release(outOffer2);
		}
		return result;
	}

	public Result CopyOfferImageInfoByIndex(ref CopyOfferImageInfoByIndexOptions options, out KeyImageInfo? outImageInfo)
	{
		CopyOfferImageInfoByIndexOptionsInternal options2 = default(CopyOfferImageInfoByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outImageInfo2 = IntPtr.Zero;
		Result result = Bindings.EOS_Ecom_CopyOfferImageInfoByIndex(base.InnerHandle, ref options2, out outImageInfo2);
		Helper.Dispose(ref options2);
		Helper.Get<KeyImageInfoInternal, KeyImageInfo>(outImageInfo2, out outImageInfo);
		if (outImageInfo2 != IntPtr.Zero)
		{
			Bindings.EOS_Ecom_KeyImageInfo_Release(outImageInfo2);
		}
		return result;
	}

	public Result CopyOfferItemByIndex(ref CopyOfferItemByIndexOptions options, out CatalogItem? outItem)
	{
		CopyOfferItemByIndexOptionsInternal options2 = default(CopyOfferItemByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outItem2 = IntPtr.Zero;
		Result result = Bindings.EOS_Ecom_CopyOfferItemByIndex(base.InnerHandle, ref options2, out outItem2);
		Helper.Dispose(ref options2);
		Helper.Get<CatalogItemInternal, CatalogItem>(outItem2, out outItem);
		if (outItem2 != IntPtr.Zero)
		{
			Bindings.EOS_Ecom_CatalogItem_Release(outItem2);
		}
		return result;
	}

	public Result CopyTransactionById(ref CopyTransactionByIdOptions options, out Transaction outTransaction)
	{
		CopyTransactionByIdOptionsInternal options2 = default(CopyTransactionByIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outTransaction2 = IntPtr.Zero;
		Result result = Bindings.EOS_Ecom_CopyTransactionById(base.InnerHandle, ref options2, out outTransaction2);
		Helper.Dispose(ref options2);
		Helper.Get(outTransaction2, out outTransaction);
		return result;
	}

	public Result CopyTransactionByIndex(ref CopyTransactionByIndexOptions options, out Transaction outTransaction)
	{
		CopyTransactionByIndexOptionsInternal options2 = default(CopyTransactionByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outTransaction2 = IntPtr.Zero;
		Result result = Bindings.EOS_Ecom_CopyTransactionByIndex(base.InnerHandle, ref options2, out outTransaction2);
		Helper.Dispose(ref options2);
		Helper.Get(outTransaction2, out outTransaction);
		return result;
	}

	public uint GetEntitlementsByNameCount(ref GetEntitlementsByNameCountOptions options)
	{
		GetEntitlementsByNameCountOptionsInternal options2 = default(GetEntitlementsByNameCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Ecom_GetEntitlementsByNameCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetEntitlementsCount(ref GetEntitlementsCountOptions options)
	{
		GetEntitlementsCountOptionsInternal options2 = default(GetEntitlementsCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Ecom_GetEntitlementsCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetItemImageInfoCount(ref GetItemImageInfoCountOptions options)
	{
		GetItemImageInfoCountOptionsInternal options2 = default(GetItemImageInfoCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Ecom_GetItemImageInfoCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetItemReleaseCount(ref GetItemReleaseCountOptions options)
	{
		GetItemReleaseCountOptionsInternal options2 = default(GetItemReleaseCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Ecom_GetItemReleaseCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetLastRedeemEntitlementsResultCount(ref GetLastRedeemEntitlementsResultCountOptions options)
	{
		GetLastRedeemEntitlementsResultCountOptionsInternal options2 = default(GetLastRedeemEntitlementsResultCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Ecom_GetLastRedeemEntitlementsResultCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetLastRedeemedEntitlementsCount(ref GetLastRedeemedEntitlementsCountOptions options)
	{
		GetLastRedeemedEntitlementsCountOptionsInternal options2 = default(GetLastRedeemedEntitlementsCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Ecom_GetLastRedeemedEntitlementsCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetOfferCount(ref GetOfferCountOptions options)
	{
		GetOfferCountOptionsInternal options2 = default(GetOfferCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Ecom_GetOfferCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetOfferImageInfoCount(ref GetOfferImageInfoCountOptions options)
	{
		GetOfferImageInfoCountOptionsInternal options2 = default(GetOfferImageInfoCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Ecom_GetOfferImageInfoCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetOfferItemCount(ref GetOfferItemCountOptions options)
	{
		GetOfferItemCountOptionsInternal options2 = default(GetOfferItemCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Ecom_GetOfferItemCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetTransactionCount(ref GetTransactionCountOptions options)
	{
		GetTransactionCountOptionsInternal options2 = default(GetTransactionCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Ecom_GetTransactionCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void QueryEntitlementToken(ref QueryEntitlementTokenOptions options, object clientData, OnQueryEntitlementTokenCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryEntitlementTokenOptionsInternal options2 = default(QueryEntitlementTokenOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Ecom_QueryEntitlementToken(base.InnerHandle, ref options2, clientDataPointer, OnQueryEntitlementTokenCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryEntitlements(ref QueryEntitlementsOptions options, object clientData, OnQueryEntitlementsCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryEntitlementsOptionsInternal options2 = default(QueryEntitlementsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Ecom_QueryEntitlements(base.InnerHandle, ref options2, clientDataPointer, OnQueryEntitlementsCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryOffers(ref QueryOffersOptions options, object clientData, OnQueryOffersCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryOffersOptionsInternal options2 = default(QueryOffersOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Ecom_QueryOffers(base.InnerHandle, ref options2, clientDataPointer, OnQueryOffersCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryOwnership(ref QueryOwnershipOptions options, object clientData, OnQueryOwnershipCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryOwnershipOptionsInternal options2 = default(QueryOwnershipOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Ecom_QueryOwnership(base.InnerHandle, ref options2, clientDataPointer, OnQueryOwnershipCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryOwnershipBySandboxIds(ref QueryOwnershipBySandboxIdsOptions options, object clientData, OnQueryOwnershipBySandboxIdsCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryOwnershipBySandboxIdsOptionsInternal options2 = default(QueryOwnershipBySandboxIdsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Ecom_QueryOwnershipBySandboxIds(base.InnerHandle, ref options2, clientDataPointer, OnQueryOwnershipBySandboxIdsCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryOwnershipToken(ref QueryOwnershipTokenOptions options, object clientData, OnQueryOwnershipTokenCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryOwnershipTokenOptionsInternal options2 = default(QueryOwnershipTokenOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Ecom_QueryOwnershipToken(base.InnerHandle, ref options2, clientDataPointer, OnQueryOwnershipTokenCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RedeemEntitlements(ref RedeemEntitlementsOptions options, object clientData, OnRedeemEntitlementsCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		RedeemEntitlementsOptionsInternal options2 = default(RedeemEntitlementsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Ecom_RedeemEntitlements(base.InnerHandle, ref options2, clientDataPointer, OnRedeemEntitlementsCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}

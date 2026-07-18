using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Epic.OnlineServices;

public sealed class Helper
{
	private struct Allocation
	{
		public int Size { get; private set; }

		public object Cache { get; private set; }

		public bool? IsArrayItemAllocated { get; private set; }

		public Allocation(int size, object cache, bool? isArrayItemAllocated = null)
		{
			Size = size;
			Cache = cache;
			IsArrayItemAllocated = isArrayItemAllocated;
		}
	}

	private struct PinnedBuffer
	{
		public GCHandle Handle { get; private set; }

		public int RefCount { get; set; }

		public PinnedBuffer(GCHandle handle)
		{
			Handle = handle;
			RefCount = 1;
		}
	}

	private class DelegateHolder
	{
		public List<Delegate> Delegates { get; private set; } = new List<Delegate>();

		public ulong? NotificationId { get; set; }

		public DelegateHolder(params Delegate[] delegates)
		{
			Delegates.AddRange(delegates.Where((Delegate d) => (object)d != null));
		}
	}

	private static Dictionary<ulong, Allocation> s_Allocations = new Dictionary<ulong, Allocation>();

	private static Dictionary<ulong, PinnedBuffer> s_PinnedBuffers = new Dictionary<ulong, PinnedBuffer>();

	private static Dictionary<IntPtr, DelegateHolder> s_Callbacks = new Dictionary<IntPtr, DelegateHolder>();

	private static Dictionary<string, DelegateHolder> s_StaticCallbacks = new Dictionary<string, DelegateHolder>();

	private static long s_LastClientDataId = 0L;

	private static Dictionary<IntPtr, object> s_ClientDatas = new Dictionary<IntPtr, object>();

	internal static void AddCallback(out IntPtr clientDataPointer, object clientData, params Delegate[] delegates)
	{
		clientDataPointer = AddClientData(clientData);
		lock (s_Callbacks)
		{
			s_Callbacks.Add(clientDataPointer, new DelegateHolder(delegates));
		}
	}

	internal static void AddCallback(IntPtr clientDataPointer, params Delegate[] delegates)
	{
		lock (s_Callbacks)
		{
			if (s_Callbacks.TryGetValue(clientDataPointer, out var value))
			{
				value.Delegates.AddRange(delegates.Where((Delegate d) => (object)d != null));
			}
		}
	}

	internal static void RemoveCallback(IntPtr clientDataPointer)
	{
		lock (s_Callbacks)
		{
			s_Callbacks.Remove(clientDataPointer);
		}
		RemoveClientData(clientDataPointer);
	}

	internal static bool TryGetCallback<TCallbackInfoInternal, TCallback, TCallbackInfo>(ref TCallbackInfoInternal callbackInfoInternal, out TCallback callback, out TCallbackInfo callbackInfo) where TCallbackInfoInternal : struct, ICallbackInfoInternal, IGettable<TCallbackInfo> where TCallback : class where TCallbackInfo : struct, ICallbackInfo
	{
		Get<TCallbackInfoInternal, TCallbackInfo>(ref callbackInfoInternal, out callbackInfo, out var clientDataPointer);
		callback = null;
		lock (s_Callbacks)
		{
			if (s_Callbacks.TryGetValue(clientDataPointer, out var value))
			{
				callback = value.Delegates.FirstOrDefault((Delegate d) => d.GetType() == typeof(TCallback)) as TCallback;
				return callback != null;
			}
		}
		return false;
	}

	internal static bool TryGetAndRemoveCallback<TCallbackInfoInternal, TCallback, TCallbackInfo>(ref TCallbackInfoInternal callbackInfoInternal, out TCallback callback, out TCallbackInfo callbackInfo) where TCallbackInfoInternal : struct, ICallbackInfoInternal, IGettable<TCallbackInfo> where TCallback : class where TCallbackInfo : struct, ICallbackInfo
	{
		Get<TCallbackInfoInternal, TCallbackInfo>(ref callbackInfoInternal, out callbackInfo, out var clientDataPointer);
		callback = null;
		ulong? num = null;
		lock (s_Callbacks)
		{
			if (s_Callbacks.TryGetValue(clientDataPointer, out var value))
			{
				callback = value.Delegates.FirstOrDefault((Delegate d) => d.GetType() == typeof(TCallback)) as TCallback;
				num = value.NotificationId;
			}
		}
		if (callback != null)
		{
			if (!num.HasValue && callbackInfo.GetResultCode().HasValue && Common.IsOperationComplete(callbackInfo.GetResultCode().Value))
			{
				RemoveCallback(clientDataPointer);
			}
			return true;
		}
		return false;
	}

	internal static bool TryGetStructCallback<TCallbackInfoInternal, TCallback, TCallbackInfo>(ref TCallbackInfoInternal callbackInfoInternal, out TCallback callback, out TCallbackInfo callbackInfo) where TCallbackInfoInternal : struct, ICallbackInfoInternal, IGettable<TCallbackInfo> where TCallback : class where TCallbackInfo : struct
	{
		Get<TCallbackInfoInternal, TCallbackInfo>(ref callbackInfoInternal, out callbackInfo, out var clientDataPointer);
		callback = null;
		lock (s_Callbacks)
		{
			if (s_Callbacks.TryGetValue(clientDataPointer, out var value))
			{
				callback = value.Delegates.FirstOrDefault((Delegate d) => d.GetType() == typeof(TCallback)) as TCallback;
				if (callback != null)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static void RemoveCallbackByNotificationId(ulong notificationId)
	{
		IntPtr clientDataPointer = IntPtr.Zero;
		lock (s_Callbacks)
		{
			clientDataPointer = s_Callbacks.SingleOrDefault((KeyValuePair<IntPtr, DelegateHolder> pair) => pair.Value.NotificationId.HasValue && pair.Value.NotificationId == notificationId).Key;
		}
		RemoveCallback(clientDataPointer);
	}

	internal static void AddStaticCallback(string key, params Delegate[] delegates)
	{
		lock (s_StaticCallbacks)
		{
			s_StaticCallbacks.Remove(key);
			s_StaticCallbacks.Add(key, new DelegateHolder(delegates));
		}
	}

	internal static bool TryGetStaticCallback<TCallback>(string key, out TCallback callback) where TCallback : class
	{
		callback = null;
		lock (s_StaticCallbacks)
		{
			if (s_StaticCallbacks.TryGetValue(key, out var value))
			{
				callback = value.Delegates.FirstOrDefault((Delegate d) => d.GetType() == typeof(TCallback)) as TCallback;
				if (callback != null)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static void AssignNotificationIdToCallback(IntPtr clientDataPointer, ulong notificationId)
	{
		if (notificationId == 0L)
		{
			RemoveCallback(clientDataPointer);
			return;
		}
		lock (s_Callbacks)
		{
			if (s_Callbacks.TryGetValue(clientDataPointer, out var value))
			{
				value.NotificationId = notificationId;
			}
		}
	}

	private static IntPtr AddClientData(object clientData)
	{
		lock (s_ClientDatas)
		{
			IntPtr intPtr = new IntPtr(++s_LastClientDataId);
			s_ClientDatas.Add(intPtr, clientData);
			return intPtr;
		}
	}

	private static void RemoveClientData(IntPtr clientDataPointer)
	{
		lock (s_ClientDatas)
		{
			s_ClientDatas.Remove(clientDataPointer);
		}
	}

	private static object GetClientData(IntPtr clientDataPointer)
	{
		lock (s_ClientDatas)
		{
			s_ClientDatas.TryGetValue(clientDataPointer, out var value);
			return value;
		}
	}

	private static void Convert<THandle>(IntPtr from, out THandle to) where THandle : Handle, new()
	{
		to = null;
		if (from != IntPtr.Zero)
		{
			to = new THandle();
			to.InnerHandle = from;
		}
	}

	private static void Convert(Handle from, out IntPtr to)
	{
		to = IntPtr.Zero;
		if (from != null)
		{
			to = from.InnerHandle;
		}
	}

	private static void Convert(byte[] from, out Utf8String to)
	{
		to = null;
		if (from != null)
		{
			to = Encoding.ASCII.GetString(from, 0, GetAnsiStringLength(from));
		}
	}

	private static void Convert(string from, out byte[] to, int fromLength)
	{
		if (from == null)
		{
			from = "";
		}
		to = new byte[fromLength];
		Encoding.ASCII.GetBytes(from, 0, from.Length, to, 0);
		to[from.Length] = 0;
	}

	private static void Convert<TArray>(TArray[] from, out int to)
	{
		to = 0;
		if (from != null)
		{
			to = from.Length;
		}
	}

	private static void Convert<TArray>(TArray[] from, out uint to)
	{
		to = 0u;
		if (from != null)
		{
			to = (uint)from.Length;
		}
	}

	private static void Convert<TArray>(ArraySegment<TArray> from, out int to)
	{
		to = from.Count;
	}

	private static void Convert<T>(ArraySegment<T> from, out uint to)
	{
		to = (uint)from.Count;
	}

	private static void Convert(int from, out bool to)
	{
		to = from != 0;
	}

	private static void Convert(bool from, out int to)
	{
		to = (from ? 1 : 0);
	}

	private static void Convert(DateTimeOffset? from, out long to)
	{
		to = -1L;
		if (from.HasValue)
		{
			DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			long num = (from.Value.UtcDateTime - dateTime).Ticks / 10000000;
			to = num;
		}
	}

	private static void Convert(long from, out DateTimeOffset? to)
	{
		to = null;
		if (from >= 0)
		{
			DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			long num = from * 10000000;
			to = new DateTimeOffset(dateTime.Ticks + num, TimeSpan.Zero);
		}
	}

	internal static void Get<TArray>(TArray[] from, out int to)
	{
		Convert(from, out to);
	}

	internal static void Get<TArray>(TArray[] from, out uint to)
	{
		Convert(from, out to);
	}

	internal static void Get(ArraySegment<byte> from, out uint to)
	{
		Convert(from, out to);
	}

	internal static void Get<TInternal, TPublic>(ref TInternal from, out TPublic to) where TInternal : struct, IGettable<TPublic> where TPublic : struct
	{
		from.Get(out to);
	}

	internal static void Get<TInternal, TPublic>(ref TInternal from, out TPublic? to) where TInternal : struct, IGettable<TPublic> where TPublic : struct
	{
		TPublic other = default(TPublic);
		from.Get(out other);
		to = other;
	}

	internal static void Get<T>(T from, out T? to) where T : struct
	{
		to = from;
	}

	internal static void Get(int from, out bool to)
	{
		Convert(from, out to);
	}

	internal static void Get(int from, out bool? to)
	{
		Convert(from, out var to2);
		to = to2;
	}

	internal static void Get(bool from, out int to)
	{
		Convert(from, out to);
	}

	internal static void Get(long from, out DateTimeOffset? to)
	{
		Convert(from, out to);
	}

	internal static void Get(IntPtr from, out ArraySegment<byte> to, uint arrayLength)
	{
		to = default(ArraySegment<byte>);
		if (arrayLength != 0)
		{
			byte[] array = new byte[arrayLength];
			Marshal.Copy(from, array, 0, (int)arrayLength);
			to = new ArraySegment<byte>(array);
		}
	}

	internal static void Get(IntPtr from, out Utf8String[] to, int arrayLength, bool isArrayItemAllocated)
	{
		GetAllocation<Utf8String>(from, out to, arrayLength, isArrayItemAllocated);
	}

	internal static void Get(IntPtr from, out Utf8String[] to, uint arrayLength, bool isArrayItemAllocated)
	{
		GetAllocation<Utf8String>(from, out to, (int)arrayLength, isArrayItemAllocated);
	}

	internal static void Get<T>(IntPtr from, out T[] to, uint arrayLength, bool isArrayItemAllocated) where T : struct
	{
		GetAllocation<T>(from, out to, (int)arrayLength, isArrayItemAllocated);
	}

	internal static void Get<T>(IntPtr from, out T[] to, int arrayLength, bool isArrayItemAllocated) where T : struct
	{
		GetAllocation<T>(from, out to, arrayLength, isArrayItemAllocated);
	}

	internal static void Get<THandle>(IntPtr from, out THandle to) where THandle : Handle, new()
	{
		Convert<THandle>(from, out to);
	}

	internal static void Get<THandle>(IntPtr from, out THandle[] to, uint arrayLength) where THandle : Handle, new()
	{
		GetAllocation<THandle>(from, out to, (int)arrayLength);
	}

	internal static void Get(IntPtr from, out IntPtr[] to, uint arrayLength)
	{
		GetAllocation<IntPtr>(from, out to, (int)arrayLength, isArrayItemAllocated: false);
	}

	internal static void Get<TInternal, TPublic>(TInternal[] from, out TPublic[] to) where TInternal : struct, IGettable<TPublic> where TPublic : struct
	{
		to = null;
		if (from != null)
		{
			to = new TPublic[from.Length];
			for (int i = 0; i < from.Length; i++)
			{
				from[i].Get(out to[i]);
			}
		}
	}

	internal static void Get<TInternal, TPublic>(IntPtr from, out TPublic[] to, int arrayLength, bool isArrayItemAllocated) where TInternal : struct, IGettable<TPublic> where TPublic : struct
	{
		Get(from, out TInternal[] to2, arrayLength, isArrayItemAllocated);
		Get(to2, out to);
	}

	internal static void Get<TInternal, TPublic>(IntPtr from, out TPublic[] to, uint arrayLength, bool isArrayItemAllocated) where TInternal : struct, IGettable<TPublic> where TPublic : struct
	{
		Get<TInternal, TPublic>(from, out to, (int)arrayLength, isArrayItemAllocated);
	}

	internal static void Get<T>(IntPtr from, out T? to) where T : struct
	{
		GetAllocation(from, out to);
	}

	internal static void Get(byte[] from, out Utf8String to)
	{
		Convert(from, out to);
	}

	internal static void Get(IntPtr from, out object to)
	{
		to = GetClientData(from);
	}

	internal static void Get(IntPtr from, out Utf8String to)
	{
		GetAllocation(from, out to);
	}

	internal static void Get<TInternal, TPublic>(IntPtr from, out TPublic to) where TInternal : struct, IGettable<TPublic> where TPublic : struct
	{
		to = default(TPublic);
		Get(from, out TInternal? to2);
		if (to2.HasValue)
		{
			to2.Value.Get(out to);
		}
	}

	internal static void Get<TInternal, TPublic>(IntPtr from, out TPublic? to) where TInternal : struct, IGettable<TPublic> where TPublic : struct
	{
		to = null;
		Get(from, out TInternal? to2);
		if (to2.HasValue)
		{
			to2.Value.Get(out var other);
			to = other;
		}
	}

	internal static void Get<TInternal, TPublic>(ref TInternal from, out TPublic to, out IntPtr clientDataPointer) where TInternal : struct, ICallbackInfoInternal, IGettable<TPublic> where TPublic : struct
	{
		from.Get(out to);
		clientDataPointer = from.ClientDataPointer;
	}

	public static int GetAllocationCount()
	{
		return s_Allocations.Count + s_PinnedBuffers.Aggregate(0, (int acc, KeyValuePair<ulong, PinnedBuffer> x) => acc + x.Value.RefCount) + s_Callbacks.Count + s_ClientDatas.Count;
	}

	internal static void Copy(byte[] from, IntPtr to)
	{
		if (from != null && to != IntPtr.Zero)
		{
			Marshal.Copy(from, 0, to, from.Length);
		}
	}

	internal static void Copy(ArraySegment<byte> from, IntPtr to)
	{
		if (from.Count != 0 && to != IntPtr.Zero)
		{
			Marshal.Copy(from.Array, from.Offset, to, from.Count);
		}
	}

	internal static void Dispose(ref IntPtr value)
	{
		RemoveAllocation(ref value);
		RemovePinnedBuffer(ref value);
		value = default(IntPtr);
	}

	internal static void Dispose(ref IDisposable disposable)
	{
		disposable?.Dispose();
	}

	internal static void Dispose<TDisposable>(ref TDisposable disposable) where TDisposable : struct, IDisposable
	{
		disposable.Dispose();
	}

	private static int GetAnsiStringLength(byte[] bytes)
	{
		int num = 0;
		for (int i = 0; i < bytes.Length && bytes[i] != 0; i++)
		{
			num++;
		}
		return num;
	}

	private static int GetAnsiStringLength(IntPtr pointer)
	{
		int i;
		for (i = 0; Marshal.ReadByte(pointer, i) != 0; i++)
		{
		}
		return i;
	}

	private static void GetAllocation<T>(IntPtr source, out T target)
	{
		target = default(T);
		if (source == IntPtr.Zero)
		{
			return;
		}
		if (TryGetAllocationCache(source, out var cache) && cache != null)
		{
			if (!(cache.GetType() == typeof(T)))
			{
				throw new CachedTypeAllocationException(source, cache.GetType(), typeof(T));
			}
			target = (T)cache;
		}
		else
		{
			target = (T)Marshal.PtrToStructure(source, typeof(T));
		}
	}

	private static void GetAllocation<T>(IntPtr source, out T? target) where T : struct
	{
		target = null;
		if (source == IntPtr.Zero)
		{
			return;
		}
		if (TryGetAllocationCache(source, out var cache) && cache != null)
		{
			if (!(cache.GetType() == typeof(T)))
			{
				throw new CachedTypeAllocationException(source, cache.GetType(), typeof(T));
			}
			target = (T?)cache;
		}
		else if (typeof(T).IsEnum)
		{
			target = (T)Marshal.PtrToStructure(source, Enum.GetUnderlyingType(typeof(T)));
		}
		else
		{
			target = (T?)Marshal.PtrToStructure(source, typeof(T));
		}
	}

	private static void GetAllocation<THandle>(IntPtr source, out THandle[] target, int arrayLength) where THandle : Handle, new()
	{
		target = null;
		if (source == IntPtr.Zero)
		{
			return;
		}
		if (TryGetAllocationCache(source, out var cache) && cache != null)
		{
			if (!(cache.GetType() == typeof(THandle[])))
			{
				throw new CachedTypeAllocationException(source, cache.GetType(), typeof(THandle[]));
			}
			Array array = (Array)cache;
			if (array.Length != arrayLength)
			{
				throw new CachedArrayAllocationException(source, array.Length, arrayLength);
			}
			target = array as THandle[];
		}
		else
		{
			int num = Marshal.SizeOf(typeof(IntPtr));
			List<THandle> list = new List<THandle>();
			for (int i = 0; i < arrayLength; i++)
			{
				Convert<THandle>(Marshal.ReadIntPtr(new IntPtr(source.ToInt64() + i * num)), out var to);
				list.Add(to);
			}
			target = list.ToArray();
		}
	}

	private static void GetAllocation<T>(IntPtr from, out T[] to, int arrayLength, bool isArrayItemAllocated)
	{
		to = null;
		if (from == IntPtr.Zero)
		{
			return;
		}
		if (TryGetAllocationCache(from, out var cache) && cache != null)
		{
			if (cache.GetType() == typeof(T[]))
			{
				Array array = (Array)cache;
				if (array.Length == arrayLength)
				{
					to = array as T[];
					return;
				}
				throw new CachedArrayAllocationException(from, array.Length, arrayLength);
			}
			throw new CachedTypeAllocationException(from, cache.GetType(), typeof(T[]));
		}
		int num = ((!isArrayItemAllocated) ? Marshal.SizeOf(typeof(T)) : Marshal.SizeOf(typeof(IntPtr)));
		List<T> list = new List<T>();
		for (int i = 0; i < arrayLength; i++)
		{
			IntPtr intPtr = new IntPtr(from.ToInt64() + i * num);
			if (isArrayItemAllocated)
			{
				intPtr = Marshal.ReadIntPtr(intPtr);
			}
			T target2;
			if (typeof(T) == typeof(Utf8String))
			{
				GetAllocation(intPtr, out var target);
				target2 = (T)(object)target;
			}
			else
			{
				GetAllocation(intPtr, out target2);
			}
			list.Add(target2);
		}
		to = list.ToArray();
	}

	private static void GetAllocation(IntPtr source, out Utf8String target)
	{
		target = null;
		if (!(source == IntPtr.Zero))
		{
			int ansiStringLength = GetAnsiStringLength(source);
			byte[] array = new byte[ansiStringLength + 1];
			Marshal.Copy(source, array, 0, ansiStringLength + 1);
			target = new Utf8String(array);
		}
	}

	internal static IntPtr AddAllocation(int size)
	{
		if (size == 0)
		{
			return IntPtr.Zero;
		}
		IntPtr intPtr = Marshal.AllocHGlobal(size);
		Marshal.WriteByte(intPtr, 0, 0);
		lock (s_Allocations)
		{
			s_Allocations.Add((ulong)(long)intPtr, new Allocation(size, null));
			return intPtr;
		}
	}

	internal static IntPtr AddAllocation(uint size)
	{
		return AddAllocation((int)size);
	}

	private static IntPtr AddAllocation<T>(int size, T cache)
	{
		if (size == 0 || cache == null)
		{
			return IntPtr.Zero;
		}
		IntPtr intPtr = Marshal.AllocHGlobal(size);
		Marshal.StructureToPtr(cache, intPtr, fDeleteOld: false);
		lock (s_Allocations)
		{
			s_Allocations.Add((ulong)(long)intPtr, new Allocation(size, cache));
			return intPtr;
		}
	}

	private static IntPtr AddAllocation<T>(int size, T[] cache, bool? isArrayItemAllocated)
	{
		if (size == 0 || cache == null)
		{
			return IntPtr.Zero;
		}
		IntPtr intPtr = Marshal.AllocHGlobal(size);
		Marshal.WriteByte(intPtr, 0, 0);
		lock (s_Allocations)
		{
			s_Allocations.Add((ulong)(long)intPtr, new Allocation(size, cache, isArrayItemAllocated));
			return intPtr;
		}
	}

	private static IntPtr AddAllocation<T>(T[] array, bool isArrayItemAllocated)
	{
		if (array == null)
		{
			return IntPtr.Zero;
		}
		int num = ((!isArrayItemAllocated && !(typeof(T).BaseType == typeof(Handle))) ? Marshal.SizeOf(typeof(T)) : Marshal.SizeOf(typeof(IntPtr)));
		IntPtr result = AddAllocation(array.Length * num, array, isArrayItemAllocated);
		for (int i = 0; i < array.Length; i++)
		{
			T val = (T)array.GetValue(i);
			if (isArrayItemAllocated)
			{
				IntPtr structure = ((!(typeof(T) == typeof(Utf8String))) ? AddAllocation(Marshal.SizeOf(typeof(T)), val) : AddPinnedBuffer((Utf8String)(object)val));
				IntPtr ptr = new IntPtr(result.ToInt64() + i * num);
				Marshal.StructureToPtr(structure, ptr, fDeleteOld: false);
				continue;
			}
			IntPtr ptr2 = new IntPtr(result.ToInt64() + i * num);
			if (typeof(T).BaseType == typeof(Handle))
			{
				Convert((Handle)(object)val, out var to);
				Marshal.StructureToPtr(to, ptr2, fDeleteOld: false);
			}
			else
			{
				Marshal.StructureToPtr(val, ptr2, fDeleteOld: false);
			}
		}
		return result;
	}

	private static void RemoveAllocation(ref IntPtr pointer)
	{
		if (pointer == IntPtr.Zero)
		{
			return;
		}
		Allocation value;
		lock (s_Allocations)
		{
			if (!s_Allocations.TryGetValue((ulong)(long)pointer, out value))
			{
				return;
			}
			s_Allocations.Remove((ulong)(long)pointer);
		}
		if (value.IsArrayItemAllocated.HasValue)
		{
			int num = ((!value.IsArrayItemAllocated.Value && !(value.Cache.GetType().GetElementType().BaseType == typeof(Handle))) ? Marshal.SizeOf(value.Cache.GetType().GetElementType()) : Marshal.SizeOf(typeof(IntPtr)));
			Array array = value.Cache as Array;
			for (int i = 0; i < array.Length; i++)
			{
				if (value.IsArrayItemAllocated.Value)
				{
					IntPtr ptr = new IntPtr(pointer.ToInt64() + i * num);
					ptr = Marshal.ReadIntPtr(ptr);
					Dispose(ref ptr);
					continue;
				}
				object value2 = array.GetValue(i);
				if (value2 is IDisposable && value2 is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
		}
		if (value.Cache is IDisposable && value.Cache is IDisposable disposable2)
		{
			disposable2.Dispose();
		}
		Marshal.FreeHGlobal(pointer);
		pointer = IntPtr.Zero;
	}

	private static bool TryGetAllocationCache(IntPtr pointer, out object cache)
	{
		cache = null;
		lock (s_Allocations)
		{
			if (s_Allocations.TryGetValue((ulong)(long)pointer, out var value))
			{
				cache = value.Cache;
				return true;
			}
		}
		return false;
	}

	private static IntPtr AddPinnedBuffer(byte[] buffer, int offset)
	{
		if (buffer == null)
		{
			return IntPtr.Zero;
		}
		GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
		ulong num = (ulong)(long)Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
		lock (s_PinnedBuffers)
		{
			if (s_PinnedBuffers.ContainsKey(num))
			{
				PinnedBuffer value = s_PinnedBuffers[num];
				value.RefCount++;
				s_PinnedBuffers[num] = value;
			}
			else
			{
				s_PinnedBuffers.Add(num, new PinnedBuffer(handle));
			}
			return (IntPtr)(long)num;
		}
	}

	private static IntPtr AddPinnedBuffer(Utf8String str)
	{
		if (str == null || str.Bytes == null)
		{
			return IntPtr.Zero;
		}
		return AddPinnedBuffer(str.Bytes, 0);
	}

	internal static IntPtr AddPinnedBuffer(ArraySegment<byte> array)
	{
		if (array == null)
		{
			return IntPtr.Zero;
		}
		return AddPinnedBuffer(array.Array, array.Offset);
	}

	internal static IntPtr AddPinnedBuffer(byte[] array)
	{
		if (array == null)
		{
			return IntPtr.Zero;
		}
		return AddPinnedBuffer(array, 0);
	}

	private static void RemovePinnedBuffer(ref IntPtr pointer)
	{
		if (pointer == IntPtr.Zero)
		{
			return;
		}
		lock (s_PinnedBuffers)
		{
			ulong key = (ulong)(long)pointer;
			if (s_PinnedBuffers.TryGetValue(key, out var value))
			{
				value.RefCount--;
				if (value.RefCount == 0)
				{
					s_PinnedBuffers.Remove(key);
					value.Handle.Free();
				}
				else
				{
					s_PinnedBuffers[key] = value;
				}
			}
		}
		pointer = IntPtr.Zero;
	}

	internal static void Set<T>(T from, ref T? to) where T : struct
	{
		to = from;
	}

	internal static void Set<T>(T? from, ref T to) where T : struct
	{
		to = default(T);
		if (from.HasValue)
		{
			to = from.Value;
		}
	}

	internal static void Set<T>(T? from, ref T? to) where T : struct
	{
		to = from;
	}

	internal static void Set(bool? from, ref int to)
	{
		to = 0;
		if (from.HasValue)
		{
			Convert(from.Value, out to);
		}
	}

	internal static void Set<T>(T from, ref IntPtr to) where T : struct
	{
		Dispose(ref to);
		to = AddAllocation(Marshal.SizeOf(typeof(T)), from);
		Marshal.StructureToPtr(from, to, fDeleteOld: false);
	}

	internal static void Set<T>(T? from, ref IntPtr to) where T : struct
	{
		Dispose(ref to);
		if (from.HasValue)
		{
			to = AddAllocation(Marshal.SizeOf(typeof(T)), from);
			Marshal.StructureToPtr(from.Value, to, fDeleteOld: false);
		}
	}

	internal static void Set(object from, ref IntPtr to)
	{
		Dispose(ref to);
		AddCallback(out to, from);
	}

	internal static void Set(Utf8String from, ref IntPtr to)
	{
		Dispose(ref to);
		to = AddPinnedBuffer(from);
	}

	internal static void Set(Handle from, ref IntPtr to)
	{
		Convert(from, out to);
	}

	internal static void Set<T>(T[] from, ref IntPtr to, bool isArrayItemAllocated)
	{
		Dispose(ref to);
		to = AddAllocation(from, isArrayItemAllocated);
	}

	internal static void Set(ArraySegment<byte> from, ref IntPtr to, out uint arrayLength)
	{
		Dispose(ref to);
		to = AddPinnedBuffer(from);
		Get(from, out arrayLength);
	}

	internal static void Set<T>(T[] from, ref IntPtr to, out int arrayLength, bool isArrayItemAllocated)
	{
		Set(from, ref to, isArrayItemAllocated);
		Get(from, out arrayLength);
	}

	internal static void Set<T>(T[] from, ref IntPtr to, out uint arrayLength, bool isArrayItemAllocated)
	{
		Set(from, ref to, isArrayItemAllocated);
		Get(from, out arrayLength);
	}

	internal static void Set(DateTimeOffset? from, ref long to)
	{
		Convert(from, out to);
	}

	internal static void Set(bool from, ref int to)
	{
		Convert(from, out to);
	}

	internal static void Set(Utf8String from, ref byte[] to, int stringLength)
	{
		Convert(from, out to, stringLength);
	}

	internal static void Set<TPublic, TInternal>(ref TPublic from, ref TInternal to) where TPublic : struct where TInternal : struct, ISettable<TPublic>
	{
		to.Set(ref from);
	}

	internal static void Set<TPublic, TInternal>(TPublic? from, ref IntPtr to) where TPublic : struct where TInternal : struct, ISettable<TPublic>
	{
		Dispose(ref to);
		to = default(IntPtr);
		if (from.HasValue)
		{
			TInternal cache = default(TInternal);
			TPublic other = from.Value;
			cache.Set(ref other);
			to = AddAllocation(Marshal.SizeOf(typeof(TInternal)), cache);
		}
	}

	internal static void Set<TPublic, TInternal>(TPublic? from, ref TInternal to) where TPublic : struct where TInternal : struct, ISettable<TPublic>
	{
		Dispose(ref to);
		to = default(TInternal);
		if (from.HasValue)
		{
			TPublic other = from.Value;
			to.Set(ref other);
		}
	}

	internal static void Set(Utf8String[] from, ref IntPtr to, out int arrayLength, bool isArrayItemAllocated)
	{
		Dispose(ref to);
		to = AddAllocation(from, isArrayItemAllocated);
		Get(from, out arrayLength);
	}

	internal static void Set(Utf8String[] from, ref IntPtr to, out uint arrayLength, bool isArrayItemAllocated)
	{
		Set(from, ref to, out int arrayLength2, isArrayItemAllocated);
		arrayLength = (uint)arrayLength2;
	}

	internal static void Set<TPublic, TInternal>(TPublic from, ref IntPtr to) where TPublic : struct where TInternal : struct, ISettable<TPublic>
	{
		Dispose(ref to);
		TInternal cache = default(TInternal);
		cache.Set(ref from);
		to = AddAllocation(Marshal.SizeOf(typeof(TInternal)), cache);
	}

	internal static void Set<TPublic, TInternal>(TPublic[] from, ref IntPtr to, out int arrayLength, bool isArrayItemAllocated) where TPublic : struct where TInternal : struct, ISettable<TPublic>
	{
		Dispose(ref to);
		arrayLength = 0;
		if (from != null)
		{
			TInternal[] array = new TInternal[from.Length];
			for (int i = 0; i < from.Length; i++)
			{
				array[i].Set(ref from[i]);
			}
			Set(array, ref to, isArrayItemAllocated);
			Get(from, out arrayLength);
		}
	}

	internal static void Set<TPublic, TInternal>(TPublic[] from, ref IntPtr to, out uint arrayLength, bool isArrayItemAllocated) where TPublic : struct where TInternal : struct, ISettable<TPublic>
	{
		Helper.Set<TPublic, TInternal>(from, ref to, out int arrayLength2, isArrayItemAllocated);
		arrayLength = (uint)arrayLength2;
	}
}

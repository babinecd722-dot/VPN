using System;

namespace Epic.OnlineServices;

internal interface ISettable<T> : IDisposable where T : struct
{
	void Set(ref T other);
}

using System;
using System.Collections.Generic;

namespace Biggy
{
	public interface IBiggyEventArgs<T> {
		IList<T> Items { get; set; }
		dynamic Item { get; set; }
	}
}


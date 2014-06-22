using System;
using BiggySamples.Core.ViewModels;
using Biggy;

namespace BiggySamples.Core.Services
{
	public class DataContext
	{
		public static IBiggy<Product> Products { get; set; }
	}

	public class Product
	{
		public string Sku { get; set; }
		public string Name { get; set; }
		public decimal Price { get; set; }
		public DateTime CreatedAt { get; set; }

        public override bool Equals(object obj)
        {
            var p1 = obj as Product;
            if (p1 == null) return false;

            return p1.Sku == this.Sku;
        }

        public override int GetHashCode()
        {
            return Sku.GetHashCode();
        }
	}
}


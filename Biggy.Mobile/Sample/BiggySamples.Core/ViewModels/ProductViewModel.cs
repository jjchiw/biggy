using Cirrious.MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Diagnostics;
using System.Linq;
using Biggy;
using BiggySamples.Core.Services;
using System.Collections.ObjectModel;

namespace BiggySamples.Core.ViewModels
{
	public class ProductViewModel  : MvxViewModel
    {
        private bool _isUpdate = false;

		private string _sku;
		public string Sku
		{ 
			get { return _sku; }
			set { _sku = value; RaisePropertyChanged(() => Sku); }
		}

		public ProductViewModel ()
		{
            _list = new ObservableCollection<ProductModel>();
		}

		public override void Start ()
		{
			foreach (var product in DataContext.Products) {
                var productModel = new ProductModel
                {
                    Sku = product.Sku,
                    Name = product.Name,
                    Price = product.Price,
                    CreatedAt = product.CreatedAt,
                    Master = this
                };
                List.Add(productModel);
			}

			base.Start ();
		}

		private string _name;
		public string Name
		{ 
			get { return _name; }
			set { _name = value; RaisePropertyChanged(() => Name); }
		}

		private decimal _price;
		public decimal Price
		{ 
			get { return _price; }
			set { _price = value; RaisePropertyChanged(() => Price); }
		}

		private DateTime _createdAt;
		public DateTime CreatedAt
		{ 
			get { return _createdAt; }
			set { _createdAt = value; RaisePropertyChanged(() => CreatedAt); }
		}

		public override bool Equals(object obj) {
			var p1 = (ProductViewModel)obj;
			return this.Sku == p1.Sku;
		}

		public override int GetHashCode (){
			return Sku.GetHashCode ();
		}

        private ObservableCollection<ProductModel> _list;
        public ObservableCollection<ProductModel> List
		{ 
			get { return _list; }
			set { _list = value; RaisePropertyChanged(() => List); }
		}

		MvxCommand _addCommand;
		public ICommand AddCommand
		{
			get 
			{
				_addCommand = _addCommand ?? new MvxCommand (DoAddCommand);
				return _addCommand;
			}
		}

        

		Product ToProduct()
		{
			return new Product {
				Sku = Sku,
				Name = Name,
				Price = Price,
				CreatedAt = DateTime.Now,
			};
		}

        ProductModel ToProductModel()
        {
            return new ProductModel
            {
                Sku = Sku,
                Name = Name,
                Price = Price,
                CreatedAt = DateTime.Now,
                Master = this
            };
        }

		void DoAddCommand()
		{
            if (!_isUpdate)
                AddProduct();
            else
                UpdateProduct();
			
            _isUpdate = false;

			Sku = "";
			Name = "";
			Price = 0.0m;
			CreatedAt = DateTime.MinValue;
		}

        void AddProduct()
        {
            var product = this.ToProduct();
            DataContext.Products.Add(product);
            List.Add(this.ToProductModel());
        }

        void UpdateProduct()
        {
            var product = this.ToProduct();
            var productModel = this.ToProductModel();

            var p = DataContext.Products.FirstOrDefault(x => x.Sku == product.Sku);
			if (p == null) {
				AddProduct();
				return;
			}

            DataContext.Products.Update(product);

            var index = List.IndexOf(productModel);

            if (index > -1)
            {
                List.RemoveAt(index);
                List.Insert(index, productModel);
            }

        }

        public void FillProductModel(ProductModel productModel)
        {
            Sku = productModel.Sku;
            Name = productModel.Name;
            Price = productModel.Price;
            _isUpdate = true;
        }
    }

    public class ProductModel  : MvxViewModel
    {
        public string Sku { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public ProductViewModel Master { get; set; }

        MvxCommand _deleteCommand;
        public ICommand DeleteCommand
        {
            get
            {
                _deleteCommand = _deleteCommand ?? new MvxCommand(DoDeleteCommand);
                return _deleteCommand;
            }
        }

        void DoDeleteCommand()
        {
            DataContext.Products.Remove(new Product { Sku = this.Sku });
            Master.List.Remove(this);
        }

        MvxCommand _updateCommand;
        public ICommand UpdateCommand
        {
            get
            {
                _updateCommand = _updateCommand ?? new MvxCommand(DoUpdateCommand);
                return _updateCommand;
            }
        }

        void DoUpdateCommand()
        {
            Master.FillProductModel(this);
        }

        public override bool Equals(object obj)
        {
            var p1 = obj as ProductModel;
            if (p1 == null) return false;

            return p1.Sku == this.Sku;
        }

        public override int GetHashCode()
        {
            return Sku.GetHashCode();
        }
    }
}

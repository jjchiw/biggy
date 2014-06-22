using Biggy.JSON;
using Biggy;
using BiggySamples.Core.Services;
using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.ViewModels;
using Cirrious.MvvmCross.WindowsPhone.Platform;
using Microsoft.Phone.Controls;
using System;
using System.Threading.Tasks;

namespace Biggy.UI.WP8
{
    public class Setup : MvxPhoneSetup
    {
        public Setup(PhoneApplicationFrame rootFrame) : base(rootFrame)
        {
        }

        protected override IMvxApplication CreateApp()
        {
            BiggySamples.Core.Services.DataContext.Products = new BiggyList<Product>(new JsonStore<Product>());

            //Task.Factory.StartNew(async () =>
            //{
            //    var t1 =  BiggySamples.Core.Services.DataContext.Products.LoadItemsAsync();
            //    await t1;
            //});

            BiggySamples.Core.Services.DataContext.Products.LoadItemsAsync();

            return new BiggySamples.Core.App();
            
        }
		
        protected override IMvxTrace CreateDebugTrace()
        {
            return new DebugTrace();
        }
    }
}
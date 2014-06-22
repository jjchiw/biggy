using Android.Content;
using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.Droid.Platform;
using Cirrious.MvvmCross.ViewModels;
using Biggy.JSON;
using BiggySamples.Core.Services;
using System;

namespace Biggy.UI.Droid
{
    public class Setup : MvxAndroidSetup
    {
        public Setup(Context applicationContext) : base(applicationContext)
        {
        }

        protected override IMvxApplication CreateApp()
        {
			var x = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);

			BiggySamples.Core.Services.DataContext.Products = new BiggyList<Product> (new JsonStore<Product> ());

			return new BiggySamples.Core.App ();
        }
		
        protected override IMvxTrace CreateDebugTrace()
        {
            return new DebugTrace();
        }
    }
}
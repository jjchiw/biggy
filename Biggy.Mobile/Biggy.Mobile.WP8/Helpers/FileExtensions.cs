using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy.Mobile.WP8.Helpers
{
    public class FileExtensions
    {
        public static string ReadAllText(string path)
        {
            using (var r = new StreamReader(path))
            {
                return r.ReadToEnd();
            }
        }
    }
}

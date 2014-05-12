using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Administration
{
    public class exelnk
    {
        public string exe { get; set; }
        public string lnk { get; set; }

        public exelnk(string a, string b)
        {
            exe = a;
            lnk = b;
        }
    }
}

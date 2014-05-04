using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Administration
{
    public partial class DepedenciesDialog : Form
    {
        public DepedenciesDialog()
        {
            InitializeComponent();
        }

        public void addData(string[] packList)
        {
            foreach (string line in packList)
            {
                if (line[0] == 'p')
                {
                    string[] tmp = line.Split(' ');
                    checkedListBox1.Items.Add(tmp[1]);
                }
            }
        }

        public string[] getData()
        {
            string[] tmp = new string[200];
            checkedListBox1.CheckedItems.CopyTo(tmp, 0);
            return tmp;
        }
    }
}

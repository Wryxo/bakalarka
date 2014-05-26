using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UserApp
{
    public partial class ProgressForm : Form
    {
        bool inc = true;

        public ProgressForm()
        {
            InitializeComponent();
        }

        private void progressBar1_VisibleChanged(object sender, EventArgs e)
        {
            while (true)
            {
                if (inc) progressBar1.Value += 10;
                else progressBar1.Value -= 10;
                if (progressBar1.Value > 90 || progressBar1.Value < 10) inc = !inc;
            }
        }
    }
}

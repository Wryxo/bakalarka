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
    public partial class ExecutableDialog : Form
    {
        
        public ExecutableDialog()
        {
            InitializeComponent();
            button1.DialogResult = DialogResult.OK;
            button2.DialogResult = DialogResult.Cancel;
        }

        public void addRow(object[] tmp) 
        {
            dataGridView1.Rows.Add(tmp);
        }

        public List<exelnk> getData()
        {
            List<exelnk> a = new List<exelnk>();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if ((bool)row.Cells[2].Value == true) { 
                    a.Add(new exelnk((string)row.Cells[0].Value, (string)row.Cells[1].Value));
                } 
            }
            return a;
        }
    }
}

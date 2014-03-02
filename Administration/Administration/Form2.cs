﻿using System;
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
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            button1.DialogResult = DialogResult.OK;
            button2.DialogResult = DialogResult.Cancel;
        }

        public void nazov(string name)
        {
            label2.Text = name;
            textBox1.Text = name.Substring(0, name.Length - 4);
        }

        public string shortcut()
        {
            return textBox1.Text;
        }
    }
}

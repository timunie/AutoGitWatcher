﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoGitWatcher
{
    public partial class MainForm : Form
    {
        private readonly ViewModel viewModel;
        public MainForm()
        {
            InitializeComponent();

            this.viewModel = new ViewModel();
            this.bindingSourceViewModel.DataSource = this.viewModel;
        }

        private void pbApply_Click(object sender, EventArgs e)
        {
            this.viewModel.Apply();
        }
    }
}
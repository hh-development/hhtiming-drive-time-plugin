using HHTiming.WinFormsControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HHTiming.Blancpain
{
    public partial class GetCarNumberForm : BaseHHForm
    {
        public string CarID
        {
            get
            {
                return tb_CarNumber.Text;
            }
        }

        public GetCarNumberForm()
        {
            InitializeComponent();
            this.DialogResult = DialogResult.Cancel;
        }

        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tb_CarNumber.Text))
            {
                MessageBox.Show("Please enter a car number", "No car entered", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();

        }
    }
}

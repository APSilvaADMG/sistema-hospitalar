namespace FolhaPagamento
{
    partial class Visualizar_FolhaPagamento1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Visualizar_FolhaPagamento1));
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            button1 = new Button();
            txtProcurar = new TextBox();
            btnBuscar = new Button();
            dataGridView1 = new DataGridView();
            label3 = new Label();
            label8 = new Label();
            label1 = new Label();
            pictureBox1 = new PictureBox();
            button2 = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.BackColor = Color.MediumSlateBlue;
            button1.BackgroundImageLayout = ImageLayout.Center;
            button1.Cursor = Cursors.Hand;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            button1.Image = (Image)resources.GetObject("button1.Image");
            button1.Location = new Point(0, 468);
            button1.Name = "button1";
            button1.Size = new Size(75, 37);
            button1.TabIndex = 0;
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // txtProcurar
            // 
            txtProcurar.BackColor = SystemColors.Control;
            txtProcurar.BorderStyle = BorderStyle.FixedSingle;
            txtProcurar.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            txtProcurar.ForeColor = Color.MediumSlateBlue;
            txtProcurar.Location = new Point(267, 103);
            txtProcurar.Name = "txtProcurar";
            txtProcurar.Size = new Size(232, 23);
            txtProcurar.TabIndex = 42;
            // 
            // btnBuscar
            // 
            btnBuscar.BackColor = SystemColors.Control;
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.FlatAppearance.MouseDownBackColor = Color.FromArgb(255, 255, 128);
            btnBuscar.FlatStyle = FlatStyle.Flat;
            btnBuscar.Image = (Image)resources.GetObject("btnBuscar.Image");
            btnBuscar.Location = new Point(505, 100);
            btnBuscar.Name = "btnBuscar";
            btnBuscar.Size = new Size(49, 27);
            btnBuscar.TabIndex = 43;
            btnBuscar.UseVisualStyleBackColor = false;
            btnBuscar.Click += btnBuscar_Click_1;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.BackgroundColor = SystemColors.Control;
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView1.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Cursor = Cursors.IBeam;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Window;
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = Color.MediumSlateBlue;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
            dataGridView1.DefaultCellStyle = dataGridViewCellStyle1;
            dataGridView1.GridColor = Color.MediumSlateBlue;
            dataGridView1.Location = new Point(12, 145);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.BackColor = SystemColors.Control;
            dataGridView1.RowsDefaultCellStyle = dataGridViewCellStyle2;
            dataGridView1.RowTemplate.Height = 25;
            dataGridView1.RowTemplate.ReadOnly = true;
            dataGridView1.RowTemplate.Resizable = DataGridViewTriState.False;
            dataGridView1.Size = new Size(776, 253);
            dataGridView1.TabIndex = 44;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point);
            label3.ForeColor = Color.MediumSlateBlue;
            label3.Location = new Point(248, 47);
            label3.Name = "label3";
            label3.Size = new Size(223, 30);
            label3.TabIndex = 54;
            label3.Text = "Folhas de Pagamento";
            // 
            // label8
            // 
            label8.BackColor = Color.MediumSlateBlue;
            label8.BorderStyle = BorderStyle.FixedSingle;
            label8.Location = new Point(0, 468);
            label8.Name = "label8";
            label8.Size = new Size(800, 42);
            label8.TabIndex = 55;
            // 
            // label1
            // 
            label1.BackColor = Color.MediumSlateBlue;
            label1.BorderStyle = BorderStyle.FixedSingle;
            label1.Location = new Point(0, 0);
            label1.Name = "label1";
            label1.Size = new Size(813, 35);
            label1.TabIndex = 56;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(192, 47);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(32, 32);
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.TabIndex = 57;
            pictureBox1.TabStop = false;
            // 
            // button2
            // 
            button2.BackColor = Color.MediumSlateBlue;
            button2.BackgroundImageLayout = ImageLayout.Center;
            button2.FlatAppearance.BorderSize = 0;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Image = (Image)resources.GetObject("button2.Image");
            button2.Location = new Point(763, 0);
            button2.Name = "button2";
            button2.Size = new Size(37, 35);
            button2.TabIndex = 58;
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            // 
            // Visualizar_FolhaPagamento1
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(800, 505);
            Controls.Add(button2);
            Controls.Add(pictureBox1);
            Controls.Add(label1);
            Controls.Add(label3);
            Controls.Add(dataGridView1);
            Controls.Add(btnBuscar);
            Controls.Add(txtProcurar);
            Controls.Add(button1);
            Controls.Add(label8);
            ForeColor = Color.MediumSlateBlue;
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Visualizar_FolhaPagamento1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "iStorm RH";
            Load += Visualizar_FolhaPagamento1_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private TextBox txtProcurar;
        private Button btnBuscar;
        private DataGridView dataGridView1;
        private Label label3;
        private Label label8;
        private Label label1;
        private PictureBox pictureBox1;
        private Button button2;
    }
}
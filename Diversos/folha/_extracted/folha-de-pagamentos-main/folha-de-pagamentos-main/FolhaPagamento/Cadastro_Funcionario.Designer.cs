namespace FolhaPagamento
{
    partial class iStorm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(iStorm));
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            label9 = new Label();
            label12 = new Label();
            label13 = new Label();
            btnEnviar_Click = new Button();
            button2 = new Button();
            txtNome = new TextBox();
            txtCargo = new TextBox();
            txtEndereco = new TextBox();
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            txtCPF = new MaskedTextBox();
            txtTelefone = new MaskedTextBox();
            txtDataNascimento = new DateTimePicker();
            txtSalario = new NumericUpDown();
            txtDepartamento = new ComboBox();
            txtDataAdmissao = new DateTimePicker();
            label2 = new Label();
            label10 = new Label();
            label11 = new Label();
            pictureBox1 = new PictureBox();
            button1 = new Button();
            ((System.ComponentModel.ISupportInitialize)txtSalario).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label3.ForeColor = Color.MediumSlateBlue;
            label3.Location = new Point(87, 133);
            label3.Name = "label3";
            label3.Size = new Size(44, 15);
            label3.TabIndex = 2;
            label3.Text = "Nome:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label4.ForeColor = Color.MediumSlateBlue;
            label4.Location = new Point(477, 244);
            label4.Name = "label4";
            label4.Size = new Size(42, 15);
            label4.TabIndex = 3;
            label4.Text = "Cargo:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label5.ForeColor = Color.MediumSlateBlue;
            label5.Location = new Point(85, 273);
            label5.Name = "label5";
            label5.Size = new Size(47, 15);
            label5.TabIndex = 4;
            label5.Text = "Salario:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label6.ForeColor = Color.MediumSlateBlue;
            label6.Location = new Point(99, 171);
            label6.Name = "label6";
            label6.Size = new Size(30, 15);
            label6.TabIndex = 5;
            label6.Text = "CPF:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label7.ForeColor = Color.MediumSlateBlue;
            label7.Location = new Point(76, 206);
            label7.Name = "label7";
            label7.Size = new Size(59, 15);
            label7.TabIndex = 6;
            label7.Text = "Telefone:";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label8.ForeColor = Color.MediumSlateBlue;
            label8.Location = new Point(71, 239);
            label8.Name = "label8";
            label8.Size = new Size(62, 15);
            label8.TabIndex = 7;
            label8.Text = "Endereço:";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label9.ForeColor = Color.MediumSlateBlue;
            label9.Location = new Point(414, 127);
            label9.Name = "label9";
            label9.Size = new Size(108, 15);
            label9.TabIndex = 8;
            label9.Text = "Data de Admissão:";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label12.ForeColor = Color.MediumSlateBlue;
            label12.Location = new Point(433, 168);
            label12.Name = "label12";
            label12.Size = new Size(92, 15);
            label12.TabIndex = 11;
            label12.Text = "Departamento:";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label13.ForeColor = Color.MediumSlateBlue;
            label13.Location = new Point(402, 203);
            label13.Name = "label13";
            label13.Size = new Size(122, 15);
            label13.TabIndex = 12;
            label13.Text = "Data de Nascimento:";
            // 
            // btnEnviar_Click
            // 
            btnEnviar_Click.BackColor = Color.MediumSlateBlue;
            btnEnviar_Click.Cursor = Cursors.Hand;
            btnEnviar_Click.FlatStyle = FlatStyle.Flat;
            btnEnviar_Click.ForeColor = Color.MediumSlateBlue;
            btnEnviar_Click.Image = (Image)resources.GetObject("btnEnviar_Click.Image");
            btnEnviar_Click.Location = new Point(725, 415);
            btnEnviar_Click.Name = "btnEnviar_Click";
            btnEnviar_Click.Size = new Size(75, 33);
            btnEnviar_Click.TabIndex = 13;
            btnEnviar_Click.UseVisualStyleBackColor = false;
            btnEnviar_Click.Click += btnEnviar_Click_Click;
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.None;
            button2.AutoSize = true;
            button2.BackColor = Color.MediumSlateBlue;
            button2.Cursor = Cursors.Hand;
            button2.FlatStyle = FlatStyle.Flat;
            button2.ForeColor = Color.MediumSlateBlue;
            button2.Image = (Image)resources.GetObject("button2.Image");
            button2.Location = new Point(-1, 415);
            button2.Name = "button2";
            button2.Size = new Size(75, 33);
            button2.TabIndex = 14;
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            // 
            // txtNome
            // 
            txtNome.BorderStyle = BorderStyle.FixedSingle;
            txtNome.Cursor = Cursors.IBeam;
            txtNome.Location = new Point(136, 127);
            txtNome.Name = "txtNome";
            txtNome.Size = new Size(168, 23);
            txtNome.TabIndex = 16;
            // 
            // txtCargo
            // 
            txtCargo.BorderStyle = BorderStyle.FixedSingle;
            txtCargo.Cursor = Cursors.IBeam;
            txtCargo.Location = new Point(525, 241);
            txtCargo.Name = "txtCargo";
            txtCargo.Size = new Size(121, 23);
            txtCargo.TabIndex = 24;
            // 
            // txtEndereco
            // 
            txtEndereco.BorderStyle = BorderStyle.FixedSingle;
            txtEndereco.Location = new Point(136, 236);
            txtEndereco.Name = "txtEndereco";
            txtEndereco.Size = new Size(168, 23);
            txtEndereco.TabIndex = 19;
            // 
            // txtCPF
            // 
            txtCPF.BorderStyle = BorderStyle.FixedSingle;
            txtCPF.Cursor = Cursors.IBeam;
            txtCPF.Location = new Point(136, 168);
            txtCPF.Mask = "00000000000";
            txtCPF.Name = "txtCPF";
            txtCPF.Size = new Size(82, 23);
            txtCPF.TabIndex = 17;
            // 
            // txtTelefone
            // 
            txtTelefone.BorderStyle = BorderStyle.FixedSingle;
            txtTelefone.Cursor = Cursors.IBeam;
            txtTelefone.Location = new Point(136, 203);
            txtTelefone.Mask = "00000000000";
            txtTelefone.Name = "txtTelefone";
            txtTelefone.Size = new Size(71, 23);
            txtTelefone.TabIndex = 18;
            // 
            // txtDataNascimento
            // 
            txtDataNascimento.CalendarFont = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            txtDataNascimento.CalendarForeColor = Color.MediumSlateBlue;
            txtDataNascimento.CalendarTitleForeColor = Color.MediumSlateBlue;
            txtDataNascimento.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            txtDataNascimento.Format = DateTimePickerFormat.Short;
            txtDataNascimento.Location = new Point(525, 202);
            txtDataNascimento.Name = "txtDataNascimento";
            txtDataNascimento.Size = new Size(121, 23);
            txtDataNascimento.TabIndex = 23;
            // 
            // txtSalario
            // 
            txtSalario.BorderStyle = BorderStyle.FixedSingle;
            txtSalario.Location = new Point(136, 271);
            txtSalario.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            txtSalario.Name = "txtSalario";
            txtSalario.Size = new Size(168, 23);
            txtSalario.TabIndex = 20;
            // 
            // txtDepartamento
            // 
            txtDepartamento.Cursor = Cursors.IBeam;
            txtDepartamento.FormattingEnabled = true;
            txtDepartamento.Items.AddRange(new object[] { "Operacional", "Gestao", "Administrativo", "RH" });
            txtDepartamento.Location = new Point(525, 165);
            txtDepartamento.Name = "txtDepartamento";
            txtDepartamento.Size = new Size(121, 23);
            txtDepartamento.TabIndex = 22;
            // 
            // txtDataAdmissao
            // 
            txtDataAdmissao.CalendarFont = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            txtDataAdmissao.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            txtDataAdmissao.Format = DateTimePickerFormat.Short;
            txtDataAdmissao.Location = new Point(525, 124);
            txtDataAdmissao.Name = "txtDataAdmissao";
            txtDataAdmissao.Size = new Size(121, 23);
            txtDataAdmissao.TabIndex = 21;
            // 
            // label2
            // 
            label2.BackColor = Color.MediumSlateBlue;
            label2.BorderStyle = BorderStyle.FixedSingle;
            label2.Location = new Point(-1, -2);
            label2.Name = "label2";
            label2.Size = new Size(801, 35);
            label2.TabIndex = 39;
            // 
            // label10
            // 
            label10.BackColor = Color.MediumSlateBlue;
            label10.BorderStyle = BorderStyle.FixedSingle;
            label10.Location = new Point(-1, 413);
            label10.Name = "label10";
            label10.Size = new Size(801, 37);
            label10.TabIndex = 40;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point);
            label11.ForeColor = Color.MediumSlateBlue;
            label11.Location = new Point(255, 45);
            label11.Name = "label11";
            label11.Size = new Size(252, 30);
            label11.TabIndex = 41;
            label11.Text = "Cadastro de Funcionario";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(217, 45);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(32, 32);
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.TabIndex = 45;
            pictureBox1.TabStop = false;
            // 
            // button1
            // 
            button1.BackColor = Color.MediumSlateBlue;
            button1.BackgroundImageLayout = ImageLayout.Center;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Image = (Image)resources.GetObject("button1.Image");
            button1.Location = new Point(763, -2);
            button1.Name = "button1";
            button1.Size = new Size(37, 35);
            button1.TabIndex = 46;
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // iStorm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(button1);
            Controls.Add(pictureBox1);
            Controls.Add(label11);
            Controls.Add(btnEnviar_Click);
            Controls.Add(button2);
            Controls.Add(label10);
            Controls.Add(label2);
            Controls.Add(txtDataAdmissao);
            Controls.Add(txtDepartamento);
            Controls.Add(txtSalario);
            Controls.Add(txtDataNascimento);
            Controls.Add(txtTelefone);
            Controls.Add(txtCPF);
            Controls.Add(txtEndereco);
            Controls.Add(txtCargo);
            Controls.Add(txtNome);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label13);
            Controls.Add(label12);
            Controls.Add(label9);
            Controls.Add(label8);
            Cursor = Cursors.Hand;
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "iStorm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "iStorm RH";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)txtSalario).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private Label label9;
        private Label label12;
        private Label label13;
        private Button btnEnviar_Click;
        private Button button2;
        private TextBox txtNome;
        private TextBox txtCargo;
        private TextBox txtEndereco;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private MaskedTextBox txtCPF;
        private MaskedTextBox txtTelefone;
        private DateTimePicker txtDataNascimento;
        private NumericUpDown txtSalario;
        private ComboBox txtDepartamento;
        private DateTimePicker txtDataAdmissao;
        private Label label2;
        private Label label10;
        private Label label11;
        private PictureBox pictureBox1;
        private Button button1;
    }
}
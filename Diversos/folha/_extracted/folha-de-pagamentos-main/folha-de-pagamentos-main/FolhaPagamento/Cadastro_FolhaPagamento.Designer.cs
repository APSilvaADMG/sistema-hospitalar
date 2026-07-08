namespace FolhaPagamento
{
    partial class Cadastro_FolhaPagamento
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Cadastro_FolhaPagamento));
            button1 = new Button();
            btnEnviar_Click = new Button();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            txtFuncionario = new TextBox();
            txtImposto = new TextBox();
            txtBonus = new TextBox();
            txtHorasTra = new TextBox();
            txtDataVi = new DateTimePicker();
            label8 = new Label();
            label14 = new Label();
            label1 = new Label();
            pictureBox1 = new PictureBox();
            button2 = new Button();
            textUsuario = new TextBox();
            label2 = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.BackColor = Color.MediumSlateBlue;
            button1.BackgroundImageLayout = ImageLayout.Center;
            button1.Cursor = Cursors.Hand;
            button1.FlatStyle = FlatStyle.Flat;
            button1.ForeColor = Color.MediumSlateBlue;
            button1.Image = (Image)resources.GetObject("button1.Image");
            button1.Location = new Point(0, 415);
            button1.Name = "button1";
            button1.Size = new Size(75, 35);
            button1.TabIndex = 0;
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // btnEnviar_Click
            // 
            btnEnviar_Click.BackColor = Color.MediumSlateBlue;
            btnEnviar_Click.BackgroundImageLayout = ImageLayout.Center;
            btnEnviar_Click.Cursor = Cursors.Hand;
            btnEnviar_Click.FlatStyle = FlatStyle.Flat;
            btnEnviar_Click.ForeColor = Color.MediumSlateBlue;
            btnEnviar_Click.Image = (Image)resources.GetObject("btnEnviar_Click.Image");
            btnEnviar_Click.Location = new Point(726, 415);
            btnEnviar_Click.Name = "btnEnviar_Click";
            btnEnviar_Click.Size = new Size(75, 35);
            btnEnviar_Click.TabIndex = 14;
            btnEnviar_Click.UseVisualStyleBackColor = false;
            btnEnviar_Click.Click += btnEnviar_Click_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.ForeColor = Color.MediumSlateBlue;
            label3.Location = new Point(274, 131);
            label3.Name = "label3";
            label3.Size = new Size(70, 15);
            label3.TabIndex = 39;
            label3.Text = "Funcionario";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.ForeColor = Color.MediumSlateBlue;
            label4.Location = new Point(293, 188);
            label4.Name = "label4";
            label4.Size = new Size(51, 15);
            label4.TabIndex = 40;
            label4.Text = "Imposto";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.ForeColor = Color.MediumSlateBlue;
            label5.Location = new Point(243, 221);
            label5.Name = "label5";
            label5.Size = new Size(103, 15);
            label5.TabIndex = 41;
            label5.Text = "Horas Trabalhadas";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.ForeColor = Color.MediumSlateBlue;
            label6.Location = new Point(304, 259);
            label6.Name = "label6";
            label6.Size = new Size(40, 15);
            label6.TabIndex = 42;
            label6.Text = "Bonus";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.ForeColor = Color.MediumSlateBlue;
            label7.Location = new Point(265, 297);
            label7.Name = "label7";
            label7.Size = new Size(79, 15);
            label7.TabIndex = 43;
            label7.Text = "Data Vigencia";
            // 
            // txtFuncionario
            // 
            txtFuncionario.BorderStyle = BorderStyle.FixedSingle;
            txtFuncionario.Cursor = Cursors.IBeam;
            txtFuncionario.Location = new Point(350, 129);
            txtFuncionario.Name = "txtFuncionario";
            txtFuncionario.Size = new Size(159, 23);
            txtFuncionario.TabIndex = 44;
            // 
            // txtImposto
            // 
            txtImposto.BorderStyle = BorderStyle.FixedSingle;
            txtImposto.Cursor = Cursors.IBeam;
            txtImposto.Location = new Point(352, 189);
            txtImposto.Name = "txtImposto";
            txtImposto.Size = new Size(100, 23);
            txtImposto.TabIndex = 45;
            // 
            // txtBonus
            // 
            txtBonus.BorderStyle = BorderStyle.FixedSingle;
            txtBonus.Cursor = Cursors.IBeam;
            txtBonus.Location = new Point(352, 251);
            txtBonus.Name = "txtBonus";
            txtBonus.Size = new Size(100, 23);
            txtBonus.TabIndex = 47;
            // 
            // txtHorasTra
            // 
            txtHorasTra.BorderStyle = BorderStyle.FixedSingle;
            txtHorasTra.Cursor = Cursors.IBeam;
            txtHorasTra.Location = new Point(352, 218);
            txtHorasTra.Name = "txtHorasTra";
            txtHorasTra.Size = new Size(100, 23);
            txtHorasTra.TabIndex = 48;
            // 
            // txtDataVi
            // 
            txtDataVi.Cursor = Cursors.IBeam;
            txtDataVi.Format = DateTimePickerFormat.Short;
            txtDataVi.Location = new Point(350, 291);
            txtDataVi.Name = "txtDataVi";
            txtDataVi.Size = new Size(102, 23);
            txtDataVi.TabIndex = 50;
            // 
            // label8
            // 
            label8.BackColor = Color.MediumSlateBlue;
            label8.BorderStyle = BorderStyle.FixedSingle;
            label8.Location = new Point(0, 0);
            label8.Name = "label8";
            label8.Size = new Size(801, 35);
            label8.TabIndex = 51;
            // 
            // label14
            // 
            label14.BackColor = Color.MediumSlateBlue;
            label14.BorderStyle = BorderStyle.FixedSingle;
            label14.Location = new Point(0, 415);
            label14.Name = "label14";
            label14.Size = new Size(801, 35);
            label14.TabIndex = 52;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point);
            label1.ForeColor = Color.MediumSlateBlue;
            label1.Location = new Point(259, 64);
            label1.Name = "label1";
            label1.Size = new Size(306, 30);
            label1.TabIndex = 53;
            label1.Text = "Cadastro Folha de Pagamento";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(221, 64);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(32, 32);
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.TabIndex = 54;
            pictureBox1.TabStop = false;
            // 
            // button2
            // 
            button2.BackColor = Color.MediumSlateBlue;
            button2.BackgroundImageLayout = ImageLayout.Center;
            button2.FlatAppearance.BorderSize = 0;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Image = (Image)resources.GetObject("button2.Image");
            button2.Location = new Point(764, 0);
            button2.Name = "button2";
            button2.Size = new Size(37, 35);
            button2.TabIndex = 55;
            button2.UseVisualStyleBackColor = false;
            // 
            // textUsuario
            // 
            textUsuario.BorderStyle = BorderStyle.FixedSingle;
            textUsuario.Cursor = Cursors.IBeam;
            textUsuario.Location = new Point(352, 158);
            textUsuario.Name = "textUsuario";
            textUsuario.Size = new Size(100, 23);
            textUsuario.TabIndex = 56;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.ForeColor = Color.MediumSlateBlue;
            label2.Location = new Point(293, 160);
            label2.Name = "label2";
            label2.Size = new Size(47, 15);
            label2.TabIndex = 57;
            label2.Text = "Usuario";
            // 
            // Cadastro_FolhaPagamento
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label2);
            Controls.Add(textUsuario);
            Controls.Add(button2);
            Controls.Add(pictureBox1);
            Controls.Add(label1);
            Controls.Add(label8);
            Controls.Add(txtDataVi);
            Controls.Add(txtHorasTra);
            Controls.Add(txtBonus);
            Controls.Add(txtImposto);
            Controls.Add(txtFuncionario);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(btnEnviar_Click);
            Controls.Add(button1);
            Controls.Add(label14);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Cadastro_FolhaPagamento";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "iStorm RH";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private Button btnEnviar_Click;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private TextBox txtFuncionario;
        private TextBox txtImposto;
        private TextBox txtBonus;
        private TextBox txtHorasTra;
        private DateTimePicker txtDataVi;
        private Label label8;
        private Label label14;
        private Label label1;
        private PictureBox pictureBox1;
        private Button button2;
        private TextBox textUsuario;
        private Label label2;
    }
}
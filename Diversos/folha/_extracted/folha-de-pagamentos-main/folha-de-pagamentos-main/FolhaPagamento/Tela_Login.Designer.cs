namespace FolhaPagamento
{
    partial class Tela_Login
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Tela_Login));
            button1 = new Button();
            txtUser = new TextBox();
            txtPass = new TextBox();
            button5 = new Button();
            button3 = new Button();
            button2 = new Button();
            label14 = new Label();
            label4 = new Label();
            pictureBox1 = new PictureBox();
            label2 = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.BackColor = SystemColors.Control;
            button1.Cursor = Cursors.Hand;
            button1.FlatAppearance.BorderColor = Color.MediumSlateBlue;
            button1.FlatStyle = FlatStyle.Flat;
            button1.ForeColor = Color.MediumSlateBlue;
            button1.Image = (Image)resources.GetObject("button1.Image");
            button1.Location = new Point(340, 262);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 0;
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click_1;
            // 
            // txtUser
            // 
            txtUser.BorderStyle = BorderStyle.FixedSingle;
            txtUser.Cursor = Cursors.IBeam;
            txtUser.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            txtUser.ForeColor = Color.MediumSlateBlue;
            txtUser.Location = new Point(326, 182);
            txtUser.Name = "txtUser";
            txtUser.Size = new Size(100, 23);
            txtUser.TabIndex = 3;
            // 
            // txtPass
            // 
            txtPass.BorderStyle = BorderStyle.FixedSingle;
            txtPass.Cursor = Cursors.IBeam;
            txtPass.Location = new Point(326, 233);
            txtPass.Name = "txtPass";
            txtPass.PasswordChar = '•';
            txtPass.Size = new Size(100, 23);
            txtPass.TabIndex = 4;
            // 
            // button5
            // 
            button5.BackColor = Color.Transparent;
            button5.FlatAppearance.BorderSize = 0;
            button5.FlatStyle = FlatStyle.Flat;
            button5.Image = (Image)resources.GetObject("button5.Image");
            button5.Location = new Point(287, 226);
            button5.Name = "button5";
            button5.Size = new Size(33, 32);
            button5.TabIndex = 43;
            button5.UseVisualStyleBackColor = false;
            // 
            // button3
            // 
            button3.BackColor = Color.Transparent;
            button3.FlatAppearance.BorderSize = 0;
            button3.FlatStyle = FlatStyle.Flat;
            button3.Image = (Image)resources.GetObject("button3.Image");
            button3.Location = new Point(287, 175);
            button3.Name = "button3";
            button3.Size = new Size(33, 32);
            button3.TabIndex = 41;
            button3.UseVisualStyleBackColor = false;
            // 
            // button2
            // 
            button2.BackColor = Color.MediumSlateBlue;
            button2.BackgroundImageLayout = ImageLayout.Center;
            button2.FlatAppearance.BorderSize = 0;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Image = (Image)resources.GetObject("button2.Image");
            button2.Location = new Point(763, -3);
            button2.Name = "button2";
            button2.Size = new Size(37, 35);
            button2.TabIndex = 7;
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            // 
            // label14
            // 
            label14.BackColor = Color.MediumSlateBlue;
            label14.BorderStyle = BorderStyle.FixedSingle;
            label14.Location = new Point(-1, -3);
            label14.Name = "label14";
            label14.Size = new Size(801, 35);
            label14.TabIndex = 37;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point);
            label4.Location = new Point(326, 131);
            label4.Name = "label4";
            label4.Size = new Size(114, 30);
            label4.TabIndex = 39;
            label4.Text = "iStorm RH";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(279, 131);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(32, 32);
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.TabIndex = 44;
            pictureBox1.TabStop = false;
            // 
            // label2
            // 
            label2.BackColor = Color.MediumSlateBlue;
            label2.BorderStyle = BorderStyle.FixedSingle;
            label2.Location = new Point(-1, 416);
            label2.Name = "label2";
            label2.Size = new Size(801, 35);
            label2.TabIndex = 46;
            // 
            // Tela_Login
            // 
            AutoScaleMode = AutoScaleMode.None;
            BackColor = SystemColors.Control;
            BackgroundImageLayout = ImageLayout.Center;
            ClientSize = new Size(800, 450);
            Controls.Add(label2);
            Controls.Add(button2);
            Controls.Add(pictureBox1);
            Controls.Add(button5);
            Controls.Add(label4);
            Controls.Add(button3);
            Controls.Add(label14);
            Controls.Add(button1);
            Controls.Add(txtPass);
            Controls.Add(txtUser);
            ForeColor = Color.MediumSlateBlue;
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Tela_Login";
            RightToLeft = RightToLeft.No;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "iStorm RH";
            Load += Tela_Login_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private TextBox txtUser;
        private TextBox txtPass;
        private Button button2;
        private Label label14;
        private Label label4;
        private Button button5;
        private Button button3;
        private PictureBox pictureBox1;
        private Label label2;
    }
}
using System;
using System.Windows.Forms;

namespace FolhaPagamento
{
    public partial class Tela_Inicial : Form
    {
        public Tela_Inicial()
        {
            InitializeComponent();
        }

        // Evento de carregamento do formulário
        private void Tela_Inicial_Load(object sender, EventArgs e)
        {
            // Código associado ao carregamento do formulário, se necessário
        }

        // Método executado ao clicar no botão "Acessar Cadastro"
        private void btnAcessarCadastro_Click_1(object sender, EventArgs e)
        {
            try
            {
                // Oculta a tela inicial
                this.Hide();

                // Abre o formulário de cadastro de funcionários
                iStorm formCadastro = new iStorm();
                formCadastro.FormClosed += (s, args) => this.Show(); // Exibe a tela inicial quando o formulário de cadastro é fechado
                formCadastro.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocorreu um erro ao acessar o cadastro: " + ex.Message);
            }
        }

        // Método executado ao clicar no botão "Logout"
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Exibe uma mensagem de confirmação
                DialogResult result = MessageBox.Show("Tem certeza de que deseja fazer logout?", "Confirmação de Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                // Se o usuário confirmar o logout, fecha a tela atual e exibe a tela de login
                if (result == DialogResult.Yes)
                {
                    this.Close(); // Fecha a tela atual
                    Tela_Login formLogin = new Tela_Login();
                    formLogin.Show(); // Exibe a tela de login
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocorreu um erro ao fazer logout: " + ex.Message);
            }
        }

        // Método executado ao clicar no botão "Visualizar Funcionários"
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                // Oculta a tela inicial
                this.Hide();

                // Abre o formulário de visualização de funcionários
                Visualizar_Funcionarios1 formVisualizar = new Visualizar_Funcionarios1();
                formVisualizar.FormClosed += (s, args) => this.Show(); // Exibe a tela inicial quando o formulário de visualização é fechado
                formVisualizar.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocorreu um erro ao visualizar os funcionários: " + ex.Message);
            }
        }

        // Método executado ao clicar no botão "Visualizar Folha de Pagamento"
        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                // Oculta a tela inicial
                this.Hide();

                // Abre o formulário de visualização de folha de pagamento
                Visualizar_FolhaPagamento1 formVisualizar2 = new Visualizar_FolhaPagamento1();
                formVisualizar2.FormClosed += (s, args) => this.Show(); // Exibe a tela inicial quando o formulário de visualização é fechado
                formVisualizar2.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocorreu um erro ao visualizar a folha de pagamento: " + ex.Message);
            }
        }

        // Método executado ao clicar no botão "Cadastrar Folha de Pagamento"
        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                // Oculta a tela inicial
                this.Hide();

                // Abre o formulário de cadastro de folha de pagamento
                Cadastro_FolhaPagamento formFolhaPagamento = new Cadastro_FolhaPagamento();
                formFolhaPagamento.FormClosed += (s, args) => this.Show(); // Exibe a tela inicial quando o formulário de cadastro é fechado
                formFolhaPagamento.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocorreu um erro ao cadastrar a folha de pagamento: " + ex.Message);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}

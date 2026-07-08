using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace FolhaPagamento
{
    public partial class Tela_Login : Form
    {
        private const string apiUrl = "https://pimbackend.onrender.com/auth/login"; // URL da sua API

        public Tela_Login()
        {
            InitializeComponent();
        }

        // Método executado ao clicar no botão "Entrar"
        private async void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                // Constrói o objeto com os dados do usuário
                var dados = new
                {
                    email = txtUser.Text,
                    senha = txtPass.Text,
                };

                // Serializa o objeto para formato JSON
                var dadosJson = JsonConvert.SerializeObject(dados);

                // Enviar a solicitação POST para a API
                using (var httpClient = new HttpClient())
                {
                    var content = new StringContent(dadosJson, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(apiUrl, content);

                    // Verifica se a resposta foi bem-sucedida
                    if (response.IsSuccessStatusCode)
                    {
                        // Lê o conteúdo da resposta como uma string JSON
                        var respostaConteudo = await response.Content.ReadAsStringAsync();
                        dynamic json = JsonConvert.DeserializeObject(respostaConteudo);

                        // Verifica o tipo de usuário retornado pela API
                        if (json.tipo == 2)
                        {
                            // Login bem-sucedido
                            MostrarMensagemStatus("Status do login: Sucesso.");

                            // Oculta o formulário de login e exibe o formulário inicial
                            this.Hide();
                            Tela_Inicial formInicial = new Tela_Inicial();
                            formInicial.ShowDialog();
                        }
                        else
                        {
                            // Login falhou
                            MostrarMensagemStatus("Status do login: Falha. Apenas gerentes de departamento podem acessar!.");
                        }

                        LimparCampos(); // Limpa os campos do formulário após o login
                    }
                    else
                    {
                        MessageBox.Show("Status do login: Falha Usuario nao cadastrado.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocorreu um erro: " + ex.Message);
            }
        }

        // Método para exibir a mensagem de status usando MessageBox
        private void MostrarMensagemStatus(string mensagem)
        {
            MessageBox.Show(mensagem, "Mensagem de Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Método para limpar os campos do formulário
        private void LimparCampos()
        {
            // Limpar os campos do formulário após o cadastro
            txtUser.Text = string.Empty;
            txtPass.Text = string.Empty;

            // Limpar outros campos conforme necessário
        }

        // Método executado ao clicar no botão "Sair"
        private void button2_Click(object sender, EventArgs e)
        {
            // Fechar o aplicativo quando o botão de saída for clicado
            Application.Exit();
        }

        private void Tela_Login_Load(object sender, EventArgs e)
        {

        }

        // Outros métodos relacionados a eventos e UI podem ser documentados conforme necessário
    }
}

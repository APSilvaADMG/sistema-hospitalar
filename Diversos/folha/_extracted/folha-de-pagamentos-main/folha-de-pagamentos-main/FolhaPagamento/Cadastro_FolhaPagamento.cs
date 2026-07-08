using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;

namespace FolhaPagamento
{
    public partial class Cadastro_FolhaPagamento : Form
    {
        private const string apiUrl = "https://pimbackend.onrender.com/folhas_pagamento"; // Substitua pela URL da sua API

        public Cadastro_FolhaPagamento()
        {
            InitializeComponent();
        }

        // Método para limpar os campos do formulário após o cadastro
        private void LimparCampos()
        {
            txtFuncionario.Text = string.Empty;
            txtImposto.Text = string.Empty;
            txtHorasTra.Text = string.Empty;
            txtBonus.Text = string.Empty;
            txtDataVi.Text = string.Empty;
            textUsuario.Text = string.Empty;
            // Limpar outros campos conforme necessário
        }

        // Método executado ao clicar no botão "Enviar"
        private async void btnEnviar_Click_Click(object sender, EventArgs e)
        {
            try
            {
                // Cria um objeto FolhaPagamento com base nos dados do formulário
                var novaFolhaPagamento = new
                {

                    funcionario = txtFuncionario.Text,
                    imposto = txtImposto.Text,
                    horasTrabalhadas = txtHorasTra.Text,
                    bonus = txtBonus.Text,
                    data_vigencia = Convert.ToDateTime(txtDataVi.Text),
                    usuario = textUsuario.Text,
                };

                // Converte o objeto FolhaPagamento para JSON
                var jsonFolhaPagamento = Newtonsoft.Json.JsonConvert.SerializeObject(novaFolhaPagamento);

                // Envia a solicitação POST para a API
                using (var httpClient = new HttpClient())
                {
                    var content = new StringContent(jsonFolhaPagamento, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(apiUrl, content);

                    // Verifica se a resposta foi bem-sucedida
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Folha de Pagamento cadastrada com sucesso!");
                        LimparCampos();
                    }
                    else
                    {
                        MessageBox.Show("Erro ao cadastrar Folha de Pagamento.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocorreu um erro: " + ex.Message);
            }
        }

        // Método executado ao clicar no botão "Voltar"
        private void button1_Click(object sender, EventArgs e)
        {
            // Fecha todas as instâncias da tela atual
            foreach (Form form in Application.OpenForms)
            {
                if (form is Cadastro_FolhaPagamento)
                {
                    form.Close();
                    break; // Sair do loop depois de fechar a primeira instância (se houver mais de uma)
                }
            }

            // Verifica se a tela inicial já está aberta antes de criá-la novamente
            Tela_Inicial formLogin = Application.OpenForms.OfType<Tela_Inicial>().FirstOrDefault();

            if (formLogin == null)
            {
                // Se não estiver aberta, cria uma nova instância e a exibe
                formLogin = new Tela_Inicial();
                formLogin.Show();
            }
            else
            {
                // Se já estiver aberta, traz para a frente
                formLogin.BringToFront();
            }
        }
    }
}

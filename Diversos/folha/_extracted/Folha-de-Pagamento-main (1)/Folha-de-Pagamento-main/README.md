# 📄 Sistema de Gestão de Folha de Pagamento   
   

# Sobre   
Este é um software interno para gestão de funcionários e processamento de folha de pagamento. O projeto foi desenvolvido com foco em escalabilidade comercial e robustez, aplicando as diretrizes de Engenharia de Software.

```Progresso:
    SDLC - Software Development Life Cycle(Ciclo de vida de desenvolvimento de software)  
        1. Definição de Objetivos ✔️  
        2. Levantamento e Análise de Requisitos ✔️  
            1. Requisitos Funcionais ✔️  
            2. Requisitos Não Funcionais ✔️    
        3. Design e Arquitetura(Em progresso)   
            1. Arquitetura de Pastas ✔️  
            2. Modelagem de Dados 🏗️ 
            3. Design de UI  🏗️

        4. Implementação(Em progresso)  
        5. Manutenção(Planejado)  
```
# 🛠️ Tecnologias Utilizadas

   - Linguagem: Python 3.10.1

   - Interface Gráfica: PyQt5

   - Banco de Dados: SQLite3

   - Documentação Legal: Consultoria via regulamentos da Previdência e CLT.

# ⚖️ Base Legal e Regras de Negócio
O sistema é norteado pela legislação brasileira vigente:

* Decreto 3.048/1999 (Art. 225): Obrigatoriedade da elaboração mensal da folha, incluindo todos os segurados e parcelas devidas.

* Artigo 462 da CLT: "ao empregador é vedado efetuar qualquer desconto nos salários do empregado,
salvo quando este resultar de adiantamentos, de dispositivos de lei ou de contrato coletivo".

Indicadores Processados:
* Encargos Sociais: INSS, IRRF, FGTS.
* Benefícios e Proventos: Salário Base, VR, Férias, 13º Salário.

----------------------------------------------------------------------------------

# ----> Informações Administrativas e funções
```
# ----> Sistema de autenticação
    * Cadastro de login
    * login
    * banco de dados

# ----> Cadastro da Empresa e Funcionario Separados
 # 1 - Dados da empresa:
        Nome da empresa
        nome fantasia
        CNPJ
        Endereço
            CEP
            Rua Nº
            Cidade
            Estado
            Pais
        1.1 salvar no Banco de dados
        1.2 Emitir Relatorio para:
            visualização pdf
            Impressão

 # 2 -  Cadastro Informaçoes pessoais do funcionario
        Nome
        Data de nascimento
        Idade
        Data de admição
        Cargo
        Endereço
            CEP
            Rua Nº
            Cidade
            Estado
            Pais
    2.1 salvar no Banco de dados
        1.2 Emitir Relatorio para:
            visualização pdf
            Impressão
            Gerar Relatorio Selecionando Data
(No mês escolhido gerar Salario, valor total do VR, valor das Ferias e data de vencimento e o proximo 13º)
```    
# 📌 Para instalar Dependências em Python
1. Primeiro, certifique-se de instalar Python 3.10.1 no seu sistema local.
 [Clique aqui para baixar Python](https://www.python.org/downloads/)

2. Vá até sua pasta de código e no terminal, ativar o ambiente virtual. 
    venv\Scripts\activate.ps1  no Windows powershell
    source venv/bin/activate no macOS/Linux

3. Para a Biblioteca GUI 

    pip install PyQt5

# 💻 Para execultar em seu sistema local.

# ---->Arquiteutra de Pastas
```
folha_pagamento/
├── venv/
├── data/
│   └── database.db          # Onde os dados dos funcionários moram
├── assets/
│   └── icons/               # Ícones de usuário, dinheiro, etc.
├── src/
│   ├── main.py              # Inicia o app
│   ├── ui/                  # Arquivos .ui do Qt Designer
│   │   ├── login.ui
│   │   └── funcionarios.ui
│   ├── views/               # Lógica da Interface (Frontend Desktop)
│   │   ├── login_view.py    # Captura o clique do botão
│   │   ├── register_view.py
│   │   └── dashboard_view.py
│   └── backend/             # O "CÉREBRO" DO APP
│       ├── database.py      # Comandos SQL (INSERT, SELECT)
│       ├── calculations.py      # Lógica da Folha (INSS, IRRF, FGTS)
│       └── validations.py    # Verifica CPF, PIS, etc.
├── requirements.txt
└── .gitignore

```


<a id="Creditos"></a>
## 🏆 Créditos
> Todo o projeto foi feito por...
  
<br /> 

<div > 

| [<img src="https://avatars.githubusercontent.com/u/139656375?v=4" width=300><br><sub> Felipe Lima </sub>](https://www.linkedin.com/in/felipenlim/) | *** Oi, Se você chegou até aqui, acredito que gostou do meu projeto, nesse caso temos algo em comum, sendo assim que tal conversamos um pouco? Entra em contato no linkedin *** | 
|---|---|

</div> 
  
<br /> 
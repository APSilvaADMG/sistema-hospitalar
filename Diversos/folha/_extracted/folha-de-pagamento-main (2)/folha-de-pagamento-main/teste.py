funcionario={}
while True:
    print("[1]Cadastrar\n[2]Remover Funcionario\n[3]Exibir funcionarios\n[4]Inserir falta")
    TelaInicial=int(input("Opção Desejada: "))

    if TelaInicial==1:
        print("_"*55)
        print("\t   Cadastrar Funcionario")
        print("-"*55)
        quant_funcionario=int(input("\nDigite quantos funcionarios q adicionar: "))

        

        for i in range(quant_funcionario):
            codigo_funcao=int(input("codigo da função "))

        if codigo_funcao==101 or codigo_funcao ==102:
            matricula=input("digite a matricula ")
            nome=input("Digite o Nome ")
            faltas=0
            if codigo_funcao==101:
                salario_bruto=1500
            else:
                salario_bruto=6950
            funcionario[matricula]={
                                        'nome':nome,
                                        'codigo_funcao':codigo_funcao,
                                        'faltas':faltas,
                                        'salario_bruto':salario_bruto}
        for matricula in funcionario:
            cadastro=funcionario[matricula]
            print("_"*55)
            print("\t   Cadastro Bem Suedido")
            print("-"*55)
            print("Nome\tNome\tCodigo_Função\tFaltas\tSalario_Bruto")
            print(f"{matricula}\t{cadastro['nome']}\t{cadastro['codigo_funcao']}\t\t{cadastro['faltas']}\t{cadastro['salario_bruto']}\n")
            TelaInicial=0
        
    if TelaInicial==2:
        excluir=input("Digite a matricula do funcuionario: ")
        if excluir in funcionario:
            cadastro=funcionario[excluir]
            print("Nome\tNome\tCodigo_Função\tFaltas\tSalario_Bruto")
            print(f"{excluir}\t{cadastro['nome']}\t{cadastro['codigo_funcao']}\t\t{cadastro['faltas']}\t{cadastro['salario_bruto']}")
            print("[1]Confirmar\n[0]Voltar")
            Confirmação=int(input("Opção Desejada: "))
            if Confirmação==1:
                del funcionario[excluir]
                print("_"*55)
                print("\t   Funcionario Deletado")
                print("-"*55)
                TelaInicial=0
    if TelaInicial==3:
        print("_" * 55)
        print("\t   Funcionarios Cadastrados")
        print("-" * 55)
        print("Matricula\tNome\tCodigo_Função\tFaltas\tSalario_Bruto")
        for matricula, cadastro in funcionario.items():
            print(f"{matricula}\t{cadastro['nome']}\t{cadastro['codigo_funcao']}\t{cadastro['faltas']}\t{cadastro['salario_bruto']}")
        print("\n[0]Voltar")
        Confirmação=int(input("Opção Desejada: "))
        if Confirmação==0:
            TelaInicial=0
    if TelaInicial==4:
        print("_" * 55)
        print("\t   Painel De Faltas")
        print("-" * 55)
        codigo_funcao=input("Digite o Codigo Do Funcionario: ")
        
        matricula=input("Digite a Matricula do funcionario: ")

        if codigo_funcao in ['101', '102']:
            matricula = input("Digite a Matricula do funcionario: ")

        if matricula in funcionario:
            cadastro = funcionario[matricula]
            cadastro['faltas'] += 1  # Incrementa as faltas
            print("Matricula\tNome\tCodigo_Função\tFaltas\tSalario_Bruto")

            if codigo_funcao == '101':
                cadastro['salario_bruto'] -= 50  # Reduz o salário bruto
            if codigo_funcao == '102':
                cadastro['salario_bruto'] -= 232  # Reduz o salário bruto
            print(f"{matricula}\t{cadastro['nome']}\t{cadastro['codigo_funcao']}\t\t{cadastro['faltas']}\t{cadastro['salario_bruto']}")
            
import sys
from PyQt5.QtWidgets import (QApplication, QWidget, QLabel, QLineEdit, 
                             QPushButton, QVBoxLayout, QMessageBox)
from PyQt5.QtCore import Qt

class LoginView(QWidget):
    def __init__(self):
        super().__init__()
        self.init_ui()

    def init_ui(self):
        # Configurações da Janela
        self.setWindowTitle('RH - Login')
        self.setFixedSize(300, 200) # Largura e Altura fixas

        # Widgets
        self.label_usuario = QLabel('Usuário:')
        self.input_usuario = QLineEdit()
        self.input_usuario.setPlaceholderText('Digite seu login')

        self.label_senha = QLabel('Senha:')
        self.input_senha = QLineEdit()
        self.input_senha.setEchoMode(QLineEdit.Password) # Mascarar senha
        self.input_senha.setPlaceholderText('Digite sua senha')

        self.botao_login = QPushButton('Entrar')
        # Conecta o clique do botão à função de validação
        self.botao_login.clicked.connect(self.autenticar)

        # Layout (Organização Vertical)
        layout = QVBoxLayout()
        layout.addWidget(self.label_usuario)
        layout.addWidget(self.input_usuario)
        layout.addWidget(self.label_senha)
        layout.addWidget(self.input_senha)
        layout.addSpacing(10) # Espaço extra antes do botão
        layout.addWidget(self.botao_login)

        self.setLayout(layout)

    def autenticar(self):
        usuario = self.input_usuario.text()
        senha = self.input_senha.text()

        # Exemplo simples de validação
        if usuario == "admin" and senha == "1234":
            QMessageBox.information(self, 'Sucesso', 'Login realizado com sucesso!')
        else:
            QMessageBox.warning(self, 'Erro', 'Usuário ou senha incorretos.')

if __name__ == '__main__':
    app = QApplication(sys.argv)
    janela = LoginView()
    janela.show()
    sys.exit(app.exec_())
import 'package:flutter/material.dart';
import 'package:sistema_hospitalar_mobile/services/api_service.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key, required this.api, required this.onLogin});

  final ApiService api;
  final VoidCallback onLogin;

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailController = TextEditingController(text: 'paciente@hospital.local');
  final _passwordController = TextEditingController(text: 'Paciente123!');
  final _serverController = TextEditingController();
  bool _loading = false;
  bool _showServer = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _serverController.text = widget.api.baseUrl.replaceAll('/api', '');
  }

  Future<void> _submit() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      if (_showServer && _serverController.text.trim().isNotEmpty) {
        await widget.api.saveBaseUrl(_serverController.text.trim());
      }
      await widget.api.login(
        _emailController.text.trim(),
        _passwordController.text,
      );
      widget.onLogin();
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const SizedBox(height: 48),
              Text(
                'Sistema Hospitalar',
                style: Theme.of(context).textTheme.headlineMedium,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              Text(
                'Portal do Paciente',
                style: Theme.of(context).textTheme.bodyLarge?.copyWith(color: Colors.grey),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 40),
              TextField(
                controller: _emailController,
                decoration: const InputDecoration(labelText: 'E-mail', border: OutlineInputBorder()),
                keyboardType: TextInputType.emailAddress,
              ),
              const SizedBox(height: 16),
              TextField(
                controller: _passwordController,
                decoration: const InputDecoration(labelText: 'Senha', border: OutlineInputBorder()),
                obscureText: true,
              ),
              const SizedBox(height: 12),
              TextButton(
                onPressed: () => setState(() => _showServer = !_showServer),
                child: Text(_showServer ? 'Ocultar servidor' : 'Configurar servidor (celular na rede)'),
              ),
              if (_showServer) ...[
                TextField(
                  controller: _serverController,
                  decoration: const InputDecoration(
                    labelText: 'URL do servidor',
                    hintText: 'http://192.168.0.10:8080',
                    border: OutlineInputBorder(),
                    helperText: 'Use o IP do PC onde o Docker está rodando. Celular e PC na mesma Wi-Fi.',
                  ),
                  keyboardType: TextInputType.url,
                ),
                const SizedBox(height: 8),
              ],
              if (_error != null) ...[
                const SizedBox(height: 12),
                Text(_error!, style: const TextStyle(color: Colors.red)),
              ],
              const SizedBox(height: 24),
              FilledButton(
                onPressed: _loading ? null : _submit,
                child: Padding(
                  padding: const EdgeInsets.symmetric(vertical: 12),
                  child: Text(_loading ? 'Entrando...' : 'Entrar'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

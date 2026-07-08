import 'package:flutter/material.dart';
import 'package:sistema_hospitalar_mobile/screens/home_screen.dart';
import 'package:sistema_hospitalar_mobile/screens/login_screen.dart';
import 'package:sistema_hospitalar_mobile/services/api_service.dart';

void main() {
  runApp(const HospitalApp());
}

class HospitalApp extends StatefulWidget {
  const HospitalApp({super.key});

  @override
  State<HospitalApp> createState() => _HospitalAppState();
}

class _HospitalAppState extends State<HospitalApp> {
  final _api = ApiService();
  bool _ready = false;
  bool _loggedIn = false;

  @override
  void initState() {
    super.initState();
    _init();
  }

  Future<void> _init() async {
    await _api.loadToken();
    setState(() {
      _loggedIn = _api.isLoggedIn;
      _ready = true;
    });
  }

  void _onLogin() => setState(() => _loggedIn = true);
  void _onLogout() async {
    await _api.clearToken();
    setState(() => _loggedIn = false);
  }

  @override
  Widget build(BuildContext context) {
    if (!_ready) {
      return const MaterialApp(
        home: Scaffold(body: Center(child: CircularProgressIndicator())),
      );
    }

    return MaterialApp(
      title: 'Sistema Hospitalar',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF0D9488)),
        useMaterial3: true,
      ),
      home: _loggedIn
          ? HomeScreen(api: _api, onLogout: _onLogout)
          : LoginScreen(api: _api, onLogin: _onLogin),
    );
  }
}

import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import 'package:sistema_hospitalar_mobile/config/api_config.dart';

class ApiService {
  ApiService({String? baseUrl}) : _baseUrl = baseUrl ?? _resolveBaseUrl();

  static const _baseUrlKey = 'api_base_url';

  static String _resolveBaseUrl() {
    const fromEnv = String.fromEnvironment('API_BASE_URL');
    if (fromEnv.isNotEmpty) return _normalizeBaseUrl(fromEnv);
    return defaultApiBaseUrl;
  }

  static String _normalizeBaseUrl(String raw) {
    var url = raw.trim();
    while (url.endsWith('/')) {
      url = url.substring(0, url.length - 1);
    }
    if (!url.endsWith('/api')) {
      url = '$url/api';
    }
    return url;
  }

  String _baseUrl;
  String get baseUrl => _baseUrl;
  String? _token;

  bool get isLoggedIn => _token != null;

  Future<void> loadSettings() async {
    final prefs = await SharedPreferences.getInstance();
    _token = prefs.getString('hospital_token');
    final saved = prefs.getString(_baseUrlKey);
    if (saved != null && saved.isNotEmpty) {
      _baseUrl = _normalizeBaseUrl(saved);
    }
  }

  Future<void> loadToken() async => loadSettings();

  Future<void> saveBaseUrl(String raw) async {
    _baseUrl = _normalizeBaseUrl(raw);
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_baseUrlKey, _baseUrl);
  }

  Future<void> saveToken(String token) async {
    _token = token;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('hospital_token', token);
  }

  Future<void> clearToken() async {
    _token = null;
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('hospital_token');
  }

  Map<String, String> get _headers => {
        'Content-Type': 'application/json',
        if (_token != null) 'Authorization': 'Bearer $_token',
      };

  Future<Map<String, dynamic>> login(String email, String password) async {
    final response = await http.post(
      Uri.parse('$_baseUrl/auth/login'),
      headers: _headers,
      body: jsonEncode({'email': email, 'password': password}),
    );
    if (response.statusCode != 200) {
      throw Exception('E-mail ou senha inválidos (servidor: $_baseUrl)');
    }
    final data = jsonDecode(response.body) as Map<String, dynamic>;
    await saveToken(data['token'] as String);
    return data;
  }

  Future<Map<String, dynamic>> getDashboard() async {
    final response = await http.get(
      Uri.parse('$_baseUrl/patient-portal/dashboard'),
      headers: _headers,
    );
    if (response.statusCode != 200) {
      throw Exception('Falha ao carregar dashboard');
    }
    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> getMedicalRecord() async {
    final response = await http.get(
      Uri.parse('$_baseUrl/patient-portal/medical-record'),
      headers: _headers,
    );
    if (response.statusCode != 200) {
      throw Exception('Falha ao carregar prontuário');
    }
    return jsonDecode(response.body) as Map<String, dynamic>;
  }
}

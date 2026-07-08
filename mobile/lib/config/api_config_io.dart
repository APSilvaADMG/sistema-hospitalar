import 'dart:io';

/// Emulador Android: `10.0.2.2` aponta para localhost do host.
/// Windows, macOS, Linux e simulador iOS: `localhost`.
String get defaultApiBaseUrl =>
    Platform.isAndroid ? 'http://10.0.2.2:8080/api' : 'http://localhost:8080/api';

# App Mobile — Sistema Hospitalar

Portal do paciente em **Flutter** (Fases 4 e 6).

## Pré-requisitos

- [Flutter SDK](https://docs.flutter.dev/get-started/install) 3.2+
- **Android Studio** com SDK e emulador configurados
- API rodando (`docker compose up -d` na raiz do projeto)

## Android Studio + Docker

1. Suba a API: `docker compose up -d` (porta **8080**)
2. Abra o emulador Android no Android Studio (AVD Manager)
3. No terminal:

```bash
cd mobile
flutter pub get
flutter devices          # confirme o emulador listado
flutter run              # ou flutter run -d emulator-5554
```

O app usa **`http://10.0.2.2:8080/api`** no emulador (host loopback do Docker no Windows).

### Dispositivo físico na mesma rede

1. Na raiz do projeto, libere o firewall: `scripts/configure-mobile-access.ps1` (como Admin)
2. Suba o Docker: `docker compose up -d`
3. No app, toque em **Configurar servidor** na tela de login
4. Informe `http://SEU_IP_LAN:8080` (ex.: `http://192.168.0.15:8080`)

Ou via linha de comando:

```bash
flutter run --dart-define=API_BASE_URL=http://SEU_IP_LAN:8080/api
```

## Login demo

| E-mail | Senha |
|--------|-------|
| paciente@hospital.local | Paciente123! |

## Telas

- Login, dashboard, agenda e prontuário eletrônico

## Abrir no Android Studio

`File → Open → mobile/` (pasta do projeto Flutter)

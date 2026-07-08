# Sistema Hospitalar

[![CI](https://github.com/APSilvaADMG/sistema-hospitalar/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/APSilvaADMG/sistema-hospitalar/actions/workflows/ci.yml)

Plataforma hospitalar integrada — **Fases 1–9** completas.

## Módulos

| Fase | Módulos |
|------|---------|
| **1** | Auth, Pacientes, PEP, Agendamentos, Financeiro |
| **2** | Internação, Centro Cirúrgico, Farmácia, Estoque |
| **3** | Laboratório, Imagem, RH, BI, Faturamento TISS |
| **4** | IA, Integrações HL7/FHIR, Portal do Paciente, App Flutter |
| **5** | Emergência, Compras, Auditoria, Notificações |
| **6** | UTI, Ambulâncias, Estacionamento, Nutrição, Kubernetes |
| **7** | Consultórios, Hotelaria, Engenharia Clínica, Segurança, RabbitMQ |
| **8** | CME, Hemoterapia, Diálise, Lavanderia |
| **9** | Oncologia, Fisioterapia, Telemedicina, CCIH |

## Fase 9 — detalhes

- **Oncologia** — sessões de quimioterapia, protocolos e ciclos de tratamento
- **Fisioterapia** — reabilitação motora, respiratória e neurológica
- **Telemedicina** — teleconsultas com sala virtual gerada automaticamente
- **CCIH** — vigilância epidemiológica, isolamentos e dashboard de infecção

## Fase 8 — detalhes

- **CME** — kits instrumentais, ciclos de esterilização (vapor, ETO, plasma)
- **Hemoterapia** — banco de sangue, compatibilização e transfusões
- **Diálise** — agendamento e fluxo de sessões de hemodiálise
- **Lavanderia** — lotes de roupa hospitalar (coleta → lavagem → entrega)

## Fase 7 — detalhes

- **Consultórios** — salas ambulatoriais, escalas por profissional e especialidade
- **Hotelaria** — acomodação de acompanhantes, reservas e check-in/check-out
- **Engenharia Clínica** — patrimônio de equipamentos médicos e ordens de manutenção
- **Segurança** — controle de visitantes, incidentes e dashboard de portaria
- **RabbitMQ** — eventos publicados na exchange `hospital.events` (incidentes, visitantes, OS)

## Fase 6 — detalhes

- **UTI** — monitoramento de sinais vitais, alertas críticos por paciente internado
- **Ambulâncias** — frota, despachos SAMU, fluxo solicitação → transporte → conclusão
- **Estacionamento** — zonas, check-in/check-out, cobrança por hora
- **Nutrição** — dietas por internação (regular, pastosa, líquida, etc.)
- **Kubernetes** — manifests em `k8s/` para deploy em cluster

## Executar (Docker)

```bash
docker compose up --build
```

| Serviço | URL |
|---------|-----|
| Frontend | http://localhost:5173 |
| API / Swagger | http://localhost:8080/swagger |
| RabbitMQ | http://localhost:15672 |

### Acesso pelo celular (mesma rede Wi-Fi)

1. Suba o Docker: `docker compose up -d`
2. Libere o firewall no Windows (PowerShell **como Administrador**):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/configure-mobile-access.ps1
```

3. No celular, abra o navegador em **`http://SEU_IP_LAN:5173`** (não use `localhost`).
4. Login demo: `medico@hospital.local` / `Medico123!` (PEP) ou `paciente@hospital.local` / `Paciente123!`

> O IP LAN aparece ao rodar o script acima (ex.: `192.168.0.15`).

### Desenvolvimento web sem Docker

```bash
# Terminal 1 — API
dotnet run --project src/SistemaHospitalar.Api

# Terminal 2 — frontend (aceita conexões da rede)
cd web && npm run dev
```

Acesse no celular: `http://SEU_IP_LAN:5173`

## App mobile (Android)

Com **Android Studio** + emulador e API no Docker:

```bash
docker compose up -d
cd mobile
flutter pub get
flutter run
```

A API no emulador Android usa `http://10.0.2.2:8080/api` automaticamente.

## Usuários demo

| Perfil | E-mail | Senha |
|--------|--------|-------|
| Admin | admin@hospital.local | Admin123! |
| Recepção | recepcao@hospital.local | Recepcao123! |
| Médico | medico@hospital.local | Medico123! |
| Paciente | paciente@hospital.local | Paciente123! |

## Fluxo Fase 9 sugerido

1. **Oncologia** — administrar quimioterapia AC-T ciclo 2/4 do paciente demo
2. **Fisioterapia** — iniciar e concluir sessão respiratória agendada
3. **Telemedicina** — abrir sala virtual e concluir teleconsulta oncológica
4. **CCIH** — resolver caso Klebsiella e suspender isolamento de contato

## Fluxo Fase 8 sugerido

1. **CME** — iniciar e concluir ciclo do kit KIT-ORT-001 na autoclave
2. **Hemoterapia** — compatibilizar bolsa HEMO-001 com solicitação pendente e transfundir
3. **Diálise** — iniciar sessão agendada na máquina DIA-03
4. **Lavanderia** — avançar lote da UTI até status "Entregue"

## Fluxo Fase 7 sugerido

1. **Consultórios** — ver escalas da Dra. Ana (segunda) e Dr. Carlos (terça)
2. **Hotelaria** — reservar quarto H101 para acompanhante e fazer check-in
3. **Eng. Clínica** — concluir OS de calibração do ventilador EQ-VENT-001
4. **Segurança** — registrar saída do visitante demo e resolver incidente na portaria
5. **RabbitMQ** — conferir mensagens na UI em http://localhost:15672 (guest/guest)

## Fluxo Fase 6 sugerido

1. **UTI** — ver paciente demo com SpO₂ em alerta → registrar novos sinais
2. **Ambulâncias** — despachar AMB-01 para remoção pendente
3. **Estacionamento** — registrar saída do veículo ABC1D23
4. **Nutrição** — marcar dieta líquida como entregue
5. **K8s** — ver `k8s/README.md` para deploy em cluster

## PostgreSQL

- Host: localhost:5432 · DB: sistema_hospitalar · User/Pass: postgres/postgres

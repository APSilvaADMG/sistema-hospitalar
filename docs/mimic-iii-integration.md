# Integração MIMIC-III (PhysioNet) — Sistema Hospitalar

> **Aviso legal:** MIMIC-III contém dados clínicos **desidentificados** de pacientes do Beth Israel Deaconess Medical Center (EUA, 2001–2012). **Não são dados do seu hospital**, **não substituem PHI real** e **não devem ser misturados** com pacientes, internações ou faturamento de produção. Uso permitido apenas sob o [Data Use Agreement (DUA)](https://physionet.org/content/mimiciii/view-license/1.4/) e treinamento CITI exigido pela PhysioNet.

## Resposta rápida

| Pergunta | Resposta |
|----------|----------|
| Pode ser usado no projeto? | **Sim**, para pesquisa, treinamento, demos de ML/BI e desenvolvimento em ambiente isolado |
| Pode alimentar o banco de produção? | **Não** |
| Pode aparecer como “pacientes reais” na UI? | **Não** |
| Integração completa automática? | **Parcialmente** — exige credenciamento, download manual e ETL dedicado |

## O que é MIMIC-III

- Base relacional com **26 tabelas** (~40 mil pacientes de UTI/hospital).
- Dados: demografia, admissões, UTI, sinais vitais horários, laboratório, medicamentos, notas clínicas, CID-9, mortalidade.
- Formato oficial: **CSV** + scripts de carga para **PostgreSQL** (também MySQL/MonetDB).
- Tamanho: dezenas de GB descompactados (varia por tabela; `CHARTEVENTS` e `NOTEEVENTS` são as maiores).
- Datas **deslocadas** (anos ~2100–2200); idades >89 anos ofuscadas — **não use para validação de datas reais**.

## Pré-requisitos de acesso (PhysioNet)

1. Conta em [PhysioNet](https://physionet.org/register/).
2. Curso **CITI — Data or Specimens Only Research** (inclui HIPAA).
3. Assinatura do **PhysioNet Credentialed Health Data Use Agreement 1.5.0**.
4. Aprovação (tipicamente ≥1 semana).
5. Download credenciado em [MIMIC-III v1.4](https://physionet.org/content/mimiciii/1.4/) — **não incluído neste repositório**.

Referência: [MIMIC-III documentation](https://mimic.mit.edu/docs/iii/).

## Arquitetura recomendada

```
┌─────────────────────────────────────────────────────────────┐
│  Produção / homologação com PHI real (LGPD)                 │
│  Database: sistema_hospitalar  ← AppDbContext               │
│  Pacientes, PEP, TISS, internação real                      │
└─────────────────────────────────────────────────────────────┘
                              ✕  NUNCA misturar
┌─────────────────────────────────────────────────────────────┐
│  Sandbox de pesquisa (dev / ML / treinamento)               │
│  Database: mimic_iii  (PostgreSQL separado)                 │
│  Schema nativo MIMIC ou views materializadas                │
│  Feature flag: MimicResearch:Enabled = true                 │
└─────────────────────────────────────────────────────────────┘
```

### Opções de integração

| Opção | Quando usar | Esforço |
|-------|-------------|---------|
| **A. DB separado + consultas read-only** | BI, ML, protótipos de alertas UTI | Baixo–médio |
| **B. ETL subset → entidades locais** | Demos na UI com `DataSource=MimicDemo` | Alto |
| **C. Módulo `/pesquisa/mimic` apenas informativo** | Onboarding da equipe, status do ambiente | Baixo (já incluído) |
| **D. BigQuery / AWS Athena** | Análises em nuvem sem carga local | Médio (credenciais cloud) |

### Mapeamento conceitual (MIMIC → Sistema Hospitalar)

Ver `scripts/mimic/table-mapping.json`. Resumo:

| MIMIC-III | Sistema Hospitalar | Observação |
|-----------|-------------------|------------|
| `PATIENTS` / `ADMISSIONS` | `Patient`, `Hospitalization` | IDs inteiros vs `Guid`; CPF/CNS inexistentes no MIMIC |
| `ICUSTAYS` | `Hospitalization` + `Ward` (UTI) | Unidades americanas (MICU, SICU…) |
| `CHARTEVENTS` | `VitalSignRecord` | Requer agregação por ITEMID (FC, PA, SpO2…) |
| `LABEVENTS` | `LabOrder` / `LabResult` | Mapear `D_LABITEMS` → `LabExamCatalog` |
| `DIAGNOSES_ICD` | `Cid10Catalog` | ICD-9 no MIMIC; conversão ICD-10 opcional |
| `PRESCRIPTIONS` | Farmácia / dispensação | Modelos de prescrição diferentes |
| `NOTEEVENTS` | `MedicalRecordEntry` | Texto livre; não misturar com PEP real |

## O que NÃO fazer

- Importar MIMIC para o mesmo schema/banco de pacientes reais.
- Exibir nomes ou narrativas MIMIC na recepção, PEP ou faturamento sem rótulo **“Dados de demonstração — MIMIC-III”**.
- Tentar reidentificar pacientes (proibido pelo DUA).
- Usar em ambiente exposto à internet sem controle de acesso e sem DUA assinado por todos os envolvidos.
- Assumir conformidade LGPD apenas por estar desidentificado — trate como **dados sensíveis de pesquisa** com política interna.

## Passos para desenvolvedores

### 1. Credenciamento

Siga https://physionet.org/content/mimiciii/1.4/ até obter acesso aos arquivos CSV.

### 2. Carga no PostgreSQL (sandbox)

```powershell
# Ajuste o caminho para onde você extraiu o download credenciado
powershell -ExecutionPolicy Bypass -File scripts/mimic/import-mimic-subset.ps1 `
  -MimicCsvPath "D:\datasets\mimic-iii-clinical-database-1.4" `
  -PostgresHost localhost -PostgresPort 5432 `
  -Database mimic_iii -SubsetOnly -RunEtl -MaxSubjects 50
```

O script **não baixa** dados; valida arquivos e pode executar o ETL subset de sinais vitais.

#### ETL subset — CHARTEVENTS → sinais vitais

Pipeline isolado no banco `mimic_iii` (schema `mimic_staging`). **Não grava** em `sistema_hospitalar` nem em `VitalSignRecord` de produção.

| Etapa | Artefato | Descrição |
|-------|----------|-----------|
| 1. Schema | `scripts/mimic/001-staging-schema.sql` | Tabelas `etl_run`, `chartevents_vitals_raw`, `vital_sign_snapshot` |
| 2. Stream CSV | `scripts/mimic/etl-chartevents-vitals.ps1` ou API dev | Filtra ITEMIDs de sinais vitais (FC, PA, SpO2, FR, temp.) |
| 3. Pivot | `scripts/mimic/002-etl-vital-signs.sql` | Agrupa por `subject_id` + `icustay_id` + `charttime` → formato wide (como `VitalSignRecord`) |

ITEMIDs MIMIC-III v1.4 (ver `table-mapping.json`):

| Sinal | ITEMID |
|-------|--------|
| Frequência cardíaca | 220045 |
| PAS | 220179 |
| PAD | 220180 |
| SpO2 | 220277 |
| Frequência respiratória | 220210 |
| Temperatura (°C) | 223761 |

**PowerShell (recomendado para volumes maiores):**

```powershell
.\scripts\mimic\etl-chartevents-vitals.ps1 `
  -MimicCsvPath "D:\datasets\mimic-iii-clinical-database-1.4" `
  -Database mimic_iii -MaxSubjects 50
```

Requer `psql` no PATH. Crie o banco antes: `CREATE DATABASE mimic_iii;`

**API (somente Development):**

```http
POST /api/research/mimic/etl/import?maxSubjects=50
GET  /api/research/mimic/etl/status
GET  /api/research/mimic/vitals?subjectId=3&limit=50
```

Configure `MimicResearch:CsvPath` com a pasta do download credenciado (contendo `CHARTEVENTS.csv` ou `.gz`).

### 3. Habilitar camada de pesquisa na API (opcional)

Em `appsettings.Development.json` (somente dev):

```json
"MimicResearch": {
  "Enabled": true,
  "ConnectionString": "Host=localhost;Port=5432;Database=mimic_iii;Username=postgres;Password=postgres",
  "RequireSeparateDatabase": true,
  "CsvPath": "D:\\datasets\\mimic-iii-clinical-database-1.4",
  "MaxSubjects": 50,
  "AllowDevImportTrigger": true,
  "DisplayLabel": "Dados de demonstração — MIMIC-III (não são pacientes deste hospital)"
}
```

### 4. UI

Rota interna: `/pesquisa/mimic` — painel de status, **status ETL**, botão de import subset (dev), consulta de sinais vitais e exemplos SQL.

## Citação

Ao publicar resultados, cite:

> Johnson, A., Pollard, T., & Mark, R. (2016). MIMIC-III Clinical Database (version 1.4). PhysioNet. https://doi.org/10.13026/C2XW26

## Arquivos neste repositório

| Arquivo | Função |
|---------|--------|
| `docs/mimic-iii-integration.md` | Este guia |
| `scripts/mimic/import-mimic-subset.ps1` | Validação CSV + flag `-RunEtl` |
| `scripts/mimic/etl-chartevents-vitals.ps1` | ETL CHARTEVENTS → `mimic_staging` |
| `scripts/mimic/001-staging-schema.sql` | DDL schema staging |
| `scripts/mimic/002-etl-vital-signs.sql` | Pivot raw → snapshots wide |
| `scripts/mimic/table-mapping.json` | Mapeamento MIMIC ↔ entidades locais |
| `MimicResearchController` + `/pesquisa/mimic` | Status, ETL, vitals API e UI |

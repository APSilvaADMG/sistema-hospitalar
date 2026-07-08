# Avaliação de PEPs externos — Prontomed e ProNele

> Avaliação para o **Sistema Hospitalar (APSMedCore)** em jun/2026. Repositórios analisados via GitHub API e inspeção de schema/código.

## Resposta rápida

| Repositório | Pode ajudar na implementação? | Recomendação |
|-------------|------------------------------|--------------|
| [CarlosSLoureiro/prontomed](https://github.com/CarlosSLoureiro/prontomed) | **Parcialmente** | Referência de fluxo consulta → observação; não integrar como serviço |
| [FelipeFelipeRenan/prontuario-medico](https://github.com/FelipeFelipeRenan/prontuario-medico) (ProNele) | **Parcialmente** | Inspirar campo `anamnesis` e UX mobile; não portar Strapi/RN |

O APSMedCore **já possui PEP mais completo** (.NET + React, offline, assinatura digital, TISS, FHIR, relatórios). Estes projetos servem sobretudo para **validar workflows** e **preencher lacunas de UI** (ex.: abas Anamnese e Solicitação de Exames).

---

## 1. Prontomed

| Atributo | Valor |
|----------|-------|
| Stack | Laravel 9, PHP 8, MySQL, JWT (php-open-source-saver/jwt-auth), Laravel UI, Docker |
| Licença | GPL-3.0 |
| Stars / forks | 1 / 0 |
| Criado | Abr/2022 |
| Último push | **Out/2022** (inativo ~3 anos) |
| Maturidade | Projeto acadêmico/MVP; testes PHPUnit presentes |

### Módulos

| Módulo externo | Entidades | No APSMedCore |
|----------------|-----------|---------------|
| Pacientes | `Paciente` | ✅ `Patient` + `MedicalRecord` |
| Médicos | `Medico` (JWT) | ✅ `Professional` + auth |
| Consultas | `Consulta` (agenda por tipo) | ✅ `Appointment` |
| Observações | `Observacoes` (texto na consulta) | ✅ `MedicalRecordEntry` |
| Anamnese estruturada | ❌ | ✅ `ClinicalEntryForm` + templates |
| Prescrição | ❌ | ✅ entryType Prescription + farmácia |
| Exames | ❌ | ✅ Lab/Imaging + entryType ExamRequest |
| Evolução / enfermagem | ❌ | ✅ múltiplas abas PEP |
| Assinatura digital | ❌ | ✅ `DigitalSignaturePad` |
| FHIR / TISS / SUS | ❌ | ✅ integrações nativas |

### API (routes/api.php)

- `POST /login`, `GET /pacientes`, CRUD paciente
- `GET /consultas/{tipo}`, agendar/editar/excluir consulta
- `POST /consulta/{id}/observacao` — único registro clínico

### Opções de integração

| Opção | Viável? | Notas |
|-------|---------|-------|
| Port direto | ❌ | Stack PHP/Laravel vs .NET |
| Microserviço side-by-side | ❌ | Duplicaria pacientes; GPL-3.0 |
| Borrow UX/workflows | ✅ | Fluxo consulta → nota clínica |
| Referência only | ✅ | **Recomendado** |

---

## 2. ProNele (prontuario-medico)

| Atributo | Valor |
|----------|-------|
| Stack | React Native (mobile), Strapi 4 (backend Node), Docker, TypeScript |
| Licença | **Não declarada** no repositório |
| Stars / forks | 0 / 1 |
| Criado | Mar/2023 |
| Último push | **Jul/2023** (inativo ~3 anos) |
| Maturidade | TCC/projeto de faculdade; schema Strapi mínimo |

### Content-types Strapi

| Tipo | Campos relevantes |
|------|-------------------|
| `consulta` | `anamnesis` (text), `comment`, `idPaciente`, `idMedico`, `idEnfermeira` |
| `paciente` | `dateBirth`, `address`, `idUser` |
| `medico` / `enfermeiro` | perfis de profissional |

### Módulos vs local

| Feature ProNele | APSMedCore |
|-----------------|------------|
| Anamnese em consulta | ✅ Aba `/pep/anamnese` + `MedicalRecordEntryType.Anamnesis` |
| App mobile | ❌ (web responsive + offline queue) |
| Portal paciente consulta prontuário | ✅ `PatientPortalPage` |
| Prescrição / exames / TISS | ❌ no ProNele | ✅ no APSMedCore |

### Opções de integração

| Opção | Viável? | Notas |
|-------|---------|-------|
| Port direto | ❌ | React Native + Strapi |
| Microserviço Strapi | ❌ | Schema pobre; sem interoperabilidade |
| Borrow anamnesis UX | ✅ | Campo `anamnesis` → formulário estruturado local |
| Referência only | ✅ | **Recomendado** |

---

## 3. PEP local (baseline)

### Rotas e UI

- Hub: `/pep` → `PepHubPage`
- Abas (`moduleSections.pepTabs`): anamnese, evoluções, prescrição, solicitação de exames, CID, procedimentos, sinais vitais, escalas, anexos, assinaturas
- Prontuário legado: `/pacientes/:id/prontuario` → `MedicalRecordPage` (tabs resumo/clínico/cuidados/internação/TISS)
- Offline: `pepActions.ts`, `pepOfflineDb`, `pepOfflineSync`

### Domínio

```text
MedicalRecord (1:1 Patient)
  └── MedicalRecordEntry
        EntryType: Anamnesis | Evolution | Prescription | ExamRequest | Procedure
        Content, Cid10Code, IsSigned, SignatureImage, ClientRequestId
```

### FHIR

- Export/import `Patient` via `IntegrationsController` (`/integrations/fhir/Patient/{id}`)
- RNDS planejado em `GovernmentIntegrationProfiles`
- Sem Composition/DocumentReference FHIR para entradas PEP ainda

### Catálogo externo

- Entradas em `web/src/data/externalSourcesCatalog.ts`
- Clone de referência: `Diversos/external-repos/sync-external-repos.ps1`

---

## 4. Lacunas identificadas (pós-auditoria)

| Lacuna | Status |
|--------|--------|
| Aba PEP dedicada a anamnese | ✅ Adicionada `/pep/anamnese` |
| Aba PEP para solicitação de exames | ✅ Adicionada `/pep/solicitacao-exames` |
| Relatório `pep.exam-requests` | 🔲 Planejado |
| FHIR Composition para evoluções | 🔲 Planejado (RNDS) |
| Sincronizar anamnese com agendamento ativo | 🔲 Melhoria futura |

---

## 5. Próximos passos sugeridos

1. **Vincular entrada PEP ao `AppointmentId`** ao criar registro a partir da agenda ambulatorial.
2. **Relatório** `pep.exam-requests` no `ReportCatalog` (paridade com prescrições).
3. **FHIR R4**: exportar `Bundle` com `Composition` + `Observation` para RNDS.
4. **Testes E2E** das novas abas PEP (anamnese, solicitação de exames).
5. Manter repositórios externos apenas em `Diversos/external-repos/` — **não** embeddar código GPL no core sem revisão jurídica.

---

## Referências

- Prontomed: https://github.com/CarlosSLoureiro/prontomed
- ProNele: https://github.com/FelipeFelipeRenan/prontuario-medico
- Painel de fontes externas: Relatórios → Integrações open source

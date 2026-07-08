# Snapshots de templates externos (referência de campos)

Gerado a partir dos repositórios GitHub listados em `manifest.json`.
Use `sync-external-repos.ps1` para clonar localmente (requer Git) ou baixar ZIP automaticamente.

## R4EPI/sitrep (GPL-3.0)

| Relatório APSMedCore | Template Rmd | Campos principais |
|----------------------|--------------|-------------------|
| `ccih.epidemic.curve` | `measles_outbreak/skeleton/skeleton.Rmd` | Epiweek, Cases (n), Population at risk, AR (per 10,000) |
| `ccih.mortality.surveillance` | `mortality/skeleton/skeleton.Rmd` | Epiweek, Deaths (n), CMR per 10,000 |
| `ccih.vaccination.coverage` | `vaccination_long/skeleton/skeleton.Rmd` | Vaccine, Age group, Doses (n) |

URLs:
- https://github.com/R4EPI/sitrep/tree/master/inst/rmarkdown/templates

## dev-queiroz/sistema-hospitalar (Apache-2.0)

| Relatório APSMedCore | Arquivo | Campos principais |
|----------------------|---------|-------------------|
| `ccih.outbreak.indicators` | `GroqService.ts` | resumo_executivo.risco, indicadores.*, recomendacoes[] |
| `er.visits.by-triage` | `Triagem.ts` | nivel_gravidade, queixa_principal |
| `er.patients.served` | `pdfCompiler.ts` | Data de chegada, Paciente, Gravidade, Status |
| Prontuário PDF | `pdfCompiler.ts` | paciente, profissional, triagens[], confidencial |

URLs:
- https://github.com/dev-queiroz/sistema-hospitalar/tree/main/src

## Mapeamento no código

- Backend: `src/SistemaHospitalar.Infrastructure/Reports/ReportFieldMappings.cs`
- Frontend: `web/src/data/reportFieldMappings.ts`
- Prefixos por módulo (`er.*`, `lab.*`, `reg.*`, etc.) aplicam rótulos comuns sitrep/HospitalRun quando não há mapeamento exato.

# GTH 360 Enterprise — Documentação de Evolução

Plataforma de gestão hospitalar modular. Este diretório concentra a visão de produto, catálogo priorizado e roadmap — **sem substituir** o código existente.

## Princípios (não negociáveis)

1. Não quebrar fluxos que já funcionam (PEP Feegow, TV, Connect, TISS, login).
2. Melhorias incrementais e extensíveis.
3. Mesma base PostgreSQL e regras de negócio compartilhadas.
4. Rastreabilidade e auditoria em ações clínicas.

## Volumes

| Arquivo | Conteúdo |
|---------|----------|
| [catalog-priorities.md](./catalog-priorities.md) | Catálogo resumido por módulo (Essencial / Importante / Futuro) |
| [roadmap.md](./roadmap.md) | Fases v1.0 → v3.0 e ordem de entrega |

## Referência externa

O repositório [ClinicCare](https://github.com/nathadriele/cliniccare-medical-clinic-management-system) serve **apenas** para benchmark de UX (agendamento, dashboards, relatórios). Não copiar stack Python/Dash nem modelo de dados.

## Status técnico recente

- **Identificação:** pulseiras GTH + QR, etiquetas, resolve por scan.
- **Leito:** `/enfermagem/leito` — Point of Care com eMAR básico e senha.
- **Assinaturas:** fila PEP `/pep/assinaturas`, re-auth por senha, auditoria.
- **Carga de teste:** `scripts/seed-hospital-load-10k.ps1` → banco `sistema_hospitalar`.

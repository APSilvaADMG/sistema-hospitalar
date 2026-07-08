using SistemaHospitalar.Application.DTOs.Reports;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Reports;

public static class ReportCatalog
{
    private static readonly IReadOnlyDictionary<ReportModule, string> ModuleLabels = new Dictionary<ReportModule, string>
    {
        [ReportModule.Administrative] = "Administrativo",
        [ReportModule.Reception] = "Recepção e Agendamento",
        [ReportModule.Emergency] = "Pronto Atendimento",
        [ReportModule.Hospitalization] = "Internação",
        [ReportModule.MedicalRecord] = "Prontuário Eletrônico",
        [ReportModule.Nursing] = "Enfermagem",
        [ReportModule.Surgery] = "Centro Cirúrgico",
        [ReportModule.Pharmacy] = "Farmácia",
        [ReportModule.Supply] = "Almoxarifado",
        [ReportModule.Laboratory] = "Laboratório",
        [ReportModule.Imaging] = "Diagnóstico por Imagem",
        [ReportModule.Financial] = "Financeiro",
        [ReportModule.Insurance] = "Convênios (ANS)",
        [ReportModule.HospitalBilling] = "Faturamento Hospitalar",
        [ReportModule.HumanResources] = "Recursos Humanos",
        [ReportModule.Quality] = "Qualidade e Segurança",
        [ReportModule.InfectionControl] = "CCIH",
        [ReportModule.Audit] = "Auditoria",
        [ReportModule.BusinessIntelligence] = "BI Executivo",
        [ReportModule.Regulatory] = "Governo / ANS / SUS",
    };

    private static readonly IReadOnlyDictionary<ReportModule, int> ModuleTargets = new Dictionary<ReportModule, int>
    {
        [ReportModule.Administrative] = 20,
        [ReportModule.Reception] = 20,
        [ReportModule.Emergency] = 15,
        [ReportModule.Hospitalization] = 25,
        [ReportModule.MedicalRecord] = 40,
        [ReportModule.Nursing] = 20,
        [ReportModule.Surgery] = 25,
        [ReportModule.Pharmacy] = 30,
        [ReportModule.Supply] = 20,
        [ReportModule.Laboratory] = 25,
        [ReportModule.Imaging] = 20,
        [ReportModule.Financial] = 40,
        [ReportModule.Insurance] = 35,
        [ReportModule.HospitalBilling] = 25,
        [ReportModule.HumanResources] = 20,
        [ReportModule.Quality] = 20,
        [ReportModule.InfectionControl] = 20,
        [ReportModule.Audit] = 20,
        [ReportModule.BusinessIntelligence] = 40,
        [ReportModule.Regulatory] = 20,
    };

    private static readonly Lazy<IReadOnlyList<ReportDefinition>> Cache = new(Build);

    public static IReadOnlyList<ReportDefinition> All => Cache.Value;

    public static ReportDefinition? Find(string code) =>
        All.FirstOrDefault(r => r.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    public static string GetModuleLabel(ReportModule module) =>
        ModuleLabels.TryGetValue(module, out var label) ? label : module.ToString();

    private static ReportDefinition D(
        string code,
        ReportModule module,
        string name,
        bool essential,
        bool implemented,
        int phase = 1,
        string? description = null) =>
        new(code, name, module, description ?? name, essential, implemented, phase);

    private static IReadOnlyList<ReportDefinition> Build()
    {
        var items = new List<ReportDefinition>
        {
            // ── 1. Administrativo ──
            D("admin.patients.registered", ReportModule.Administrative, "Pacientes cadastrados", true, true),
            D("admin.patients.new-by-period", ReportModule.Administrative, "Novos pacientes por período", true, true),
            D("admin.patients.active-inactive", ReportModule.Administrative, "Pacientes ativos/inativos", true, true),
            D("admin.patients.by-city", ReportModule.Administrative, "Pacientes por cidade", true, true),
            D("admin.patients.by-insurance", ReportModule.Administrative, "Pacientes por convênio", true, true),
            D("admin.patients.consultation-history", ReportModule.Administrative, "Histórico de consultas", true, true),
            D("admin.patients.hospitalization-history", ReportModule.Administrative, "Histórico de internações", true, true),
            D("admin.patients.indicators", ReportModule.Administrative, "Indicadores de pacientes", true, true),
            D("admin.appointments.total", ReportModule.Administrative, "Atendimentos realizados", true, true),
            D("admin.appointments.by-unit", ReportModule.Administrative, "Atendimentos por unidade", false, true, 2),
            D("admin.appointments.by-specialty", ReportModule.Administrative, "Atendimentos por especialidade", true, true),
            D("admin.appointments.by-doctor", ReportModule.Administrative, "Atendimentos por médico", true, true),
            D("admin.appointments.avg-time", ReportModule.Administrative, "Tempo médio de atendimento", false, true, 2),
            D("admin.beds.occupancy", ReportModule.Administrative, "Taxa de ocupação hospitalar", true, true),
            D("admin.indicators.summary", ReportModule.Administrative, "Indicadores hospitalares", true, true),

            // ── 2. Recepção ──
            D("reception.appointments.scheduled", ReportModule.Reception, "Consultas agendadas", true, true),
            D("reception.appointments.completed", ReportModule.Reception, "Consultas realizadas", true, true),
            D("reception.appointments.cancelled", ReportModule.Reception, "Consultas canceladas", true, true),
            D("reception.appointments.rescheduled", ReportModule.Reception, "Consultas remarcadas", false, true, 2),
            D("reception.appointments.no-show", ReportModule.Reception, "Faltas", true, true),
            D("reception.schedule.by-doctor", ReportModule.Reception, "Agenda por médico", true, true),
            D("reception.schedule.by-specialty", ReportModule.Reception, "Agenda por especialidade", true, true),
            D("reception.schedule.by-insurance", ReportModule.Reception, "Agenda por convênio", true, true),
            D("reception.wait.avg-time", ReportModule.Reception, "Tempo médio de espera", false, true, 2),
            D("reception.productivity", ReportModule.Reception, "Produtividade da recepção", false, true, 2),

            // ── 3. Pronto Atendimento ──
            D("er.visits.by-triage", ReportModule.Emergency, "Atendimentos por classificação de risco", true, true),
            D("er.wait.by-triage", ReportModule.Emergency, "Tempo de espera por classificação", true, true),
            D("er.patients.served", ReportModule.Emergency, "Pacientes atendidos", true, true),
            D("er.patients.transferred", ReportModule.Emergency, "Pacientes transferidos", true, true),
            D("er.patients.admitted", ReportModule.Emergency, "Pacientes internados após atendimento", true, true),
            D("er.stay.avg-time", ReportModule.Emergency, "Tempo médio de permanência", true, true),
            D("er.diagnoses.top", ReportModule.Emergency, "Principais diagnósticos", true, true),

            // ── 4. Internação ──
            D("hosp.patients.current", ReportModule.Hospitalization, "Pacientes internados", true, true),
            D("hosp.admissions.by-period", ReportModule.Hospitalization, "Internações por período", true, true),
            D("hosp.discharges", ReportModule.Hospitalization, "Altas hospitalares", true, true),
            D("hosp.transfers.internal", ReportModule.Hospitalization, "Transferências internas", true, true),
            D("hosp.deaths", ReportModule.Hospitalization, "Óbitos", true, true),
            D("hosp.los.avg", ReportModule.Hospitalization, "Média de permanência", true, true),
            D("hosp.beds.occupancy", ReportModule.Hospitalization, "Taxa de ocupação de leitos", true, true),
            D("hosp.beds.turnover", ReportModule.Hospitalization, "Giro de leitos", false, true, 2),
            D("hosp.beds.available", ReportModule.Hospitalization, "Leitos disponíveis", true, true),
            D("hosp.beds.blocked", ReportModule.Hospitalization, "Leitos bloqueados", true, true),

            // ── 5. Prontuário ──
            D("pep.evolutions.medical", ReportModule.MedicalRecord, "Evoluções médicas", true, true),
            D("pep.evolutions.nursing", ReportModule.MedicalRecord, "Evoluções de enfermagem", true, true),
            D("pep.prescriptions", ReportModule.MedicalRecord, "Prescrições médicas", true, true),
            D("pep.prescriptions.expired", ReportModule.MedicalRecord, "Prescrições vencidas", true, true),
            D("pep.diagnoses", ReportModule.MedicalRecord, "Diagnósticos realizados", true, true),
            D("pep.cid.top", ReportModule.MedicalRecord, "CID mais utilizados", true, true),
            D("pep.procedures", ReportModule.MedicalRecord, "Procedimentos realizados", true, true),
            D("pep.patient.history", ReportModule.MedicalRecord, "Histórico completo do paciente", true, true),
            D("pep.vaccinations", ReportModule.MedicalRecord, "Vacinações aplicadas", true, true),

            // ── 6. Enfermagem ──
            D("nursing.meds.administered", ReportModule.Nursing, "Administração de medicamentos", true, true),
            D("nursing.meds.pending", ReportModule.Nursing, "Medicamentos pendentes", false, false, 2),
            D("nursing.scales", ReportModule.Nursing, "Escalas de enfermagem", false, false, 2),
            D("nursing.shift.handover", ReportModule.Nursing, "Passagem de plantão", false, false, 2),
            D("nursing.dressings", ReportModule.Nursing, "Curativos realizados", false, false, 2),
            D("nursing.vitals", ReportModule.Nursing, "Sinais vitais registrados", true, true),
            D("nursing.adverse-events", ReportModule.Nursing, "Eventos adversos", false, false, 2),

            // ── 7. Centro Cirúrgico ──
            D("surgery.completed", ReportModule.Surgery, "Cirurgias realizadas", true, true),
            D("surgery.cancelled", ReportModule.Surgery, "Cirurgias canceladas", true, true),
            D("surgery.by-specialty", ReportModule.Surgery, "Cirurgias por especialidade", true, true),
            D("surgery.by-surgeon", ReportModule.Surgery, "Cirurgias por cirurgião", true, true),
            D("surgery.avg-duration", ReportModule.Surgery, "Tempo médio de cirurgia", true, true),
            D("surgery.materials.consumption", ReportModule.Surgery, "Consumo de materiais por cirurgia", false, false, 2),
            D("surgery.room.occupancy", ReportModule.Surgery, "Ocupação das salas cirúrgicas", true, true),

            // ── 8. Farmácia ──
            D("pharmacy.stock.current", ReportModule.Pharmacy, "Estoque atual", true, true),
            D("pharmacy.stock.movements", ReportModule.Pharmacy, "Movimentação de medicamentos", true, true),
            D("pharmacy.dispensed", ReportModule.Pharmacy, "Medicamentos dispensados", true, true),
            D("pharmacy.expired", ReportModule.Pharmacy, "Medicamentos vencidos", true, true),
            D("pharmacy.ward.stock", ReportModule.Pharmacy, "Estoque por ala", true, true),
            D("pharmacy.expiring-soon", ReportModule.Pharmacy, "Medicamentos próximos do vencimento", false, true, 2),
            D("pharmacy.consumption.by-sector", ReportModule.Pharmacy, "Consumo por setor", false, true, 2),
            D("pharmacy.consumption.by-patient", ReportModule.Pharmacy, "Consumo por paciente", false, true, 2),
            D("pharmacy.inventory", ReportModule.Pharmacy, "Inventário", false, false, 2),
            D("pharmacy.abc-curve", ReportModule.Pharmacy, "Curva ABC", false, true, 3),

            // ── 9. Almoxarifado ──
            D("supply.entries", ReportModule.Supply, "Entrada de materiais", true, true),
            D("supply.exits", ReportModule.Supply, "Saída de materiais", true, true),
            D("supply.consumption.by-sector", ReportModule.Supply, "Consumo por setor", true, true),
            D("supply.stock.minimum", ReportModule.Supply, "Estoque mínimo", true, true),
            D("supply.stock.maximum", ReportModule.Supply, "Estoque máximo", false, false, 2),
            D("supply.expired", ReportModule.Supply, "Materiais vencidos", false, true, 2),
            D("supply.inventory", ReportModule.Supply, "Inventário geral", false, false, 2),
            D("supply.abc-curve", ReportModule.Supply, "Curva ABC", false, true, 3),

            // ── 10. Laboratório ──
            D("lab.orders.requested", ReportModule.Laboratory, "Exames solicitados", true, true),
            D("lab.orders.completed", ReportModule.Laboratory, "Exames realizados", true, true),
            D("lab.orders.pending", ReportModule.Laboratory, "Exames pendentes", true, true),
            D("lab.release.avg-time", ReportModule.Laboratory, "Tempo médio de liberação", true, true),
            D("lab.orders.by-doctor", ReportModule.Laboratory, "Exames por médico", true, true),
            D("lab.orders.by-insurance", ReportModule.Laboratory, "Exames por convênio", false, true, 2),
            D("lab.production", ReportModule.Laboratory, "Produção laboratorial", true, true),

            // ── 11. Imagem ──
            D("img.xray", ReportModule.Imaging, "Raio-X realizados", true, true),
            D("img.ct", ReportModule.Imaging, "Tomografias realizadas", true, true),
            D("img.mri", ReportModule.Imaging, "Ressonâncias realizadas", true, true),
            D("img.ultrasound", ReportModule.Imaging, "Ultrassons realizados", true, true),
            D("img.report.avg-time", ReportModule.Imaging, "Tempo de entrega dos laudos", true, true),
            D("img.production.by-equipment", ReportModule.Imaging, "Produção por equipamento", false, false, 2),
            D("img.production.by-doctor", ReportModule.Imaging, "Produção por médico", true, true),

            // ── 12. Financeiro ──
            D("fin.revenue.gross", ReportModule.Financial, "Faturamento bruto", true, true),
            D("fin.revenue.net", ReportModule.Financial, "Faturamento líquido", true, true),
            D("fin.revenue.by-period", ReportModule.Financial, "Receitas por período", true, true),
            D("fin.expenses.by-period", ReportModule.Financial, "Despesas por período", true, true),
            D("fin.cashflow", ReportModule.Financial, "Fluxo de caixa", true, true),
            D("fin.cash.sessions", ReportModule.Financial, "Fechamento de caixa", true, true),
            D("fin.payables", ReportModule.Financial, "Contas a pagar", true, true),
            D("fin.receivables", ReportModule.Financial, "Contas a receber", true, true),
            D("fin.delinquency", ReportModule.Financial, "Inadimplência", true, true),
            D("fin.statement", ReportModule.Financial, "Demonstrativo financeiro", false, true, 2),
            D("fin.dre", ReportModule.Financial, "DRE", false, true, 3),

            // ── 13. Convênios ──
            D("ins.production.by-insurance", ReportModule.Insurance, "Produção por convênio", true, true),
            D("ins.guides.issued", ReportModule.Insurance, "Guias emitidas", true, true),
            D("ins.guides.authorized", ReportModule.Insurance, "Guias autorizadas", true, true),
            D("ins.guides.glosas", ReportModule.Insurance, "Guias glosadas", true, true),
            D("ins.glosas.by-reason", ReportModule.Insurance, "Glosas por motivo", true, true),
            D("ins.glosas.appeals", ReportModule.Insurance, "Recursos de glosa", false, true, 2),
            D("ins.billing.by-operator", ReportModule.Insurance, "Faturamento por operadora", true, true),
            D("ins.tiss.sent", ReportModule.Insurance, "TISS enviado", true, true),
            D("ins.tiss.rejected", ReportModule.Insurance, "TISS rejeitado", true, true),
            D("ins.billing.pending", ReportModule.Insurance, "Pendências de faturamento", true, true),
            D("ins.tpa.summary", ReportModule.Insurance, "Resumo de TPA", true, true),

            // ── 14. Faturamento Hospitalar ──
            D("bill.procedures.billed", ReportModule.HospitalBilling, "Procedimentos faturados", true, true),
            D("bill.procedures.unbilled", ReportModule.HospitalBilling, "Procedimentos não faturados", false, false, 2),
            D("bill.accounts.open", ReportModule.HospitalBilling, "Contas hospitalares abertas", true, true),
            D("bill.accounts.closed", ReportModule.HospitalBilling, "Contas fechadas", true, true),
            D("bill.accounts.audit", ReportModule.HospitalBilling, "Contas em auditoria", false, false, 2),
            D("bill.accounts.glosas", ReportModule.HospitalBilling, "Contas glosadas", true, true),
            D("bill.revenue.by-procedure", ReportModule.HospitalBilling, "Receita por procedimento", true, true),
            D("bill.revenue.by-specialty", ReportModule.HospitalBilling, "Receita por especialidade", false, true, 2),

            // ── 15. RH ──
            D("hr.employees.active", ReportModule.HumanResources, "Funcionários ativos", true, true),
            D("hr.employees.on-leave", ReportModule.HumanResources, "Funcionários afastados", false, false, 2),
            D("hr.schedules", ReportModule.HumanResources, "Escalas de trabalho", false, true, 2),
            D("hr.shifts", ReportModule.HumanResources, "Plantões realizados", false, true, 2),
            D("hr.overtime", ReportModule.HumanResources, "Horas extras", false, true, 2),
            D("hr.productivity", ReportModule.HumanResources, "Produtividade por setor", false, true, 2),
            D("hr.absenteeism", ReportModule.HumanResources, "Absenteísmo", false, false, 2),
            D("hr.trainings", ReportModule.HumanResources, "Treinamentos realizados", false, false, 2),
            D("hr.performance", ReportModule.HumanResources, "Avaliações de desempenho", false, false, 3),
            D("hr.payroll.summary", ReportModule.HumanResources, "Resumo da folha de pagamento", true, true),

            // ── 16. Qualidade ──
            D("quality.adverse-events", ReportModule.Quality, "Eventos adversos", true, true),
            D("quality.patient-falls", ReportModule.Quality, "Quedas de pacientes", false, true, 2),
            D("quality.infections", ReportModule.Quality, "Infecções hospitalares", true, true),
            D("quality.nonconformities", ReportModule.Quality, "Não conformidades", false, false, 2),
            D("quality.indicators", ReportModule.Quality, "Indicadores de qualidade", false, true, 2),
            D("quality.satisfaction", ReportModule.Quality, "Satisfação dos pacientes", false, false, 3),
            D("quality.complaints", ReportModule.Quality, "Reclamações", false, false, 2),
            D("quality.ombudsman", ReportModule.Quality, "Ouvidoria", false, false, 3),

            // ── 17. CCIH ──
            D("ccih.infections.by-sector", ReportModule.InfectionControl, "Infecções por setor", true, true),
            D("ccih.infections.by-period", ReportModule.InfectionControl, "Infecções por período", true, true),
            D("ccih.antibiotics", ReportModule.InfectionControl, "Uso de antibióticos", false, true, 2),
            D("ccih.infection-rate", ReportModule.InfectionControl, "Taxa de infecção hospitalar", true, true),
            D("ccih.monitored-cases", ReportModule.InfectionControl, "Casos monitorados", true, true),
            D("ccih.epidemic.curve", ReportModule.InfectionControl, "Curva epidêmica de infecções", true, true, 1,
                "Inspirado em R4EPI/sitrep — casos por semana epidemiológica"),
            D("ccih.mortality.surveillance", ReportModule.InfectionControl, "Vigilância de mortalidade hospitalar", true, true, 1,
                "Inspirado em sitrep mortality — óbitos por semana"),
            D("ccih.vaccination.coverage", ReportModule.InfectionControl, "Cobertura vacinal por imunobiológico", true, true, 1,
                "Inspirado em sitrep vacinação"),
            D("ccih.outbreak.indicators", ReportModule.InfectionControl, "Indicadores de surto (CCIH)", true, true, 2,
                "Taxa de infecção, casos recentes e alertas — referência dev-queiroz"),
            D("ccih.anvisa-indicators", ReportModule.InfectionControl, "Indicadores da ANVISA", false, false, 3),

            // ── 18. Auditoria ──
            D("audit.medical", ReportModule.Audit, "Auditoria médica", false, false, 2),
            D("audit.nursing", ReportModule.Audit, "Auditoria de enfermagem", false, false, 2),
            D("audit.record-changes", ReportModule.Audit, "Alterações em prontuários", true, true),
            D("audit.access-log", ReportModule.Audit, "Log de acesso", true, true),
            D("audit.change-log", ReportModule.Audit, "Log de alterações", true, true),
            D("audit.access-by-user", ReportModule.Audit, "Acessos por usuário", true, true),
            D("audit.unauthorized-attempts", ReportModule.Audit, "Tentativas de acesso indevidas", false, true, 2),

            // ── 19. BI ──
            D("bi.occupancy-rate", ReportModule.BusinessIntelligence, "Taxa de ocupação", true, true),
            D("bi.revenue.daily", ReportModule.BusinessIntelligence, "Receita diária", true, true),
            D("bi.revenue.monthly", ReportModule.BusinessIntelligence, "Receita mensal", true, true),
            D("bi.ticket.avg", ReportModule.BusinessIntelligence, "Ticket médio", true, true),
            D("bi.hospitalizations", ReportModule.BusinessIntelligence, "Internações", true, true),
            D("bi.discharges", ReportModule.BusinessIntelligence, "Altas", true, true),
            D("bi.deaths", ReportModule.BusinessIntelligence, "Óbitos", true, true),
            D("bi.medical-production", ReportModule.BusinessIntelligence, "Produção médica", true, true),
            D("bi.production.by-specialty", ReportModule.BusinessIntelligence, "Produção por especialidade", true, true),
            D("bi.indicators.clinical", ReportModule.BusinessIntelligence, "Indicadores assistenciais", false, true, 2),
            D("bi.indicators.financial", ReportModule.BusinessIntelligence, "Indicadores financeiros", true, true),

            // ── 20. Regulatório ──
            D("reg.tiss", ReportModule.Regulatory, "TISS", true, true),
            D("reg.ciha", ReportModule.Regulatory, "CIHA", false, true, 3),
            D("reg.bpa", ReportModule.Regulatory, "BPA", false, true, 3),
            D("reg.apac", ReportModule.Regulatory, "APAC", false, true, 3),
            D("reg.aih", ReportModule.Regulatory, "AIH", false, true, 3),
            D("reg.cnes", ReportModule.Regulatory, "CNES", false, true, 3),
            D("reg.sih-sus", ReportModule.Regulatory, "SIH/SUS", false, true, 3),
            D("reg.sia-sus", ReportModule.Regulatory, "SIA/SUS", false, true, 3),
            D("reg.esus", ReportModule.Regulatory, "e-SUS", false, true, 3),
            D("reg.compulsory-notifications", ReportModule.Regulatory, "Notificações compulsórias", false, true, 3),
            D("reg.ambulatory-production", ReportModule.Regulatory, "Produção ambulatorial", false, true, 2),
            D("reg.hospital-production", ReportModule.Regulatory, "Produção hospitalar", false, true, 2),
            D("reg.ambulance.operations", ReportModule.Regulatory, "Operações de ambulância", true, true),
            D("lab.pathology.summary", ReportModule.Laboratory, "Resumo de patologia", true, true),
        };

        ExpandToTargets(items);
        return items;
    }

    private static void ExpandToTargets(List<ReportDefinition> items)
    {
        foreach (var (module, target) in ModuleTargets)
        {
            var current = items.Count(i => i.Module == module);
            var label = GetModuleLabel(module);
            for (var n = current + 1; n <= target; n++)
            {
                var phase = n <= current + (target - current) / 2 ? 2 : 3;
                items.Add(new ReportDefinition(
                    $"{module.ToString().ToLowerInvariant()}.planned.{n:D2}",
                    $"{label} — relatório analítico #{n}",
                    module,
                    $"Relatório complementar planejado para fase {phase}.",
                    false,
                    false,
                    phase));
            }
        }
    }
}

using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Serialization;

public static partial class PortugueseEnumRegistry
{
    private static void RegisterAdministrativeMaps(Dictionary<Type, EnumMap> maps)
    {
        Register(maps,
            (PaymentMethod.Cash, "Dinheiro"),
            (PaymentMethod.Pix, "PIX"),
            (PaymentMethod.DebitCard, "Cartão de débito"),
            (PaymentMethod.CreditCard, "Cartão de crédito"),
            (PaymentMethod.BankTransfer, "Transferência"));

        Register(maps,
            (FinancialAccountStatus.Open, "Em aberto"),
            (FinancialAccountStatus.PartiallyPaid, "Parcial"),
            (FinancialAccountStatus.Paid, "Pago"),
            (FinancialAccountStatus.Cancelled, "Cancelado"));

        Register(maps,
            (FinancialAccountDirection.Receivable, "A receber"),
            (FinancialAccountDirection.Payable, "A pagar"));

        Register(maps,
            (FinancialAccountCategory.Consultation, "Consulta"),
            (FinancialAccountCategory.Hospitalization, "Internação"),
            (FinancialAccountCategory.Exam, "Exame"),
            (FinancialAccountCategory.Copayment, "Coparticipação"),
            (FinancialAccountCategory.Parking, "Estacionamento"),
            (FinancialAccountCategory.Other, "Outros"),
            (FinancialAccountCategory.SupplierPurchase, "Compra / fornecedor"),
            (FinancialAccountCategory.Payroll, "Folha de pagamento"),
            (FinancialAccountCategory.Utilities, "Utilidades"),
            (FinancialAccountCategory.Taxes, "Impostos e taxas"),
            (FinancialAccountCategory.Maintenance, "Manutenção"),
            (FinancialAccountCategory.OtherExpense, "Outras despesas"),
            (FinancialAccountCategory.InsuranceReceivable, "Recebível convênio"));

        Register(maps,
            (FinancialCashSessionStatus.Open, "Aberta"),
            (FinancialCashSessionStatus.Closed, "Fechada"));

        Register(maps,
            (PixChargeStatus.Pending, "Pendente"),
            (PixChargeStatus.Paid, "Pago"),
            (PixChargeStatus.Expired, "Expirado"),
            (PixChargeStatus.Cancelled, "Cancelado"));

        Register(maps,
            (UserRole.Admin, "Administrador"),
            (UserRole.Reception, "Recepção"),
            (UserRole.Doctor, "Médico"),
            (UserRole.Patient, "Paciente"),
            (UserRole.HospitalDirector, "Diretor hospitalar"),
            (UserRole.Nurse, "Enfermeiro"),
            (UserRole.NursingTechnician, "Téc. enfermagem"),
            (UserRole.Billing, "Faturista"),
            (UserRole.Pharmacy, "Farmácia"),
            (UserRole.Warehouse, "Almoxarifado"),
            (UserRole.Porter, "Maqueiro"),
            (UserRole.Hospitality, "Hotelaria"),
            (UserRole.IT, "TI"),
            (UserRole.Auditor, "Auditor"),
            (UserRole.Insurance, "Convênios"));

        Register(maps,
            (EmployeeRole.Nurse, "Enfermeiro"),
            (EmployeeRole.Technician, "Técnico"),
            (EmployeeRole.Administrative, "Administrativo"),
            (EmployeeRole.Manager, "Gestor"),
            (EmployeeRole.Other, "Outro"));

        Register(maps,
            (ShiftType.Morning, "Manhã"),
            (ShiftType.Afternoon, "Tarde"),
            (ShiftType.Night, "Noite"));

        Register(maps,
            (HrEventType.Vacation, "Férias"),
            (HrEventType.Training, "Treinamento"),
            (HrEventType.PerformanceReview, "Avaliação de desempenho"));

        Register(maps,
            (TpaClaimStatus.Draft, "Rascunho"),
            (TpaClaimStatus.Submitted, "Enviado"),
            (TpaClaimStatus.Approved, "Aprovado"),
            (TpaClaimStatus.Denied, "Negado"),
            (TpaClaimStatus.Paid, "Pago"));

        Register(maps,
            (PayrollRunStatus.Draft, "Rascunho"),
            (PayrollRunStatus.Generated, "Gerado"),
            (PayrollRunStatus.Approved, "Aprovado"),
            (PayrollRunStatus.Paid, "Pago"));

        Register(maps,
            (PayrollLineType.Earning, "Provento"),
            (PayrollLineType.Discount, "Desconto"));

        Register(maps,
            (PharmacyBillingPayerType.Private, "Particular"),
            (PharmacyBillingPayerType.Insurance, "Convênio"));

        Register(maps,
            (EligibilityStatus.Eligible, "Elegível"),
            (EligibilityStatus.Ineligible, "Inelegível"),
            (EligibilityStatus.Pending, "Pendente"),
            (EligibilityStatus.Error, "Erro"));

        Register(maps,
            (InsuranceAuthorizationType.Consultation, "Consulta"),
            (InsuranceAuthorizationType.SpSadt, "SP/SADT"),
            (InsuranceAuthorizationType.Hospitalization, "Internação"),
            (InsuranceAuthorizationType.Opme, "OPME"),
            (InsuranceAuthorizationType.Extension, "Prorrogação"));

        Register(maps,
            (InsuranceAuthorizationStatus.Requested, "Solicitada"),
            (InsuranceAuthorizationStatus.Approved, "Aprovada"),
            (InsuranceAuthorizationStatus.Denied, "Negada"),
            (InsuranceAuthorizationStatus.Partial, "Parcial"),
            (InsuranceAuthorizationStatus.Expired, "Expirada"),
            (InsuranceAuthorizationStatus.Cancelled, "Cancelada"));

        Register(maps,
            (TissBatchStatus.Draft, "Rascunho"),
            (TissBatchStatus.Generated, "Gerado"),
            (TissBatchStatus.Sent, "Enviado"),
            (TissBatchStatus.Processed, "Processado"),
            (TissBatchStatus.Rejected, "Rejeitado"));

        Register(maps,
            (GlosaContestationStatus.None, "Nenhuma"),
            (GlosaContestationStatus.Submitted, "Enviada"),
            (GlosaContestationStatus.Accepted, "Aceita"),
            (GlosaContestationStatus.Rejected, "Rejeitada"));

        Register(maps,
            (TissGuideStatus.Draft, "Rascunho"),
            (TissGuideStatus.Sent, "Enviada"),
            (TissGuideStatus.Paid, "Paga"),
            (TissGuideStatus.Glosa, "Glosa"),
            (TissGuideStatus.Cancelled, "Cancelada"));

        Register(maps,
            (TissGuideType.Consultation, "Consulta"),
            (TissGuideType.SpSadt, "SP/SADT"),
            (TissGuideType.Hospitalization, "Internação"),
            (TissGuideType.DischargeSummary, "Resumo de alta"),
            (TissGuideType.IndividualFees, "Honorários individuais"),
            (TissGuideType.HospitalizationRequest, "Solicitação de internação"),
            (TissGuideType.OtherExpenses, "Outras despesas"),
            (TissGuideType.PresenceProof, "Comprovante de presença"),
            (TissGuideType.ExtensionRequest, "Pedido de prorrogação"),
            (TissGuideType.GlosaAppeal, "Recurso de glosa"),
            (TissGuideType.PaymentStatement, "Demonstrativo de pagamento"),
            (TissGuideType.DentalTreatment, "Tratamento odontológico"),
            (TissGuideType.DentalInitialAnnex, "Anexo odonto inicial"),
            (TissGuideType.DentalPaymentStatement, "Demonstrativo odonto"),
            (TissGuideType.DentalGlosaAppeal, "Recurso glosa odonto"),
            (TissGuideType.OpmeAnnex, "Anexo OPME"),
            (TissGuideType.ChemotherapyAnnex, "Anexo quimioterapia"),
            (TissGuideType.RadiotherapyAnnex, "Anexo radioterapia"),
            (TissGuideType.MonitoringReport, "Relatório de monitoramento"));

        Register(maps,
            (TissServiceCharacter.Elective, "Eletivo"),
            (TissServiceCharacter.Urgent, "Urgente"),
            (TissServiceCharacter.Emergency, "Emergência"));

        Register(maps,
            (TissAccidentIndicator.NotApplicable, "Não se aplica"),
            (TissAccidentIndicator.WorkAccident, "Acidente de trabalho"),
            (TissAccidentIndicator.TrafficAccident, "Acidente de trânsito"),
            (TissAccidentIndicator.OtherAccidents, "Outros acidentes"));

        Register(maps,
            (TissProfessionalRole.Surgeon, "Cirurgião"),
            (TissProfessionalRole.FirstAssistant, "Primeiro auxiliar"),
            (TissProfessionalRole.SecondAssistant, "Segundo auxiliar"),
            (TissProfessionalRole.Anesthesiologist, "Anestesista"),
            (TissProfessionalRole.Instrumentator, "Instrumentador"));

        Register(maps,
            (TissPriceTableSource.Tuss, "TUSS"),
            (TissPriceTableSource.Cbhpm, "CBHPM"),
            (TissPriceTableSource.Brasindice, "Brasíndice"),
            (TissPriceTableSource.Simpro, "Simpro"),
            (TissPriceTableSource.Manual, "Manual"));

        Register(maps,
            (TussTableType.Procedure, "Procedimento"),
            (TussTableType.Material, "Material"),
            (TussTableType.Medication, "Medicamento"),
            (TussTableType.Daily, "Diária"),
            (TussTableType.Fee, "Taxa"),
            (TussTableType.Package, "Pacote"));

        Register(maps,
            (TissAnnexType.Chemotherapy, "Quimioterapia"),
            (TissAnnexType.Radiotherapy, "Radioterapia"),
            (TissAnnexType.Opme, "OPME"),
            (TissAnnexType.SpecialRequest, "Solicitação especial"));

        Register(maps,
            (TissDemonstrativoStatus.Imported, "Importado"),
            (TissDemonstrativoStatus.Processed, "Processado"),
            (TissDemonstrativoStatus.PartiallyProcessed, "Processado parcial"),
            (TissDemonstrativoStatus.Error, "Erro"));

        Register(maps,
            (OperatorTransactionType.Eligibility, "Elegibilidade"),
            (OperatorTransactionType.Authorization, "Autorização"),
            (OperatorTransactionType.BatchSend, "Envio de lote"),
            (OperatorTransactionType.DemonstrativoFetch, "Busca demonstrativo"));

        Register(maps,
            (OperatorTransactionStatus.Success, "Sucesso"),
            (OperatorTransactionStatus.Failure, "Falha"),
            (OperatorTransactionStatus.Pending, "Pendente"));

        Register(maps,
            (SusGuideType.Bpa, "BPA"),
            (SusGuideType.Apac, "APAC"),
            (SusGuideType.Aih, "AIH"));

        Register(maps,
            (SusGuideStatus.Draft, "Rascunho"),
            (SusGuideStatus.Submitted, "Enviada"),
            (SusGuideStatus.Authorized, "Autorizada"),
            (SusGuideStatus.Billed, "Faturada"),
            (SusGuideStatus.Glosa, "Glosa"),
            (SusGuideStatus.Cancelled, "Cancelada"));

        Register(maps,
            (PendingItemStatus.Aberta, "Aberta"),
            (PendingItemStatus.EmAndamento, "Em andamento"),
            (PendingItemStatus.Concluida, "Concluída"),
            (PendingItemStatus.Cancelada, "Cancelada"));

        Register(maps,
            (PendingItemPriority.Baixa, "Baixa"),
            (PendingItemPriority.Normal, "Normal"),
            (PendingItemPriority.Alta, "Alta"),
            (PendingItemPriority.Critica, "Crítica"));

        Register(maps,
            (PendingModule.Connect, "Connect"),
            (PendingModule.Mail, "E-mail"),
            (PendingModule.Chat, "Chat"),
            (PendingModule.Guides, "Guias"),
            (PendingModule.Inventory, "Estoque"),
            (PendingModule.Financial, "Financeiro"),
            (PendingModule.System, "Sistema"),
            (PendingModule.Tickets, "Chamados"),
            (PendingModule.Tasks, "Tarefas"),
            (PendingModule.Hotelaria, "Hotelaria"),
            (PendingModule.Nursing, "Enfermagem"),
            (PendingModule.Reception, "Recepção"));

        Register(maps,
            (PendingItemType.TicketOverdue, "Chamado atrasado"),
            (PendingItemType.UnreadMail, "E-mail não lido"),
            (PendingItemType.UnreadChat, "Chat não lido"),
            (PendingItemType.GuideDraft, "Guia em rascunho"),
            (PendingItemType.LowStock, "Estoque baixo"),
            (PendingItemType.TaskOverdue, "Tarefa atrasada"),
            (PendingItemType.SystemAlert, "Alerta do sistema"),
            (PendingItemType.WorkflowPending, "Workflow pendente"),
            (PendingItemType.BedCleaning, "Higienização de leito"),
            (PendingItemType.UnsignedPrescription, "Prescrição não assinada"),
            (PendingItemType.CheckInPending, "Check-in pendente"),
            (PendingItemType.EligibilityRequired, "Elegibilidade necessária"));

        Register(maps,
            (ReportModule.Administrative, "Administrativo"),
            (ReportModule.Reception, "Recepção"),
            (ReportModule.Emergency, "Pronto-socorro"),
            (ReportModule.Hospitalization, "Internação"),
            (ReportModule.MedicalRecord, "Prontuário"),
            (ReportModule.Nursing, "Enfermagem"),
            (ReportModule.Surgery, "Centro cirúrgico"),
            (ReportModule.Pharmacy, "Farmácia"),
            (ReportModule.Supply, "Suprimentos"),
            (ReportModule.Laboratory, "Laboratório"),
            (ReportModule.Imaging, "Imagem"),
            (ReportModule.Financial, "Financeiro"),
            (ReportModule.Insurance, "Convênios"),
            (ReportModule.HospitalBilling, "Faturamento hospitalar"),
            (ReportModule.HumanResources, "Recursos humanos"),
            (ReportModule.Quality, "Qualidade"),
            (ReportModule.InfectionControl, "CCIH"),
            (ReportModule.Audit, "Auditoria"),
            (ReportModule.BusinessIntelligence, "Inteligência de negócios"),
            (ReportModule.Regulatory, "Regulatório"));

        Register(maps,
            (HospitalReferenceCatalogType.UserType, "Tipo de usuário"),
            (HospitalReferenceCatalogType.HospitalSector, "Setor hospitalar"),
            (HospitalReferenceCatalogType.Ward, "Ala"),
            (HospitalReferenceCatalogType.BedType, "Tipo de leito"),
            (HospitalReferenceCatalogType.SupplierType, "Tipo de fornecedor"),
            (HospitalReferenceCatalogType.ProductType, "Tipo de produto"),
            (HospitalReferenceCatalogType.ServiceType, "Tipo de serviço"),
            (HospitalReferenceCatalogType.MedicalSpecialty, "Especialidade médica"),
            (HospitalReferenceCatalogType.LabExam, "Exame laboratorial"),
            (HospitalReferenceCatalogType.ImagingExam, "Exame de imagem"),
            (HospitalReferenceCatalogType.TissGuideType, "Tipo de guia TISS"),
            (HospitalReferenceCatalogType.SystemMenu, "Menu do sistema"),
            (HospitalReferenceCatalogType.PermissionAction, "Ação de permissão"),
            (HospitalReferenceCatalogType.ReadyProfile, "Perfil pronto"),
            (HospitalReferenceCatalogType.RegulatoryBase, "Base regulatória"),
            (HospitalReferenceCatalogType.RecommendedModule, "Módulo recomendado"));

        Register(maps,
            (HelpArticleType.Faq, "FAQ"),
            (HelpArticleType.Article, "Artigo"),
            (HelpArticleType.Video, "Vídeo"),
            (HelpArticleType.Manual, "Manual"),
            (HelpArticleType.Training, "Treinamento"));

        Register(maps,
            (HelpSuggestionStatus.Pendente, "Pendente"),
            (HelpSuggestionStatus.EmAnalise, "Em análise"),
            (HelpSuggestionStatus.Aceita, "Aceita"),
            (HelpSuggestionStatus.Rejeitada, "Rejeitada"),
            (HelpSuggestionStatus.Implementada, "Implementada"));
    }
}

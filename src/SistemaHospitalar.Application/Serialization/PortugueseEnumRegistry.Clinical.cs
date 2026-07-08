using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Serialization;

public static partial class PortugueseEnumRegistry
{
    private static void RegisterClinicalMaps(Dictionary<Type, EnumMap> maps)
    {
        Register(maps,
            (AppointmentStatus.Scheduled, "Agendado"),
            (AppointmentStatus.Confirmed, "Confirmado"),
            (AppointmentStatus.InProgress, "Em atendimento"),
            (AppointmentStatus.Completed, "Concluído"),
            (AppointmentStatus.Cancelled, "Cancelado"),
            (AppointmentStatus.NoShow, "Faltou"));

        Register(maps,
            (EmergencyVisitStatus.Waiting, "Aguardando"),
            (EmergencyVisitStatus.InCare, "Em atendimento"),
            (EmergencyVisitStatus.Discharged, "Alta"),
            (EmergencyVisitStatus.Referred, "Encaminhado"));

        Register(maps,
            (TriageUrgency.Low, "Baixa"),
            (TriageUrgency.Medium, "Média"),
            (TriageUrgency.High, "Alta"),
            (TriageUrgency.Emergency, "Emergência"),
            (TriageUrgency.NonUrgent, "Não urgente"));

        Register(maps,
            (Gender.NotInformed, "Não informado"),
            (Gender.Male, "Masculino"),
            (Gender.Female, "Feminino"),
            (Gender.Other, "Outro"));

        Register(maps,
            (BedStatus.Available, "Disponível"),
            (BedStatus.Occupied, "Ocupado"),
            (BedStatus.Maintenance, "Manutenção"),
            (BedStatus.Cleaning, "Higienização"),
            (BedStatus.Reserved, "Reservado"));

        Register(maps,
            (HospitalizationStatus.Active, "Internado"),
            (HospitalizationStatus.Discharged, "Alta"),
            (HospitalizationStatus.Transferred, "Transferido"));

        Register(maps,
            (HospitalizationRequestStatus.Pending, "Pendente"),
            (HospitalizationRequestStatus.Approved, "Aprovada"),
            (HospitalizationRequestStatus.Rejected, "Rejeitada"),
            (HospitalizationRequestStatus.Admitted, "Internado"),
            (HospitalizationRequestStatus.Cancelled, "Cancelada"));

        Register(maps,
            (HospitalizationRequestPriority.Elective, "Eletiva"),
            (HospitalizationRequestPriority.Urgent, "Urgente"),
            (HospitalizationRequestPriority.Emergency, "Emergência"));

        Register(maps,
            (HospitalizationSnippetType.Reason, "Motivo"),
            (HospitalizationSnippetType.Diagnosis, "Diagnóstico"));

        Register(maps,
            (SusHospitalizationCharacter.Elective, "Eletiva"),
            (SusHospitalizationCharacter.Urgent, "Urgência"),
            (SusHospitalizationCharacter.Emergency, "Emergência"));

        Register(maps,
            (SusHospitalizationModality.Clinical, "Clínica"),
            (SusHospitalizationModality.Surgical, "Cirúrgica"),
            (SusHospitalizationModality.Obstetric, "Obstétrica"),
            (SusHospitalizationModality.Pediatric, "Pediátrica"),
            (SusHospitalizationModality.Psychiatric, "Psiquiátrica"));

        Register(maps,
            (WardCategory.Enfermaria, "Enfermaria"),
            (WardCategory.Apartamento, "Apartamento"),
            (WardCategory.Uti, "UTI"),
            (WardCategory.Pediatrica, "Pediátrica"),
            (WardCategory.Maternidade, "Maternidade"));

        Register(maps,
            (WardCoverageModality.Particular, "Particular"),
            (WardCoverageModality.Convenio, "Convênio"),
            (WardCoverageModality.Sus, "SUS"),
            (WardCoverageModality.Mixed, "Misto"));

        Register(maps,
            (OperatingRoomStatus.Available, "Disponível"),
            (OperatingRoomStatus.InUse, "Em uso"),
            (OperatingRoomStatus.Maintenance, "Manutenção"));

        Register(maps,
            (SurgeryStatus.Scheduled, "Agendada"),
            (SurgeryStatus.InProgress, "Em andamento"),
            (SurgeryStatus.Completed, "Concluída"),
            (SurgeryStatus.Cancelled, "Cancelada"));

        Register(maps,
            (LabOrderStatus.Requested, "Solicitado"),
            (LabOrderStatus.InProgress, "Em processamento"),
            (LabOrderStatus.Completed, "Concluído"),
            (LabOrderStatus.Cancelled, "Cancelado"));

        Register(maps,
            (LabItemStatus.Pending, "Pendente"),
            (LabItemStatus.Collected, "Coletado"),
            (LabItemStatus.Processing, "Processando"),
            (LabItemStatus.Completed, "Concluído"),
            (LabItemStatus.Cancelled, "Cancelado"));

        Register(maps,
            (ImagingModality.XRay, "Raio-X"),
            (ImagingModality.CT, "Tomografia"),
            (ImagingModality.MRI, "Ressonância"),
            (ImagingModality.Ultrasound, "Ultrassom"),
            (ImagingModality.Mammography, "Mamografia"));

        Register(maps,
            (ImagingStudyStatus.Scheduled, "Agendado"),
            (ImagingStudyStatus.InProgress, "Em andamento"),
            (ImagingStudyStatus.Completed, "Concluído"),
            (ImagingStudyStatus.Cancelled, "Cancelado"));

        Register(maps,
            (MedicalRecordEntryType.Anamnesis, "Anamnese"),
            (MedicalRecordEntryType.Evolution, "Evolução"),
            (MedicalRecordEntryType.Prescription, "Prescrição"),
            (MedicalRecordEntryType.ExamRequest, "Solicitação de exame"),
            (MedicalRecordEntryType.Procedure, "Procedimento"));

        Register(maps,
            (ClinicalDocumentKind.TissGuide, "Guia TISS"),
            (ClinicalDocumentKind.Report, "Laudo"));

        Register(maps,
            (PatientIdentityType.Bracelet, "Pulseira"),
            (PatientIdentityType.ExamLabel, "Etiqueta de exame"),
            (PatientIdentityType.MedicationLabel, "Etiqueta de medicamento"),
            (PatientIdentityType.SampleLabel, "Etiqueta de amostra"));

        Register(maps,
            (ClinicalSignatureType.Simple, "Simples"),
            (ClinicalSignatureType.Clinical, "Clínica"));

        Register(maps,
            (LegalResponsibleRelationship.NotInformed, "Não informado"),
            (LegalResponsibleRelationship.Father, "Pai"),
            (LegalResponsibleRelationship.Mother, "Mãe"),
            (LegalResponsibleRelationship.Spouse, "Cônjuge"),
            (LegalResponsibleRelationship.Other, "Outro"));

        Register(maps,
            (LegalAuthorizationDocumentType.NotInformed, "Não informado"),
            (LegalAuthorizationDocumentType.CuratorshipTerm, "Termo de curatela"),
            (LegalAuthorizationDocumentType.GuardianshipTerm, "Termo de tutela"),
            (LegalAuthorizationDocumentType.PublicPowerOfAttorney, "Procuração pública"));

        Register(maps,
            (BedEventType.Reserve, "Reservar"),
            (BedEventType.Block, "Bloquear"),
            (BedEventType.Occupy, "Ocupar"),
            (BedEventType.Release, "Liberar"));

        Register(maps,
            (PatientReferenceCatalogType.Race, "Raça"),
            (PatientReferenceCatalogType.Ethnicity, "Etnia"),
            (PatientReferenceCatalogType.Religion, "Religião"),
            (PatientReferenceCatalogType.MaritalStatus, "Estado civil"));

        Register(maps,
            (VaccineScheduleType.Child, "Infantil"),
            (VaccineScheduleType.Pregnant, "Gestante"),
            (VaccineScheduleType.NonPregnantAdult, "Adulto"));

        Register(maps,
            (EpidemicDiseaseClass.Notifiable, "Notificável"),
            (EpidemicDiseaseClass.Chronic, "Crônica"),
            (EpidemicDiseaseClass.MaternalPerinatal, "Materno-perinatal"),
            (EpidemicDiseaseClass.OtherCondition, "Outra condição"),
            (EpidemicDiseaseClass.Other, "Outro"));

        Register(maps,
            (ChemotherapySessionStatus.Scheduled, "Agendada"),
            (ChemotherapySessionStatus.InPreparation, "Em preparação"),
            (ChemotherapySessionStatus.Administered, "Administrada"),
            (ChemotherapySessionStatus.Completed, "Concluída"),
            (ChemotherapySessionStatus.Cancelled, "Cancelada"));

        Register(maps,
            (PhysiotherapySessionType.Mobility, "Mobilidade"),
            (PhysiotherapySessionType.Respiratory, "Respiratória"),
            (PhysiotherapySessionType.Neurological, "Neurológica"),
            (PhysiotherapySessionType.PostOperative, "Pós-operatório"),
            (PhysiotherapySessionType.Other, "Outra"));

        Register(maps,
            (PhysiotherapySessionStatus.Scheduled, "Agendada"),
            (PhysiotherapySessionStatus.InProgress, "Em andamento"),
            (PhysiotherapySessionStatus.Completed, "Concluída"),
            (PhysiotherapySessionStatus.Cancelled, "Cancelada"));

        Register(maps,
            (TelemedicineStatus.Scheduled, "Agendada"),
            (TelemedicineStatus.Waiting, "Aguardando"),
            (TelemedicineStatus.InProgress, "Em andamento"),
            (TelemedicineStatus.Completed, "Concluída"),
            (TelemedicineStatus.Cancelled, "Cancelada"),
            (TelemedicineStatus.NoShow, "Faltou"));

        Register(maps,
            (InfectionType.Urinary, "Urinária"),
            (InfectionType.Respiratory, "Respiratória"),
            (InfectionType.SurgicalSite, "Sítio cirúrgico"),
            (InfectionType.Bloodstream, "Corrente sanguínea"),
            (InfectionType.Other, "Outra"));

        Register(maps,
            (InfectionSurveillanceStatus.Suspected, "Suspeita"),
            (InfectionSurveillanceStatus.Confirmed, "Confirmada"),
            (InfectionSurveillanceStatus.Resolved, "Resolvida"));

        Register(maps,
            (IsolationPrecautionType.Contact, "Contato"),
            (IsolationPrecautionType.Droplet, "Gotículas"),
            (IsolationPrecautionType.Airborne, "Aerossóis"),
            (IsolationPrecautionType.Protective, "Protetora"));

        Register(maps,
            (IsolationPrecautionStatus.Active, "Ativa"),
            (IsolationPrecautionStatus.Lifted, "Suspensa"));

        Register(maps,
            (InstrumentKitStatus.Available, "Disponível"),
            (InstrumentKitStatus.InSterilization, "Em esterilização"),
            (InstrumentKitStatus.Sterile, "Estéril"),
            (InstrumentKitStatus.Expired, "Vencido"),
            (InstrumentKitStatus.InUse, "Em uso"));

        Register(maps,
            (SterilizationMethod.Steam, "Vapor"),
            (SterilizationMethod.Eto, "Óxido de etileno"),
            (SterilizationMethod.Plasma, "Plasma"));

        Register(maps,
            (SterilizationCycleStatus.Pending, "Pendente"),
            (SterilizationCycleStatus.InProgress, "Em andamento"),
            (SterilizationCycleStatus.Completed, "Concluído"),
            (SterilizationCycleStatus.Failed, "Falhou"));

        Register(maps,
            (BloodType.APositive, "A positivo"),
            (BloodType.ANegative, "A negativo"),
            (BloodType.BPositive, "B positivo"),
            (BloodType.BNegative, "B negativo"),
            (BloodType.ABPositive, "AB positivo"),
            (BloodType.ABNegative, "AB negativo"),
            (BloodType.OPositive, "O positivo"),
            (BloodType.ONegative, "O negativo"));

        Register(maps,
            (BloodComponent.WholeBlood, "Sangue total"),
            (BloodComponent.PackedRedCells, "Hemácias"),
            (BloodComponent.Platelets, "Plaquetas"),
            (BloodComponent.Plasma, "Plasma"));

        Register(maps,
            (BloodUnitStatus.Available, "Disponível"),
            (BloodUnitStatus.Reserved, "Reservada"),
            (BloodUnitStatus.Transfused, "Transfundida"),
            (BloodUnitStatus.Discarded, "Descartada"),
            (BloodUnitStatus.Expired, "Vencida"));

        Register(maps,
            (TransfusionRequestStatus.Requested, "Solicitada"),
            (TransfusionRequestStatus.Matched, "Compatibilizada"),
            (TransfusionRequestStatus.Transfused, "Transfundida"),
            (TransfusionRequestStatus.Cancelled, "Cancelada"));

        Register(maps,
            (DialysisSessionStatus.Scheduled, "Agendada"),
            (DialysisSessionStatus.InProgress, "Em andamento"),
            (DialysisSessionStatus.Completed, "Concluída"),
            (DialysisSessionStatus.Cancelled, "Cancelada"));

        Register(maps,
            (DietType.Regular, "Regular"),
            (DietType.Soft, "Pastosa"),
            (DietType.Liquid, "Líquida"),
            (DietType.Diabetic, "Diabética"),
            (DietType.LowSodium, "Baixo sódio"));

        Register(maps,
            (DietOrderStatus.Pending, "Pendente"),
            (DietOrderStatus.InPreparation, "Em preparo"),
            (DietOrderStatus.Delivered, "Entregue"),
            (DietOrderStatus.Cancelled, "Cancelada"));

        Register(maps,
            (MealPeriod.Breakfast, "Café da manhã"),
            (MealPeriod.Lunch, "Almoço"),
            (MealPeriod.Dinner, "Jantar"),
            (MealPeriod.Snack, "Lanche"));

        Register(maps,
            (ClinicalIncidentSeverity.Low, "Baixa"),
            (ClinicalIncidentSeverity.Moderate, "Moderada"),
            (ClinicalIncidentSeverity.High, "Alta"),
            (ClinicalIncidentSeverity.Severe, "Grave"));
    }
}

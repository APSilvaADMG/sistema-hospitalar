using SistemaHospitalar.Domain.Enums;

// InstrumentKitStatus lives in Domain.Enums (CME module)

namespace SistemaHospitalar.Domain.BusinessRules;

public static class HospitalBusinessRules
{
    public const int MinorAgeYears = 18;
    public const int CadastralUpdateMonths = 12;
    public const decimal CriticalBedOccupancyPercent = 90m;
    public const int TriageSlaMinutesEmergency = 0;
    public const int TriageSlaMinutesHigh = 10;
    public const int TriageSlaMinutesMedium = 60;
    public const int TriageSlaMinutesLow = 120;
    public const int TriageSlaMinutesNonUrgent = 240;

    public static int CalculateAgeYears(DateOnly birthDate, DateOnly? reference = null)
    {
        var today = reference ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age)) age--;
        return age;
    }

    public static bool IsMinor(DateOnly birthDate, DateOnly? reference = null)
        => CalculateAgeYears(birthDate, reference) < MinorAgeYears;

    public static void ValidateUniqueCpf(bool cpfExists, string? cpf = null)
    {
        if (!cpfExists) return;
        throw new InvalidOperationException(
            $"[{BusinessRuleCodes.UniqueCpf}] Já existe um prontuário cadastrado com este CPF.");
    }

    public static void ValidateUniqueCns(bool cnsExists)
    {
        if (!cnsExists) return;
        throw new InvalidOperationException(
            $"[{BusinessRuleCodes.UniqueCns}] Já existe um prontuário cadastrado com este CNS.");
    }

    public static void ValidateMinorHasResponsible(
        DateOnly birthDate,
        string? emergencyContactName,
        string? emergencyContactPhone,
        string? motherName = null,
        bool usesLegalResponsibleCpf = false)
    {
        if (!IsMinor(birthDate)) return;

        if (usesLegalResponsibleCpf)
        {
            return;
        }

        var hasGuardianContact = !string.IsNullOrWhiteSpace(emergencyContactName)
            && !string.IsNullOrWhiteSpace(emergencyContactPhone);

        if (!hasGuardianContact)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.MinorResponsible}] Paciente menor de idade deve ter responsável legal " +
                "(nome e telefone de contato de emergência) ou cadastro com CPF do responsável.");
        }
    }

    public static void ValidateInactivationReason(bool wasActive, bool willBeActive, string? reason)
    {
        if (wasActive && !willBeActive && string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.InactivationReason}] Informe o motivo da inativação do cadastro.");
        }
    }

    public static void ValidateCannotChangePatientAfterCareStarted(
        bool careStarted,
        Guid currentPatientId,
        Guid? requestedPatientId)
    {
        if (!requestedPatientId.HasValue || requestedPatientId.Value == currentPatientId)
        {
            return;
        }

        if (careStarted)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.PatientSwapBlocked}] Não é permitido trocar o paciente após o início do atendimento.");
        }
    }

    public static void ValidateVitalSignCorrectionAsNewEntry(bool isUpdateAttempt)
    {
        if (isUpdateAttempt)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.VitalSignImmutable}] Sinais vitais não podem ser sobrescritos — registre uma nova aferição com motivo da retificação.");
        }
    }

    public static void ValidateOccupiedBedBlocked(BedStatus status)
    {
        if (status is BedStatus.Occupied or BedStatus.Cleaning or BedStatus.Maintenance)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.OccupiedBedBlocked}] Leito indisponível para internação (ocupado, em higienização ou interditado).");
        }
    }

    public static void ValidateBedAvailableForAdmission(BedStatus status)
    {
        ValidateOccupiedBedBlocked(status);

        if (status is BedStatus.Available or BedStatus.Reserved)
        {
            return;
        }

        throw new InvalidOperationException(
            $"[{BusinessRuleCodes.BedAvailable}] Internação permitida somente em leito disponível ou reservado.");
    }

    public static void ValidateDispenseQuantity(decimal quantityOnHand, decimal requested)
    {
        if (requested <= 0)
        {
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");
        }

        if (quantityOnHand < requested)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.StockAvailable}] Estoque insuficiente. Disponível: {quantityOnHand}.");
        }
    }

    public static void ValidateMedicationNotExpired(DateOnly? batchExpiry)
    {
        if (!batchExpiry.HasValue)
        {
            return;
        }

        if (batchExpiry.Value < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.ExpiredMedication}] Medicamento com lote vencido — dispensação bloqueada (RN-016).");
        }
    }

    public static void ValidateTriageBeforeMedicalCare(TriageUrgency urgency, Guid? aiTriageLogId)
    {
        if (urgency is not (TriageUrgency.Emergency or TriageUrgency.High))
        {
            return;
        }

        if (aiTriageLogId is null)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.TriageRequired}] Triagem com sinais vitais obrigatória antes do atendimento médico (RN-007).");
        }
    }

    public static int GetTriageSlaMinutes(TriageUrgency urgency) => urgency switch
    {
        TriageUrgency.Emergency => TriageSlaMinutesEmergency,
        TriageUrgency.High => TriageSlaMinutesHigh,
        TriageUrgency.Medium => TriageSlaMinutesMedium,
        TriageUrgency.Low => TriageSlaMinutesLow,
        _ => TriageSlaMinutesNonUrgent
    };

    public static bool IsEmergencyWaitExceeded(DateTime arrivedAt, TriageUrgency urgency, DateTime? now = null)
    {
        var reference = now ?? DateTime.UtcNow;
        var waitedMinutes = (reference - arrivedAt).TotalMinutes;
        return waitedMinutes > GetTriageSlaMinutes(urgency);
    }

    public static void ValidateEligibleForCare(bool isActive, bool isDeceased)
    {
        if (!isActive)
        {
            throw new InvalidOperationException("Paciente inativo.");
        }

        if (isDeceased)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.DeceasedPatient}] Paciente com óbito registrado não pode receber novos atendimentos.");
        }
    }

    public static void ValidateCnsForSus(string? primaryInsuranceName, string? patientCns, string? insuranceCns)
    {
        if (string.IsNullOrWhiteSpace(primaryInsuranceName)
            || !primaryInsuranceName.Contains("SUS", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var cns = !string.IsNullOrWhiteSpace(patientCns) ? patientCns : insuranceCns;
        var digits = new string((cns ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length < 15)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.CnsRequiredSus}] Atendimento SUS exige CNS válido (15 dígitos).");
        }
    }

    public static void ValidateOmsBeforeSurgeryStart(
        bool consentConfirmed,
        bool omsSignInCompleted,
        bool omsTimeOutCompleted)
    {
        if (!consentConfirmed)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.SurgeryConsent}] Cirurgia bloqueada: consentimento não confirmado (RN-021).");
        }

        if (!omsSignInCompleted || !omsTimeOutCompleted)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.OmsChecklist}] Cirurgia bloqueada: checklist OMS incompleto — Sign In e Time Out obrigatórios (RN-020).");
        }
    }

    public static void ValidateBillingAccountClosed(DateTime? accountClosedAt)
    {
        if (accountClosedAt.HasValue) return;
        throw new InvalidOperationException(
            $"[{BusinessRuleCodes.AccountClosed}] Conta hospitalar não auditada/fechada. Feche a conta antes de faturar (RN-028).");
    }

    public static void ValidateTissGuideReadyForBilling(
        DateTime? accountClosedAt,
        IEnumerable<bool> itemAuditedFlags,
        int activeItemCount)
    {
        if (activeItemCount == 0)
        {
            throw new InvalidOperationException("Guia sem itens não pode ser faturada.");
        }

        if (!accountClosedAt.HasValue)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.AccountClosed}] Conta não fechada. Audite os itens e feche a conta antes do envio (RN-028).");
        }

        if (itemAuditedFlags.Any(a => !a))
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.AccountClosed}] Todos os itens devem ser auditados antes do faturamento (RN-028).");
        }
    }

    public static void ValidateSterileKit(InstrumentKitStatus status, DateOnly? sterilityExpiration)
    {
        if (status != InstrumentKitStatus.Sterile && status != InstrumentKitStatus.Available)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.SterileKit}] Instrumental não esterilizado não pode ser liberado (RN-021).");
        }

        if (sterilityExpiration.HasValue && sterilityExpiration.Value < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.ExpiredKit}] Material estéril vencido — uso bloqueado (RN-022).");
        }
    }

    public static void ValidateOneBedPerPatient(bool hasActiveHospitalization)
    {
        if (hasActiveHospitalization)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.OneBedPerPatient}] Paciente já possui internação ativa — apenas um leito por paciente.");
        }
    }

    public static void ValidateMedicationPatientIdentified(bool identityMatchesPatient)
    {
        if (!identityMatchesPatient)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.MedicationPatientId}] Identifique o paciente antes de administrar medicamento.");
        }
    }
}

public static class BusinessRuleCodes
{
    public const string UniqueCpf = "RN-001";
    public const string UniqueCns = "RN-001b";
    public const string PatientCpfRequired = "RN-004";
    public const string LegalResponsibleRequired = "RN-005";
    public const string LegalResponsibleAdult = "RN-006";
    public const string LegalAuthorizationRequired = "RN-003b";
    public const string CnsRequiredSus = "RN-002";
    public const string MinorResponsible = "RN-003";
    public const string BedAvailable = "RN-016";
    public const string BedCleaning = "RN-017";
    public const string StockAvailable = "RN-023";
    public const string ExpiredMedication = "RN-016";
    public const string TriageRequired = "RN-007";
    public const string OmsChecklist = "RN-020";
    public const string SurgeryConsent = "RN-021";
    public const string SterileKit = "RN-021b";
    public const string ExpiredKit = "RN-022";
    public const string DeceasedPatient = "RN-047";
    public const string AccountClosed = "RN-028";
    public const string SoftDeleteOnly = "BR-CORE-001";
    public const string InactivationReason = "RN-GER-006";
    public const string AuditCriticalChange = "RN-GER-005";
    public const string PatientNumberFormat = "RN-PAC-001";
    public const string DuplicatePatientSignal = "RN-PAC-025";
    public const string PatientSwapBlocked = "RN-ATD-003";
    public const string StatusChangeHistory = "RN-ATD-007";
    public const string ScheduleConflict = "RN-AGD-008";
    public const string PrescriptionAllergy = "RN-PRE-006";
    public const string MedicationPatientId = "RN-ADM-002";
    public const string VitalSignImmutable = "RN-TRI-004";
    public const string OneBedPerPatient = "RN-INT-003";
    public const string OccupiedBedBlocked = "RN-INT-006";
    public const string LotTraceabilityRequired = "RN-MAT-020";
    public const string DisposableNoReturn = "RN-MAT-022";
    public const string CadastralChangeAudited = "RN-MAT-025";
    public const string FefoOutbound = "RN-023";
    public const string CancellationJustification = "RN-CAN-001";
    public const string DischargePendingChecks = "RN-ALT-004";
    public const string FiveRights = "RN-011b";
    public const string AmbulanceMaintenanceBlocksDispatch = "RN-AMB-008";
    public const string AmbulanceUniqueCode = "RN-AMB-001";
    public const string AmbulanceUniquePlate = "RN-AMB-002";
    public const string TelemedicineFutureSchedule = "RN-TEL-001";
    public const string TelemedicineSessionStart = "RN-TEL-002";
    public const string SterilizationFailedNoSterile = "RN-EST-004";
    public const string SterilizationUniqueCycle = "RN-EST-001";
    public const string SterilizationKitInProgress = "RN-EST-002";
    public const string EquipmentMaintenanceBlocksReservation = "RN-EQP-016";
    public const string EquipmentCalibrationDue = "RN-EQP-017";
    public const string SupplierBlocked = "RN-FOR-010";
}

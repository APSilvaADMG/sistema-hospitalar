namespace SistemaHospitalar.Domain.Security;

public static class PermissionCodes
{
    public const string PatientsCreate = "patients.create";
    public const string PatientsRead = "patients.read";
    public const string PatientsUpdate = "patients.update";
    public const string PepRead = "pep.read";
    public const string PepWrite = "pep.write";
    public const string BillingRead = "billing.read";
    public const string BillingWrite = "billing.write";
    public const string PharmacyDispense = "pharmacy.dispense";
    public const string WarehouseManage = "warehouse.manage";
    public const string TransportOperate = "transport.operate";
    public const string TransportManage = "transport.manage";
    public const string CleaningOperate = "cleaning.operate";
    public const string CleaningManage = "cleaning.manage";
    public const string HospitalizationManage = "hospitalization.manage";
    public const string ReportsRead = "reports.read";
    public const string UsersManage = "users.manage";
    public const string AuditRead = "audit.read";
    public const string SecurityManage = "security.manage";
    public const string LgpdManage = "lgpd.manage";
    public const string LgpdConsentManage = "lgpd.consent.manage";
    public const string LgpdSubjectRequests = "lgpd.subject_requests";
    public const string IncidentsManage = "incidents.manage";
    public const string ConnectRead = "connect.read";
    public const string ConnectWrite = "connect.write";
    public const string ConnectAdmin = "connect.admin";
    public const string ConnectApprove = "connect.approve";
    public const string IntegrationsManage = "integrations.manage";
    public const string TpaManage = "tpa.manage";
    public const string PayrollManage = "payroll.manage";
    public const string PharmacyBillingManage = "pharmacy.billing.manage";

    public static IReadOnlyList<string> All { get; } =
    [
        PatientsCreate, PatientsRead, PatientsUpdate,
        PepRead, PepWrite,
        BillingRead, BillingWrite,
        PharmacyDispense, WarehouseManage,
        TransportOperate, TransportManage,
        CleaningOperate, CleaningManage,
        HospitalizationManage,
        ReportsRead, UsersManage, AuditRead,
        SecurityManage, LgpdManage, LgpdConsentManage,
        LgpdSubjectRequests, IncidentsManage,
        ConnectRead, ConnectWrite, ConnectAdmin, ConnectApprove,
        IntegrationsManage,
        TpaManage, PayrollManage, PharmacyBillingManage,
    ];
}

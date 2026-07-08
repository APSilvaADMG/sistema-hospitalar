using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class RolePermissionSeed
{
  private static readonly (string Code, string Name, string Module)[] Definitions =
  [
      (PermissionCodes.PatientsCreate, "Cadastrar paciente", "Pacientes"),
      (PermissionCodes.PatientsRead, "Visualizar paciente", "Pacientes"),
      (PermissionCodes.PatientsUpdate, "Alterar cadastro de paciente", "Pacientes"),
      (PermissionCodes.PepRead, "Visualizar prontuário", "PEP"),
      (PermissionCodes.PepWrite, "Evoluir prontuário", "PEP"),
      (PermissionCodes.BillingRead, "Visualizar faturamento", "Faturamento"),
      (PermissionCodes.BillingWrite, "Alterar faturamento", "Faturamento"),
      (PermissionCodes.PharmacyDispense, "Dispensar medicamento", "Farmácia"),
      (PermissionCodes.WarehouseManage, "Gerenciar almoxarifado", "Estoque"),
      (PermissionCodes.TransportOperate, "Operar transportes", "Transportes"),
      (PermissionCodes.TransportManage, "Gerenciar transportes", "Transportes"),
      (PermissionCodes.CleaningOperate, "Operar higienização", "Hotelaria"),
      (PermissionCodes.CleaningManage, "Gerenciar higienização", "Hotelaria"),
      (PermissionCodes.HospitalizationManage, "Gerenciar internação", "Internação"),
      (PermissionCodes.ReportsRead, "Visualizar relatórios", "Relatórios"),
      (PermissionCodes.UsersManage, "Gerenciar usuários", "Segurança"),
      (PermissionCodes.AuditRead, "Visualizar auditoria", "Segurança"),
      (PermissionCodes.SecurityManage, "Administrar segurança", "Segurança"),
      (PermissionCodes.LgpdManage, "Administrar LGPD", "LGPD"),
      (PermissionCodes.LgpdConsentManage, "Gerenciar consentimentos", "LGPD"),
      (PermissionCodes.LgpdSubjectRequests, "Solicitações do titular", "LGPD"),
      (PermissionCodes.IncidentsManage, "Gestão de incidentes", "LGPD"),
      (PermissionCodes.ConnectRead, "Visualizar APSMed Connect", "Connect"),
      (PermissionCodes.ConnectWrite, "Operar APSMed Connect", "Connect"),
      (PermissionCodes.ConnectAdmin, "Administrar mural e comunicação", "Connect"),
      (PermissionCodes.ConnectApprove, "Aprovar solicitações no Connect", "Connect"),
      (PermissionCodes.IntegrationsManage, "Gerenciar integrações e atualizações oficiais", "Integrações"),
      (PermissionCodes.TpaManage, "Gerenciar TPA", "Convênios"),
      (PermissionCodes.PayrollManage, "Gerenciar folha de pagamento", "RH"),
      (PermissionCodes.PharmacyBillingManage, "Gerenciar faturamento farmácia", "Farmácia"),
  ];

    private static readonly Dictionary<UserRole, string[]> RoleMap = new()
    {
        [UserRole.Admin] = PermissionCodes.All.ToArray(),
        [UserRole.HospitalDirector] =
        [
            PermissionCodes.PatientsRead, PermissionCodes.PepRead,
            PermissionCodes.BillingRead, PermissionCodes.ReportsRead,
            PermissionCodes.AuditRead, PermissionCodes.SecurityManage,
            PermissionCodes.LgpdManage, PermissionCodes.LgpdConsentManage,
            PermissionCodes.LgpdSubjectRequests, PermissionCodes.IncidentsManage,
            PermissionCodes.TransportManage, PermissionCodes.CleaningManage,
            PermissionCodes.HospitalizationManage,
            PermissionCodes.ConnectRead, PermissionCodes.ConnectWrite, PermissionCodes.ConnectAdmin,
            PermissionCodes.ConnectApprove,
            PermissionCodes.IntegrationsManage,
            PermissionCodes.TpaManage, PermissionCodes.PayrollManage, PermissionCodes.PharmacyBillingManage,
        ],
        [UserRole.Doctor] =
        [
            PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.PepWrite,
            PermissionCodes.HospitalizationManage, PermissionCodes.ReportsRead,
            PermissionCodes.TransportOperate, PermissionCodes.CleaningOperate,
            PermissionCodes.ConnectRead, PermissionCodes.ConnectWrite,
        ],
        [UserRole.Nurse] =
        [
            PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.PepWrite,
            PermissionCodes.HospitalizationManage, PermissionCodes.TransportOperate,
            PermissionCodes.CleaningOperate,
            PermissionCodes.ConnectRead, PermissionCodes.ConnectWrite,
        ],
        [UserRole.NursingTechnician] =
        [
            PermissionCodes.PatientsRead, PermissionCodes.PepRead,
            PermissionCodes.TransportOperate,
            PermissionCodes.ConnectRead,
        ],
        [UserRole.Reception] =
        [
            PermissionCodes.PatientsCreate, PermissionCodes.PatientsRead, PermissionCodes.PatientsUpdate,
            PermissionCodes.PepRead,
            PermissionCodes.BillingRead, PermissionCodes.HospitalizationManage,
            PermissionCodes.TransportOperate, PermissionCodes.TransportManage,
            PermissionCodes.CleaningOperate, PermissionCodes.CleaningManage,
            PermissionCodes.ReportsRead,
            PermissionCodes.ConnectRead, PermissionCodes.ConnectWrite,
            PermissionCodes.IntegrationsManage,
            PermissionCodes.TpaManage,
        ],
        [UserRole.Billing] =
        [
            PermissionCodes.PatientsRead, PermissionCodes.BillingRead, PermissionCodes.BillingWrite,
            PermissionCodes.ReportsRead,
            PermissionCodes.ConnectRead, PermissionCodes.ConnectWrite, PermissionCodes.ConnectApprove,
            PermissionCodes.TpaManage, PermissionCodes.PayrollManage,
        ],
        [UserRole.Pharmacy] =
        [
            PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.PharmacyDispense,
            PermissionCodes.PharmacyBillingManage,
        ],
        [UserRole.Warehouse] =
        [
            PermissionCodes.WarehouseManage, PermissionCodes.ReportsRead,
            PermissionCodes.ConnectRead, PermissionCodes.ConnectWrite,
        ],
        [UserRole.Porter] =
        [
            PermissionCodes.TransportOperate,
        ],
        [UserRole.Hospitality] =
        [
            PermissionCodes.CleaningOperate, PermissionCodes.CleaningManage,
            PermissionCodes.PatientsRead,
        ],
        [UserRole.IT] =
        [
            PermissionCodes.UsersManage, PermissionCodes.SecurityManage,
            PermissionCodes.AuditRead, PermissionCodes.ReportsRead,
            PermissionCodes.ConnectRead, PermissionCodes.ConnectWrite, PermissionCodes.ConnectApprove,
            PermissionCodes.IntegrationsManage,
        ],
        [UserRole.Auditor] =
        [
            PermissionCodes.AuditRead, PermissionCodes.ReportsRead,
            PermissionCodes.LgpdManage, PermissionCodes.LgpdSubjectRequests,
            PermissionCodes.IncidentsManage, PermissionCodes.PatientsRead,
            PermissionCodes.PepRead, PermissionCodes.BillingRead,
        ],
        [UserRole.Insurance] =
        [
            PermissionCodes.PatientsRead, PermissionCodes.BillingRead, PermissionCodes.BillingWrite,
            PermissionCodes.ReportsRead,
        ],
        [UserRole.Patient] = [],
    };

    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        foreach (var (code, name, module) in Definitions)
        {
            if (!await db.PermissionDefinitions.AnyAsync(p => p.Code == code, cancellationToken))
            {
                db.PermissionDefinitions.Add(new PermissionDefinition
                {
                    Code = code,
                    Name = name,
                    Module = module,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var (role, permissions) in RoleMap)
        {
            foreach (var permission in permissions)
            {
                if (!await db.RolePermissions.AnyAsync(
                        r => r.Role == role && r.PermissionCode == permission, cancellationToken))
                {
                    db.RolePermissions.Add(new RolePermission
                    {
                        Role = role,
                        PermissionCode = permission,
                    });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

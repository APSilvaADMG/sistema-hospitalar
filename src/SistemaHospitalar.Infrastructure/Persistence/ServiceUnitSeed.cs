using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class ServiceUnitSeed
{
    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.ServiceUnits.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.ServiceUnits.Add(new ServiceUnit
        {
            Name = "Unidade Principal",
            Code = "UNID-01",
            Cnes = "0000000",
            Address = "Sede hospitalar",
            IsDefault = true,
        });

        dbContext.ServiceUnits.Add(new ServiceUnit
        {
            Name = "Ambulatório",
            Code = "AMB-01",
            IsDefault = false,
        });

        dbContext.ServiceUnits.Add(new ServiceUnit
        {
            Name = "Internação",
            Code = "INT-01",
            IsDefault = false,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

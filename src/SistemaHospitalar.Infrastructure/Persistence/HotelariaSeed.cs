using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class HotelariaSeed
{
    public const string DemoMarker = "gth-hotelaria-demo-v1";

    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.CleaningRequests.AnyAsync(
                c => c.IsActive && c.Notes != null && c.Notes.Contains(DemoMarker),
                cancellationToken))
        {
            return;
        }

        var beds = await dbContext.Beds
            .Include(b => b.Ward)
            .Where(b => b.IsActive)
            .OrderBy(b => b.Ward.Name)
            .ThenBy(b => b.BedNumber)
            .Take(6)
            .ToListAsync(cancellationToken);

        if (beds.Count == 0)
        {
            return;
        }

        var checklist =
            """[{"id":"1","label":"Remoção de enxoval","done":false},{"id":"2","label":"Desinfecção de superfícies","done":false},{"id":"3","label":"Troca de lençóis","done":false},{"id":"4","label":"Verificação de equipamentos","done":false},{"id":"5","label":"Liberação do leito","done":false}]""";

        var scenarios = new[]
        {
            (CleaningType.Terminal, CleaningRequestStatus.Requested, "Equipe Hotelaria A", "Alta hospitalar — higienização terminal"),
            (CleaningType.Concurrent, CleaningRequestStatus.InProgress, "Equipe Hotelaria B", "Higienização concorrente — paciente em isolamento"),
            (CleaningType.Terminal, CleaningRequestStatus.Requested, "Equipe Hotelaria C", "Transferência UTI — preparo de leito"),
            (CleaningType.Concurrent, CleaningRequestStatus.Completed, "Equipe Hotelaria A", "Rotina diária concluída"),
            (CleaningType.Terminal, CleaningRequestStatus.Requested, "Equipe Hotelaria B", "Centro cirúrgico — leito pós-procedimento"),
        };

        for (var i = 0; i < scenarios.Length && i < beds.Count; i++)
        {
            var bed = beds[i];
            var (cleaningType, status, team, note) = scenarios[i];

            if (status is CleaningRequestStatus.Requested or CleaningRequestStatus.InProgress)
            {
                bed.Status = BedStatus.Cleaning;
                bed.StatusReason = cleaningType == CleaningType.Terminal
                    ? "Higienização terminal"
                    : "Higienização concorrente";
            }

            dbContext.CleaningRequests.Add(new CleaningRequest
            {
                BedId = bed.Id,
                CleaningType = cleaningType,
                TriggerReason = CleaningTriggerReason.Manual,
                Status = status,
                AssignedTeam = team,
                ChecklistJson = checklist,
                Notes = $"{note}. {DemoMarker}",
                StartedAt = status is CleaningRequestStatus.InProgress or CleaningRequestStatus.Completed
                    ? DateTime.UtcNow.AddHours(-2)
                    : null,
                CompletedAt = status == CleaningRequestStatus.Completed
                    ? DateTime.UtcNow.AddHours(-1)
                    : null,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

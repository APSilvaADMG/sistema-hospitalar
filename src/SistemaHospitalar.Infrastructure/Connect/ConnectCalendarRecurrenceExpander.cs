using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Connect;

internal static class ConnectCalendarRecurrenceExpander
{
    private const int MaxInstancesPerEvent = 366;

    public static IEnumerable<(DateTime Inicio, DateTime Fim, bool IsRecurrenceInstance)> Expand(
        ConnectCalendarEvent entity,
        DateTime rangeFrom,
        DateTime rangeTo)
    {
        if (entity.RecurrenceRule == ConnectCalendarRecurrenceRule.None)
        {
            if (entity.Inicio < rangeTo && entity.Fim > rangeFrom)
            {
                yield return (entity.Inicio, entity.Fim, false);
            }

            yield break;
        }

        var duration = entity.Fim - entity.Inicio;
        var step = entity.RecurrenceRule == ConnectCalendarRecurrenceRule.Daily
            ? TimeSpan.FromDays(1)
            : TimeSpan.FromDays(7);

        var occurrenceStart = entity.Inicio;
        var count = 0;
        var isFirst = true;

        while (occurrenceStart < rangeTo && count < MaxInstancesPerEvent)
        {
            var occurrenceEnd = occurrenceStart + duration;

            if (occurrenceEnd > rangeFrom)
            {
                yield return (occurrenceStart, occurrenceEnd, !isFirst);
            }

            occurrenceStart += step;
            count++;
            isFirst = false;

            if (occurrenceStart > rangeTo.Add(step))
            {
                break;
            }
        }
    }
}

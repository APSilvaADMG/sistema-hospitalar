using SistemaHospitalar.Domain.Enums;



namespace SistemaHospitalar.Infrastructure.Connect;



public static class ConnectTicketSlaCalculator

{

  /// <summary>

  /// SLA básico RN011 para categoria TI; demais categorias usam prazos equivalentes à prioridade Normal.

  /// </summary>

  public static DateTime CalculateDueAt(ConnectTicketCategory category, MessagePriority priority, DateTime createdAt)

  {

    var hours = category == ConnectTicketCategory.TI

      ? priority switch

      {

        MessagePriority.Critica => 4,

        MessagePriority.Alta or MessagePriority.Urgente => 24,

        MessagePriority.Normal => 48,

        _ => 72,

      }

      : priority switch

      {

        MessagePriority.Critica => 8,

        MessagePriority.Alta or MessagePriority.Urgente => 48,

        MessagePriority.Normal => 72,

        _ => 120,

      };



    return createdAt.AddHours(hours);

  }

}


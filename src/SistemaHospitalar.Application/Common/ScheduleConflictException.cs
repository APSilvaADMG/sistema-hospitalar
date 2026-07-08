namespace SistemaHospitalar.Application.Common;

/// <summary>
/// Lançada quando o profissional já possui agendamento ativo no horário solicitado.
/// </summary>
public sealed class ScheduleConflictException : InvalidOperationException
{
    public const string DefaultMessage = "Este profissional já possui agendamento neste horário.";

    public ScheduleConflictException()
        : base(DefaultMessage)
    {
    }

    public ScheduleConflictException(string message)
        : base(message)
    {
    }
}

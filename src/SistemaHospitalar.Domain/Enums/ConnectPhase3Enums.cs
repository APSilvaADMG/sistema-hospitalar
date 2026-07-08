namespace SistemaHospitalar.Domain.Enums;

public enum ConnectCalendarEventType
{
    Reuniao = 1,
    Evento = 2,
    Escala = 3,
    Treinamento = 4,
}

public enum ConnectCalendarParticipantResponse
{
    Pendente = 1,
    Aceito = 2,
    Recusado = 3,
    Talvez = 4,
}

public enum ConnectCalendarRecurrenceRule
{
    None = 0,
    Daily = 1,
    Weekly = 2,
}

public enum ConnectContextType
{
    Patient = 1,
    Guide = 2,
    Appointment = 3,
    Hospitalization = 4,
    Ticket = 5,
}

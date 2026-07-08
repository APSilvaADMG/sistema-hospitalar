namespace SistemaHospitalar.Domain.Enums;

public enum PendingItemStatus
{
    Aberta = 0,
    EmAndamento = 1,
    Concluida = 2,
    Cancelada = 3
}

public enum PendingItemPriority
{
    Baixa = 0,
    Normal = 1,
    Alta = 2,
    Critica = 3
}

public enum PendingModule
{
    Connect = 0,
    Mail = 1,
    Chat = 2,
    Guides = 3,
    Inventory = 4,
    Financial = 5,
    System = 6,
    Tickets = 7,
    Tasks = 8,
    Hotelaria = 9,
    Nursing = 10,
    Reception = 11
}

public enum PendingItemType
{
    TicketOverdue = 0,
    UnreadMail = 1,
    UnreadChat = 2,
    GuideDraft = 3,
    LowStock = 4,
    TaskOverdue = 5,
    SystemAlert = 6,
    WorkflowPending = 7,
    BedCleaning = 8,
    UnsignedPrescription = 9,
    CheckInPending = 10,
    EligibilityRequired = 11
}

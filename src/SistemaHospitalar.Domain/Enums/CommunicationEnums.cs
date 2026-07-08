namespace SistemaHospitalar.Domain.Enums;

public enum MessagePriority
{
    Baixa = 1,
    Normal = 2,
    Alta = 3,
    Urgente = 4,
    Critica = 5,
}

public enum InternalMessageStatus
{
    Draft = 1,
    Sent = 2,
}

public enum MessageRecipientType
{
    To = 1,
    Cc = 2,
    Bcc = 3,
}

public enum MailFolder
{
    Inbox = 1,
    Sent = 2,
    Drafts = 3,
    Trash = 4,
    Archive = 5,
}

public enum ChatRoomType
{
    Private = 1,
    Sector = 2,
    Group = 3,
}

public enum ConnectNotificationCategory
{
    Info = 1,
    Alert = 2,
    System = 3,
}

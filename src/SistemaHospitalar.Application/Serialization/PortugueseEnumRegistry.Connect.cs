using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Serialization;

public static partial class PortugueseEnumRegistry
{
    private static void RegisterConnectMaps(Dictionary<Type, EnumMap> maps)
    {
        Register(maps,
            (ConnectChannel.WhatsApp, "WhatsApp"),
            (ConnectChannel.Sms, "SMS"),
            (ConnectChannel.Email, "E-mail"),
            (ConnectChannel.Telegram, "Telegram"),
            (ConnectChannel.Push, "Push"));

        Register(maps,
            (ConnectMessageDirection.Inbound, "Entrada"),
            (ConnectMessageDirection.Outbound, "Saída"));

        Register(maps,
            (ConnectMessageStatus.Pending, "Pendente"),
            (ConnectMessageStatus.Sent, "Enviada"),
            (ConnectMessageStatus.Delivered, "Entregue"),
            (ConnectMessageStatus.Read, "Lida"),
            (ConnectMessageStatus.Failed, "Falhou"));

        Register(maps,
            (ConnectBotStep.MainMenu, "Menu principal"),
            (ConnectBotStep.AwaitingCpf, "Aguardando CPF"),
            (ConnectBotStep.ScheduleSpecialty, "Agendar — especialidade"),
            (ConnectBotStep.ScheduleProfessional, "Agendar — profissional"),
            (ConnectBotStep.ScheduleDate, "Agendar — data"),
            (ConnectBotStep.ScheduleSlot, "Agendar — horário"),
            (ConnectBotStep.ScheduleConfirm, "Agendar — confirmação"),
            (ConnectBotStep.RescheduleSelect, "Reagendar — seleção"),
            (ConnectBotStep.CancelSelect, "Cancelar — seleção"),
            (ConnectBotStep.ConfirmReminder, "Confirmar lembrete"),
            (ConnectBotStep.WaitlistOffer, "Oferta de fila"),
            (ConnectBotStep.CheckIn, "Check-in"),
            (ConnectBotStep.PreTriage, "Pré-triagem"),
            (ConnectBotStep.Satisfaction, "Satisfação"),
            (ConnectBotStep.AwaitingHuman, "Aguardando atendente"),
            (ConnectBotStep.BillingSelectAccount, "Faturamento — conta"),
            (ConnectBotStep.BillingPaymentInfo, "Faturamento — pagamento"));

        Register(maps,
            (ConnectReminderType.AppointmentConfirmation, "Confirmação de consulta"),
            (ConnectReminderType.Confirmation72h, "Confirmação 72h"),
            (ConnectReminderType.Reminder24h, "Lembrete 24h"),
            (ConnectReminderType.RescheduleOffer, "Oferta de reagendamento"),
            (ConnectReminderType.WaitlistOffer, "Oferta de fila de espera"),
            (ConnectReminderType.CheckInInvite, "Convite check-in"),
            (ConnectReminderType.ExamResultAvailable, "Resultado disponível"),
            (ConnectReminderType.SatisfactionSurvey, "Pesquisa de satisfação"),
            (ConnectReminderType.ReturnRecovery, "Retorno pós-alta"),
            (ConnectReminderType.BillingReminder, "Lembrete de faturamento"),
            (ConnectReminderType.BillingOverdue, "Faturamento em atraso"));

        Register(maps,
            (ConnectWaitlistStatus.Waiting, "Aguardando"),
            (ConnectWaitlistStatus.Offered, "Ofertada"),
            (ConnectWaitlistStatus.Accepted, "Aceita"),
            (ConnectWaitlistStatus.Declined, "Recusada"),
            (ConnectWaitlistStatus.Expired, "Expirada"));

        Register(maps,
            (ConnectInboxQueue.None, "Nenhuma"),
            (ConnectInboxQueue.Reception, "Recepção"),
            (ConnectInboxQueue.Billing, "Faturamento"));

        Register(maps,
            (ConnectTicketCategory.TI, "TI"),
            (ConnectTicketCategory.Infraestrutura, "Infraestrutura"),
            (ConnectTicketCategory.Compras, "Compras"),
            (ConnectTicketCategory.RH, "RH"),
            (ConnectTicketCategory.Financeiro, "Financeiro"),
            (ConnectTicketCategory.EngenhariaClinica, "Eng. clínica"),
            (ConnectTicketCategory.Manutencao, "Manutenção"));

        Register(maps,
            (ConnectTicketStatus.Aberto, "Aberto"),
            (ConnectTicketStatus.EmAndamento, "Em andamento"),
            (ConnectTicketStatus.Aguardando, "Aguardando"),
            (ConnectTicketStatus.Resolvido, "Resolvido"),
            (ConnectTicketStatus.Cancelado, "Cancelado"));

        Register(maps,
            (ConnectTaskStatus.Aberta, "Aberta"),
            (ConnectTaskStatus.EmAndamento, "Em andamento"),
            (ConnectTaskStatus.Aguardando, "Aguardando"),
            (ConnectTaskStatus.Concluida, "Concluída"),
            (ConnectTaskStatus.Cancelada, "Cancelada"));

        Register(maps,
            (WorkflowType.SolicitacaoCompra, "Solicitação de compra"),
            (WorkflowType.AprovacaoGenerica, "Aprovação genérica"));

        Register(maps,
            (WorkflowInstanceStatus.Pendente, "Pendente"),
            (WorkflowInstanceStatus.Aprovado, "Aprovado"),
            (WorkflowInstanceStatus.Rejeitado, "Rejeitado"),
            (WorkflowInstanceStatus.Cancelado, "Cancelado"));

        Register(maps,
            (WorkflowStepStatus.Pendente, "Pendente"),
            (WorkflowStepStatus.Aprovado, "Aprovado"),
            (WorkflowStepStatus.Rejeitado, "Rejeitado"));

        Register(maps,
            (ConnectCalendarEventType.Reuniao, "Reunião"),
            (ConnectCalendarEventType.Evento, "Evento"),
            (ConnectCalendarEventType.Escala, "Escala"),
            (ConnectCalendarEventType.Treinamento, "Treinamento"));

        Register(maps,
            (ConnectCalendarParticipantResponse.Pendente, "Pendente"),
            (ConnectCalendarParticipantResponse.Aceito, "Aceito"),
            (ConnectCalendarParticipantResponse.Recusado, "Recusado"),
            (ConnectCalendarParticipantResponse.Talvez, "Talvez"));

        Register(maps,
            (ConnectCalendarRecurrenceRule.None, "Nenhuma"),
            (ConnectCalendarRecurrenceRule.Daily, "Diária"),
            (ConnectCalendarRecurrenceRule.Weekly, "Semanal"));

        Register(maps,
            (ConnectContextType.Patient, "Paciente"),
            (ConnectContextType.Guide, "Guia"),
            (ConnectContextType.Appointment, "Agendamento"),
            (ConnectContextType.Hospitalization, "Internação"),
            (ConnectContextType.Ticket, "Chamado"));

        Register(maps,
            (MessagePriority.Baixa, "Baixa"),
            (MessagePriority.Normal, "Normal"),
            (MessagePriority.Alta, "Alta"),
            (MessagePriority.Urgente, "Urgente"),
            (MessagePriority.Critica, "Crítica"));

        Register(maps,
            (InternalMessageStatus.Draft, "Rascunho"),
            (InternalMessageStatus.Sent, "Enviada"));

        Register(maps,
            (MessageRecipientType.To, "Para"),
            (MessageRecipientType.Cc, "Cópia"),
            (MessageRecipientType.Bcc, "Cópia oculta"));

        Register(maps,
            (MailFolder.Inbox, "Caixa de entrada"),
            (MailFolder.Sent, "Enviados"),
            (MailFolder.Drafts, "Rascunhos"),
            (MailFolder.Trash, "Lixeira"),
            (MailFolder.Archive, "Arquivo"));

        Register(maps,
            (ChatRoomType.Private, "Privado"),
            (ChatRoomType.Sector, "Setor"),
            (ChatRoomType.Group, "Grupo"));

        Register(maps,
            (ConnectNotificationCategory.Info, "Informação"),
            (ConnectNotificationCategory.Alert, "Alerta"),
            (ConnectNotificationCategory.System, "Sistema"));
    }
}

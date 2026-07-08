using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class ConnectKnowledgeSeed
{
    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.ConnectKnowledgeArticles.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.ConnectKnowledgeArticles.AddRange(
            new ConnectKnowledgeArticle
            {
                Category = "Convênios",
                Question = "Aceita Unimed?",
                Answer = "Sim, aceitamos Unimed e diversos convênios. Informe seu plano no agendamento ou na recepção.",
                Keywords = "unimed convênio plano aceita",
            },
            new ConnectKnowledgeArticle
            {
                Category = "Horários",
                Question = "Qual horário da pediatria?",
                Answer = "Pediatria: seg-sex 8h às 17h. Agendamento pelo WhatsApp opção 1 ou recepção.",
                Keywords = "pediatria horário pediatra",
            },
            new ConnectKnowledgeArticle
            {
                Category = "Unidade",
                Question = "Onde fica a unidade?",
                Answer = "Unidade Principal — consulte a recepção para endereço completo e estacionamento.",
                Keywords = "endereço unidade localização onde fica",
            },
            new ConnectKnowledgeArticle
            {
                Category = "Exames",
                Question = "Como faço jejum para exame?",
                Answer = "Jejum de 8 a 12 horas para a maioria dos exames laboratoriais. Siga a orientação específica do seu pedido médico.",
                Keywords = "jejum exame laboratório água",
            },
            new ConnectKnowledgeArticle
            {
                Category = "Exames",
                Question = "Posso tomar água antes do exame?",
                Answer = "Para exames de sangue com jejum, água pura geralmente é permitida. Confirme no seu pedido ou com a laboratório.",
                Keywords = "água jejum exame beber",
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

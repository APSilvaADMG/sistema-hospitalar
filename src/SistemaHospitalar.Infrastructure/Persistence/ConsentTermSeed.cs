using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class ConsentTermSeed
{
    private static readonly (string Version, string Title, string Content, string[] Purposes)[] DefaultTerms =
    [
        (
            "lgpd-1.0",
            "Termo de Consentimento LGPD — Tratamento de Dados de Saúde",
            """
            Autorizo o hospital a tratar meus dados pessoais e dados sensíveis de saúde para as finalidades
            informadas neste termo, em conformidade com a Lei nº 13.709/2018 (LGPD).
            Estou ciente de que posso revogar este consentimento a qualquer momento, observadas as bases legais
            aplicáveis ao tratamento de dados em ambiente hospitalar.
            """,
            [
                "Atendimento clínico e hospitalar",
                "Faturamento e convênios",
                "Comunicação sobre cuidados de saúde",
            ]
        ),
        (
            "cirurgia-1.0",
            "Termo de Consentimento — Procedimento Cirúrgico",
            """
            Declaro ter recebido informações claras sobre o procedimento cirúrgico proposto, incluindo indicação,
            benefícios esperados, riscos, alternativas e cuidados pós-operatórios.
            Autorizo a equipe assistencial a realizar o procedimento e as condutas necessárias em caso de intercorrência,
            conforme boas práticas médicas e protocolos institucionais.
            """,
            [
                "Realização de procedimento cirúrgico",
                "Registro clínico e auditoria assistencial",
            ]
        ),
        (
            "telemedicina-1.0",
            "Termo de Consentimento — Telemedicina",
            """
            Autorizo o atendimento à distância por meio de telemedicina, ciente de que a qualidade do serviço depende
            da conexão, do ambiente e das informações fornecidas por mim.
            Estou ciente de que situações de urgência ou emergência exigem atendimento presencial imediato.
            """,
            [
                "Consulta e acompanhamento à distância",
                "Registro eletrônico do atendimento",
            ]
        ),
        (
            "pesquisa-1.0",
            "Termo de Consentimento — Pesquisa Clínica",
            """
            Declaro ter recebido o Termo de Consentimento Livre e Esclarecido da pesquisa, com explicação sobre objetivos,
            procedimentos, riscos, benefícios, confidencialidade e direito de desistir a qualquer momento sem prejuízo
            ao meu tratamento assistencial.
            """,
            [
                "Participação em estudo clínico",
                "Coleta e tratamento de dados de pesquisa",
            ]
        ),
    ];

    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        foreach (var (version, title, content, purposes) in DefaultTerms)
        {
            var exists = await db.ConsentTerms.AnyAsync(
                t => t.Version == version || t.Title == title,
                cancellationToken);
            if (exists)
            {
                continue;
            }

            db.ConsentTerms.Add(new ConsentTerm
            {
                Version = version,
                Title = title,
                Content = content,
                PurposesJson = JsonSerializer.Serialize(purposes),
                EffectiveFrom = DateTime.UtcNow,
                IsCurrent = true,
            });

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

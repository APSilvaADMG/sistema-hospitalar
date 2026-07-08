using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public class ConnectAiAssistantService(
    AppDbContext db,
    IConnectMailService mailService,
    IConnectTicketService ticketService,
    IConnectTaskService taskService,
    IGroqLlmService groqLlmService) : IConnectAiAssistantService
{
    private const string SystemPrompt = """
        Você é o assistente APSMed Connect de um hospital. Responda SEMPRE em português brasileiro.
        Use APENAS os dados agregados fornecidos — nunca invente números.
        NÃO exporte, cite ou solicite dados pessoais de pacientes (nomes, CPF, CNS, prontuário).
        Se a pergunta pedir PHI ou dados clínicos individuais, recuse educadamente.
        Seja conciso (máximo 4 frases). Se não souber, oriente o usuário a usar os painéis do Connect.
        """;

    private static readonly ConnectAiQuickQueryDto[] QuickQueries =
    [
        new("unread-mail", "Mensagens não lidas", "Quantas mensagens não lidas?"),
        new("open-tickets", "Chamados abertos", "Chamados abertos"),
        new("pending-tasks", "Tarefas pendentes", "Tarefas pendentes"),
        new("pending-guides", "Guias pendentes", "Guias pendentes"),
    ];

    public Task<IReadOnlyList<ConnectAiQuickQueryDto>> GetQuickQueriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ConnectAiQuickQueryDto>>(QuickQueries);

    public async Task<ConnectAiAskResponse> AskAsync(
        Guid userId,
        ConnectAiAskRequest request,
        CancellationToken cancellationToken = default)
    {
        var question = request.Question?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(question))
        {
            return new ConnectAiAskResponse(question, "Por favor, digite uma pergunta.", "invalid", null);
        }

        var metrics = await GatherMetricsAsync(userId, cancellationToken);
        var keywordResult = await ResolveKeywordAsync(userId, question, metrics, cancellationToken);

        if (groqLlmService.IsConfigured)
        {
            var contextBlock = FormatMetrics(metrics);
            var userPrompt = $"""
                Dados agregados do usuário:
                {contextBlock}

                Pergunta: {question}
                """;

            var llmAnswer = await groqLlmService.CompleteAsync(SystemPrompt, userPrompt, cancellationToken);
            if (!string.IsNullOrWhiteSpace(llmAnswer))
            {
                return new ConnectAiAskResponse(
                    question,
                    llmAnswer,
                    keywordResult.Intent,
                    keywordResult.Data,
                    UsedLlm: true);
            }
        }

        return keywordResult;
    }

    public async IAsyncEnumerable<ConnectAiStreamChunk> AskStreamAsync(
        Guid userId,
        ConnectAiAskRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var question = request.Question?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(question))
        {
            yield return new ConnectAiStreamChunk("done", Text: "Por favor, digite uma pergunta.", Intent: "invalid");
            yield break;
        }

        var metrics = await GatherMetricsAsync(userId, cancellationToken);
        var keywordResult = await ResolveKeywordAsync(userId, question, metrics, cancellationToken);

        if (groqLlmService.IsConfigured)
        {
            var contextBlock = FormatMetrics(metrics);
            var userPrompt = $"""
                Dados agregados do usuário:
                {contextBlock}

                Pergunta: {question}
                """;

            var hasTokens = false;
            await foreach (var token in groqLlmService.StreamCompleteAsync(SystemPrompt, userPrompt, cancellationToken))
            {
                hasTokens = true;
                yield return new ConnectAiStreamChunk("token", Text: token);
            }

            if (hasTokens)
            {
                yield return new ConnectAiStreamChunk(
                    "done",
                    Intent: keywordResult.Intent,
                    UsedLlm: true,
                    Data: keywordResult.Data);
                yield break;
            }
        }

        yield return new ConnectAiStreamChunk("token", Text: keywordResult.Answer);
        yield return new ConnectAiStreamChunk(
            "done",
            Intent: keywordResult.Intent,
            UsedLlm: keywordResult.UsedLlm,
            Data: keywordResult.Data);
    }

    private async Task<ConnectAiAskResponse> ResolveKeywordAsync(
        Guid userId,
        string question,
        AssistantMetrics metrics,
        CancellationToken cancellationToken)
    {
        var normalized = question.ToLowerInvariant();

        if (Matches(normalized, "mensagem", "não lida", "nao lida", "e-mail", "email", "caixa"))
        {
            return new ConnectAiAskResponse(
                question,
                $"Você tem {metrics.UnreadMail} mensagem(ns) interna(s) não lida(s) no APSMed Connect.",
                "unread-mail",
                new Dictionary<string, object> { ["count"] = metrics.UnreadMail });
        }

        if (Matches(normalized, "chamado", "ticket", "protocolo"))
        {
            var open = metrics.TicketsAbertos + metrics.TicketsEmAndamento + metrics.TicketsAguardando;
            return new ConnectAiAskResponse(
                question,
                $"Há {metrics.TicketsAbertos} chamado(s) aberto(s), {metrics.TicketsEmAndamento} em andamento, {metrics.TicketsAguardando} aguardando e {metrics.TicketsVencidos} vencido(s) (SLA). Total ativo: {open}.",
                "open-tickets",
                new Dictionary<string, object>
                {
                    ["abertos"] = metrics.TicketsAbertos,
                    ["emAndamento"] = metrics.TicketsEmAndamento,
                    ["aguardando"] = metrics.TicketsAguardando,
                    ["vencidos"] = metrics.TicketsVencidos,
                });
        }

        if (Matches(normalized, "tarefa", "task", "pendente"))
        {
            var pending = metrics.TasksMinhasAbertas + metrics.TasksDelegadasAbertas;
            return new ConnectAiAskResponse(
                question,
                $"Você tem {pending} tarefa(s) pendente(s): {metrics.TasksMinhasAbertas} atribuída(s) a você e {metrics.TasksDelegadasAbertas} delegada(s). {metrics.TasksVencidas} vencida(s).",
                "pending-tasks",
                new Dictionary<string, object>
                {
                    ["minhasAbertas"] = metrics.TasksMinhasAbertas,
                    ["delegadasAbertas"] = metrics.TasksDelegadasAbertas,
                    ["vencidas"] = metrics.TasksVencidas,
                });
        }

        if (Matches(normalized, "guia", "tiss", "sus", "faturamento"))
        {
            return new ConnectAiAskResponse(
                question,
                $"Existem {metrics.GuidesTotal} guia(s) pendente(s): {metrics.GuidesTiss} TISS e {metrics.GuidesSus} SUS (rascunho ou enviadas, não finalizadas).",
                "pending-guides",
                new Dictionary<string, object>
                {
                    ["tiss"] = metrics.GuidesTiss,
                    ["sus"] = metrics.GuidesSus,
                    ["total"] = metrics.GuidesTotal,
                });
        }

        return new ConnectAiAskResponse(
            question,
            "Posso responder consultas sobre mensagens não lidas, chamados abertos, tarefas pendentes e guias pendentes. Use os atalhos ou reformule sua pergunta.",
            "unknown",
            null);
    }

    private async Task<AssistantMetrics> GatherMetricsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var mailCount = await mailService.GetUnreadCountAsync(userId, cancellationToken);
        var ticketSummary = await ticketService.GetSummaryAsync(userId, cancellationToken);
        var taskSummary = await taskService.GetSummaryAsync(userId, cancellationToken);
        var tissPending = await db.TissGuides.AsNoTracking()
            .CountAsync(g => g.IsActive && (g.Status == TissGuideStatus.Draft || g.Status == TissGuideStatus.Sent), cancellationToken);
        var susPending = await db.SusGuides.AsNoTracking()
            .CountAsync(g => g.IsActive && (g.Status == SusGuideStatus.Draft || g.Status == SusGuideStatus.Submitted), cancellationToken);

        return new AssistantMetrics(
            mailCount,
            ticketSummary.TotalAbertos,
            ticketSummary.TotalEmAndamento,
            ticketSummary.TotalAguardando,
            ticketSummary.TotalVencidos,
            taskSummary.MinhasAbertas,
            taskSummary.DelegadasAbertas,
            taskSummary.Vencidas,
            tissPending,
            susPending);
    }

    private static string FormatMetrics(AssistantMetrics m) =>
        $"""
        - Mensagens não lidas: {m.UnreadMail}
        - Chamados abertos: {m.TicketsAbertos}; em andamento: {m.TicketsEmAndamento}; aguardando: {m.TicketsAguardando}; SLA vencidos: {m.TicketsVencidos}
        - Tarefas minhas abertas: {m.TasksMinhasAbertas}; delegadas abertas: {m.TasksDelegadasAbertas}; vencidas: {m.TasksVencidas}
        - Guias pendentes TISS: {m.GuidesTiss}; SUS: {m.GuidesSus}
        """;

    private static bool Matches(string normalized, params string[] keywords) =>
        keywords.Any(k => normalized.Contains(k, StringComparison.Ordinal));

    private sealed record AssistantMetrics(
        int UnreadMail,
        int TicketsAbertos,
        int TicketsEmAndamento,
        int TicketsAguardando,
        int TicketsVencidos,
        int TasksMinhasAbertas,
        int TasksDelegadasAbertas,
        int TasksVencidas,
        int GuidesTiss,
        int GuidesSus)
    {
        public int GuidesTotal => GuidesTiss + GuidesSus;
    }
}

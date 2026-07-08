using Microsoft.EntityFrameworkCore;

using SistemaHospitalar.Application.DTOs.ClinicalOperations;

using SistemaHospitalar.Application.Interfaces;

using SistemaHospitalar.Domain.Entities;

using SistemaHospitalar.Domain.Enums;

using SistemaHospitalar.Infrastructure.Persistence;



namespace SistemaHospitalar.Infrastructure.Services;



public class FinancialCashSessionService(AppDbContext dbContext) : IFinancialCashSessionService

{

    private static readonly PaymentMethod[] CounterMethods =

    [

        PaymentMethod.Cash,

        PaymentMethod.Pix,

        PaymentMethod.DebitCard,

        PaymentMethod.CreditCard,

    ];



    private sealed record CashSessionMovements(

        decimal CashReceived,

        decimal CashPaidOut,

        decimal CounterReceived,

        decimal CounterPaidOut,

        decimal DayOperationalReceived,

        decimal DayOperationalPaidOut);



    public async Task<FinancialCashSessionDto?> GetOpenSessionAsync(CancellationToken cancellationToken = default)

    {

        var session = await dbContext.FinancialCashSessions

            .AsNoTracking()

            .Where(s => s.IsActive && s.Status == FinancialCashSessionStatus.Open)

            .OrderByDescending(s => s.OpenedAt)

            .FirstOrDefaultAsync(cancellationToken);



        if (session is null)

        {

            return null;

        }



        var movements = await ComputeMovementsAsync(session.OpenedAt, null, cancellationToken);

        return ToDto(session, movements);

    }



    public async Task<IReadOnlyList<FinancialCashSessionDto>> ListSessionsAsync(

        int limit = 30,

        CancellationToken cancellationToken = default)

    {

        limit = Math.Clamp(limit, 1, 100);

        var sessions = await dbContext.FinancialCashSessions

            .AsNoTracking()

            .Where(s => s.IsActive)

            .OrderByDescending(s => s.OpenedAt)

            .Take(limit)

            .ToListAsync(cancellationToken);



        var result = new List<FinancialCashSessionDto>(sessions.Count);

        foreach (var session in sessions)

        {

            var movements = await ComputeMovementsAsync(session.OpenedAt, session.ClosedAt, cancellationToken);

            result.Add(ToDto(session, movements));

        }



        return result;

    }



    public async Task<FinancialCashSessionDto> OpenSessionAsync(

        OpenFinancialCashSessionRequest request,

        Guid? userId = null,

        CancellationToken cancellationToken = default)

    {

        var hasOpen = await dbContext.FinancialCashSessions.AnyAsync(

            s => s.IsActive && s.Status == FinancialCashSessionStatus.Open,

            cancellationToken);

        if (hasOpen)

        {

            throw new InvalidOperationException("Já existe um caixa aberto. Feche-o antes de abrir outro.");

        }



        var session = new FinancialCashSession

        {

            Label = string.IsNullOrWhiteSpace(request.Label) ? "Caixa principal" : request.Label.Trim(),

            OpenedAt = DateTime.UtcNow,

            OpeningBalance = request.OpeningBalance,

            ExpectedBalance = request.OpeningBalance,

            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),

            OpenedByUserId = userId,

            Status = FinancialCashSessionStatus.Open,

        };



        dbContext.FinancialCashSessions.Add(session);

        await dbContext.SaveChangesAsync(cancellationToken);



        return (await GetOpenSessionAsync(cancellationToken))!;

    }



    public async Task<FinancialCashSessionDto> CloseSessionAsync(

        Guid sessionId,

        CloseFinancialCashSessionRequest request,

        Guid? userId = null,

        CancellationToken cancellationToken = default)

    {

        var session = await dbContext.FinancialCashSessions.FirstOrDefaultAsync(

            s => s.Id == sessionId && s.IsActive,

            cancellationToken) ?? throw new InvalidOperationException("Sessão de caixa não encontrada.");



        if (session.Status == FinancialCashSessionStatus.Closed)

        {

            throw new InvalidOperationException("Caixa já encerrado.");

        }



        var movements = await ComputeMovementsAsync(session.OpenedAt, null, cancellationToken);

        session.ExpectedBalance = session.OpeningBalance + movements.CashReceived - movements.CashPaidOut;

        session.ClosingBalance = request.ClosingBalance;

        session.ClosedAt = DateTime.UtcNow;

        session.ClosedByUserId = userId;

        session.Status = FinancialCashSessionStatus.Closed;

        session.Notes = string.IsNullOrWhiteSpace(request.Notes)

            ? session.Notes

            : $"{session.Notes}\n{request.Notes}".Trim();



        await dbContext.SaveChangesAsync(cancellationToken);



        var closedMovements = await ComputeMovementsAsync(session.OpenedAt, session.ClosedAt, cancellationToken);

        return ToDto(session, closedMovements);

    }



    private async Task<CashSessionMovements> ComputeMovementsAsync(

        DateTime openedAt,

        DateTime? closedAt,

        CancellationToken cancellationToken)

    {

        var windowEnd = closedAt ?? DateTime.UtcNow;

        var referenceDate = closedAt?.Date ?? openedAt.Date;

        var dayStart = DateTime.SpecifyKind(referenceDate, DateTimeKind.Utc);

        var dayEnd = dayStart.AddDays(1);



        var payments = dbContext.FinancialPayments

            .AsNoTracking()

            .Where(p => p.IsActive && p.FinancialAccount.IsActive);



        var sessionPayments = payments.Where(p => p.PaidAt >= openedAt && p.PaidAt <= windowEnd);

        var dayPayments = payments.Where(p => p.PaidAt >= dayStart && p.PaidAt < dayEnd);



        var cashReceived = await sessionPayments

            .Where(p => p.FinancialAccount.Direction == FinancialAccountDirection.Receivable

                && p.Method == PaymentMethod.Cash)

            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;



        var cashPaidOut = await sessionPayments

            .Where(p => p.FinancialAccount.Direction == FinancialAccountDirection.Payable

                && p.Method == PaymentMethod.Cash)

            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;



        var counterReceived = await sessionPayments

            .Where(p => p.FinancialAccount.Direction == FinancialAccountDirection.Receivable

                && CounterMethods.Contains(p.Method))

            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;



        var counterPaidOut = await sessionPayments

            .Where(p => p.FinancialAccount.Direction == FinancialAccountDirection.Payable

                && CounterMethods.Contains(p.Method))

            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;



        var dayOperationalReceived = await dayPayments

            .Where(p => p.FinancialAccount.Direction == FinancialAccountDirection.Receivable)

            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;



        var dayOperationalPaidOut = await dayPayments

            .Where(p => p.FinancialAccount.Direction == FinancialAccountDirection.Payable)

            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;



        return new CashSessionMovements(

            cashReceived,

            cashPaidOut,

            counterReceived,

            counterPaidOut,

            dayOperationalReceived,

            dayOperationalPaidOut);

    }



    private static FinancialCashSessionDto ToDto(FinancialCashSession session, CashSessionMovements movements) =>

        new(

            session.Id,

            session.Label,

            session.OpenedAt,

            session.ClosedAt,

            session.OpeningBalance,

            session.ClosingBalance,

            session.OpeningBalance + movements.CashReceived - movements.CashPaidOut,

            session.Status,

            session.Notes,

            movements.CashReceived,

            movements.CashPaidOut,

            movements.CounterReceived,

            movements.CounterPaidOut,

            movements.DayOperationalReceived,

            movements.DayOperationalPaidOut);

}

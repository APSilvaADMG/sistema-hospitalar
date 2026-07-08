using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.DTOs.Financial;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Connect;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Pix;

public class PixPaymentService(
    AppDbContext dbContext,
    IFinancialAccountService financialAccountService,
    ConnectMessagingService messaging,
    IOptions<ConnectSettings> settings,
    ILogger<PixPaymentService> logger) : IPixPaymentService
{
    private readonly CollectionSettings _collection = settings.Value.Collection;

    public async Task<PixChargeDto> CreateChargeForAccountAsync(
        Guid financialAccountId,
        CancellationToken cancellationToken = default)
    {
        if (!_collection.PixEnabled)
        {
            throw new InvalidOperationException("Cobrança PIX automática não está habilitada.");
        }

        var account = await dbContext.FinancialAccounts
            .Include(f => f.Patient)
            .FirstOrDefaultAsync(f => f.Id == financialAccountId && f.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Conta financeira não encontrada.");

        if (account.Direction != FinancialAccountDirection.Receivable || account.PatientId is null)
        {
            throw new InvalidOperationException("PIX disponível apenas para contas a receber de pacientes.");
        }

        if (account.Status is FinancialAccountStatus.Paid or FinancialAccountStatus.Cancelled)
        {
            throw new InvalidOperationException("Conta já quitada ou cancelada.");
        }

        var balance = account.Amount - account.PaidAmount;
        if (balance <= 0)
        {
            throw new InvalidOperationException("Não há saldo em aberto para cobrança PIX.");
        }

        var existingPending = await dbContext.PixCharges
            .Where(c => c.FinancialAccountId == financialAccountId
                && c.Status == PixChargeStatus.Pending
                && c.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingPending is not null && existingPending.Amount == balance)
        {
            return MapToDto(existingPending, account.Patient!.FullName);
        }

        if (existingPending is not null)
        {
            existingPending.Status = PixChargeStatus.Cancelled;
            existingPending.UpdatedAt = DateTime.UtcNow;
        }

        var txId = PixBrCodeHelper.GenerateTxId();
        var copyPaste = PixBrCodeHelper.BuildCopyPasteCode(
            _collection.PixKey,
            _collection.PixBeneficiary,
            _collection.PixCity,
            balance,
            txId);

        var charge = new PixCharge
        {
            FinancialAccountId = account.Id,
            PatientId = account.PatientId!.Value,
            TxId = txId,
            Amount = balance,
            CopyPasteCode = copyPaste,
            ExpiresAt = DateTime.UtcNow.AddHours(_collection.PixChargeExpirationHours),
            ProviderReference = _collection.UseMockPixProvider ? "mock" : "gateway",
        };

        dbContext.PixCharges.Add(charge);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Cobrança PIX criada {TxId} para conta {AccountId} — R$ {Amount}", txId, account.Id, balance);
        return MapToDto(charge, account.Patient!.FullName);
    }

    public async Task<PixChargeDto?> GetChargeAsync(Guid chargeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PixCharges.AsNoTracking()
            .Where(c => c.Id == chargeId)
            .Select(c => new PixChargeDto(
                c.Id,
                c.FinancialAccountId,
                c.PatientId,
                c.Patient.FullName,
                c.TxId,
                c.Amount,
                c.Status,
                c.CopyPasteCode,
                c.ExpiresAt,
                c.PaidAt,
                c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PixChargeDto?> GetActiveChargeForAccountAsync(
        Guid financialAccountId,
        CancellationToken cancellationToken = default)
    {
        await ExpireStaleChargesAsync(cancellationToken);

        return await dbContext.PixCharges.AsNoTracking()
            .Where(c => c.FinancialAccountId == financialAccountId
                && c.Status == PixChargeStatus.Pending
                && c.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new PixChargeDto(
                c.Id,
                c.FinancialAccountId,
                c.PatientId,
                c.Patient.FullName,
                c.TxId,
                c.Amount,
                c.Status,
                c.CopyPasteCode,
                c.ExpiresAt,
                c.PaidAt,
                c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PixWebhookResult> ProcessWebhookAsync(
        PixWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TxId))
        {
            return new PixWebhookResult(false, "TxId obrigatório.", null, null);
        }

        if (!IsPaidStatus(request.Status))
        {
            return new PixWebhookResult(false, "Status ignorado.", null, null);
        }

        var charge = await dbContext.PixCharges
            .Include(c => c.Patient)
            .Include(c => c.FinancialAccount)
            .FirstOrDefaultAsync(c => c.TxId == request.TxId, cancellationToken);

        if (charge is null)
        {
            return new PixWebhookResult(false, "Cobrança não encontrada.", null, null);
        }

        if (charge.Status == PixChargeStatus.Paid)
        {
            return new PixWebhookResult(true, "Pagamento já confirmado.", charge.Id, charge.FinancialAccountId);
        }

        if (charge.Status != PixChargeStatus.Pending)
        {
            return new PixWebhookResult(false, "Cobrança não está pendente.", charge.Id, charge.FinancialAccountId);
        }

        if (charge.ExpiresAt < DateTime.UtcNow)
        {
            charge.Status = PixChargeStatus.Expired;
            charge.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return new PixWebhookResult(false, "Cobrança expirada.", charge.Id, charge.FinancialAccountId);
        }

        var paidAmount = request.Amount ?? charge.Amount;
        await ConfirmPaymentAsync(charge, paidAmount, request.PayerName, cancellationToken);

        return new PixWebhookResult(true, "Pagamento PIX confirmado e conta baixada.", charge.Id, charge.FinancialAccountId);
    }

    public async Task<PixChargeDto?> SimulatePaymentAsync(Guid chargeId, CancellationToken cancellationToken = default)
    {
        if (!_collection.UseMockPixProvider)
        {
            throw new InvalidOperationException("Simulação disponível apenas com provedor PIX mock.");
        }

        var charge = await dbContext.PixCharges.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == chargeId, cancellationToken);

        if (charge is null)
        {
            return null;
        }

        var result = await ProcessWebhookAsync(new PixWebhookRequest(
            charge.TxId,
            "CONCLUIDA",
            charge.Amount,
            "Pagamento simulado"), cancellationToken);

        return result.Processed ? await GetChargeAsync(chargeId, cancellationToken) : null;
    }

    private async Task ConfirmPaymentAsync(
        PixCharge charge,
        decimal paidAmount,
        string? payerName,
        CancellationToken cancellationToken)
    {
        charge.Status = PixChargeStatus.Paid;
        charge.PaidAt = DateTime.UtcNow;
        charge.PayerName = payerName;
        charge.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await financialAccountService.RegisterPaymentAsync(
            charge.FinancialAccountId,
            new RegisterPaymentRequest(paidAmount, PaymentMethod.Pix, charge.PaidAt, null, charge.Id),
            cancellationToken);

        await NotifyPatientAsync(charge, cancellationToken);
        logger.LogInformation("PIX {TxId} confirmado — conta {AccountId}", charge.TxId, charge.FinancialAccountId);
    }

    private async Task NotifyPatientAsync(PixCharge charge, CancellationToken cancellationToken)
    {
        var phone = charge.Patient.MobilePhone ?? charge.Patient.Phone;
        if (string.IsNullOrWhiteSpace(phone))
        {
            return;
        }

        try
        {
            var conversation = await messaging.GetOrCreateConversationAsync(phone, charge.Patient.FullName, cancellationToken);
            conversation.PatientId = charge.PatientId;
            conversation.BotStep = ConnectBotStep.MainMenu;

            var culture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
            await messaging.SendOutboundAsync(
                conversation,
                $"""
                ✅ Pagamento PIX confirmado!

                Conta: {charge.FinancialAccount.Description}
                Valor: {charge.Amount.ToString("C", culture)}
                ID: {charge.TxId}

                Obrigado! Sua conta foi quitada automaticamente.
                """,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao notificar paciente sobre PIX {TxId}", charge.TxId);
        }
    }

    private async Task ExpireStaleChargesAsync(CancellationToken cancellationToken)
    {
        var stale = await dbContext.PixCharges
            .Where(c => c.Status == PixChargeStatus.Pending && c.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            return;
        }

        foreach (var charge in stale)
        {
            charge.Status = PixChargeStatus.Expired;
            charge.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsPaidStatus(string status)
    {
        var normalized = status.Trim().ToUpperInvariant();
        return normalized is "CONCLUIDA" or "PAID" or "CONFIRMED" or "COMPLETED";
    }

    private static PixChargeDto MapToDto(PixCharge charge, string patientName)
        => new(
            charge.Id,
            charge.FinancialAccountId,
            charge.PatientId,
            patientName,
            charge.TxId,
            charge.Amount,
            charge.Status,
            charge.CopyPasteCode,
            charge.ExpiresAt,
            charge.PaidAt,
            charge.CreatedAt);
}

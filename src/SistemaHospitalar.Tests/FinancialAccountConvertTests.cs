using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Services;
using Xunit;

namespace SistemaHospitalar.Tests;

public class FinancialAccountConvertTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly FinancialAccountService _service;
    private readonly Guid _patientId = Guid.NewGuid();

    public FinancialAccountConvertTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"financial-convert-{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
        _service = new FinancialAccountService(_db);

        _db.Patients.Add(new Patient
        {
            Id = _patientId,
            FullName = "Paciente Financeiro",
            Cpf = "12345678901",
            BirthDate = new DateOnly(1992, 3, 20),
            IsActive = true,
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task ConvertProposalToBillingAsync_ValidProposal_CreatesBillingWithOutstanding()
    {
        var proposalId = Guid.NewGuid();
        _db.FinancialAccounts.Add(new FinancialAccount
        {
            Id = proposalId,
            PatientId = _patientId,
            Direction = FinancialAccountDirection.Receivable,
            Description = "Proposta — Pacote cirúrgico",
            Amount = 1200m,
            PaidAmount = 200m,
            Status = FinancialAccountStatus.PartiallyPaid,
            Category = FinancialAccountCategory.Exam,
            DueDate = DateTime.UtcNow.AddDays(10),
            IsActive = true,
            LineItems =
            [
                new FinancialAccountLineItem
                {
                    Description = "Honorários",
                    Quantity = 1,
                    UnitAmount = 1200m,
                    TotalAmount = 1200m,
                    IsActive = true,
                },
            ],
        });
        await _db.SaveChangesAsync();

        var result = await _service.ConvertProposalToBillingAsync(proposalId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("Faturamento", result!.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1000m, result.Amount);
        Assert.Equal(proposalId, result.ParentFinancialAccountId);

        var billingCount = await _db.FinancialAccounts.CountAsync(
            f => f.ParentFinancialAccountId == proposalId && f.IsActive);
        Assert.Equal(1, billingCount);
    }

    [Fact]
    public async Task ConvertProposalToBillingAsync_UnknownId_ReturnsNull()
    {
        var result = await _service.ConvertProposalToBillingAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ConvertProposalToBillingAsync_PayableProposal_Throws()
    {
        var proposalId = Guid.NewGuid();
        _db.FinancialAccounts.Add(new FinancialAccount
        {
            Id = proposalId,
            Direction = FinancialAccountDirection.Payable,
            Description = "Proposta — despesa",
            Amount = 500m,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ConvertProposalToBillingAsync(proposalId, CancellationToken.None));

        Assert.Contains("propostas a receber", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConvertProposalToBillingAsync_NotMarkedAsProposal_Throws()
    {
        var accountId = Guid.NewGuid();
        _db.FinancialAccounts.Add(new FinancialAccount
        {
            Id = accountId,
            PatientId = _patientId,
            Direction = FinancialAccountDirection.Receivable,
            Description = "Consulta avulsa",
            Amount = 300m,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ConvertProposalToBillingAsync(accountId, CancellationToken.None));

        Assert.Contains("proposta", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConvertProposalToBillingAsync_FullyPaidProposal_Throws()
    {
        var proposalId = Guid.NewGuid();
        _db.FinancialAccounts.Add(new FinancialAccount
        {
            Id = proposalId,
            PatientId = _patientId,
            Direction = FinancialAccountDirection.Receivable,
            Description = "Proposta — consulta",
            Amount = 400m,
            PaidAmount = 400m,
            Status = FinancialAccountStatus.Paid,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ConvertProposalToBillingAsync(proposalId, CancellationToken.None));

        Assert.Contains("saldo em aberto", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetPayableCategoryPresets_IncludesPayrollAndUtilities()
    {
        var presets = _service.GetPayableCategoryPresets();

        Assert.Contains(presets, p => p.Label.Contains("Folha", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(presets, p => p.Category == FinancialAccountCategory.Utilities);
        Assert.True(presets.Count >= 5);
    }

    public void Dispose() => _db.Dispose();
}

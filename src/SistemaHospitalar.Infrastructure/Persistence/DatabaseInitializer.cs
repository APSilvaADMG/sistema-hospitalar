using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Infrastructure;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var enableDemoSeeds = HospitalOptions.ResolveEnableDemoSeeds(configuration);

        if (HospitalOptions.ResolveCiMinimalBootstrap(configuration))
        {
            await ApplyCiMinimalBootstrapAsync(dbContext, cancellationToken);
            return;
        }

        await dbContext.Database.MigrateAsync(cancellationToken);
        await EnsureAdminProfessionalLinkAsync(dbContext, cancellationToken);
        await HospitalizationSnippetSeed.EnsureAsync(dbContext, cancellationToken);
        await TransportSeed.EnsureAsync(dbContext, cancellationToken);
        await HotelariaSeed.EnsureAsync(dbContext, cancellationToken);
        await RolePermissionSeed.EnsureAsync(dbContext, cancellationToken);
        await OpenHospitalCatalogSeed.EnsureAsync(dbContext, cancellationToken);
        await MadreInspiredCatalogSeed.EnsureAsync(dbContext, cancellationToken);
        await HospitalErpCatalogSeed.EnsureAsync(dbContext, cancellationToken);
        await TvSignageSeed.EnsureAsync(dbContext, cancellationToken);
        await ConsentTermSeed.EnsureAsync(dbContext, cancellationToken);
        await ServiceUnitSeed.EnsureAsync(dbContext, cancellationToken);
        if (enableDemoSeeds)
        {
            await EnsureDemoRoleUsersAsync(dbContext, cancellationToken);
        }
        else
        {
            logger.LogInformation("Seeds de demonstração desabilitados");
        }

        await BackfillPatientFieldEncryptionAsync(scope.ServiceProvider, cancellationToken);

        if (!await dbContext.Specialties.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Aplicando dados iniciais de demonstração...");

            var clinicaGeral = new Specialty { Name = "Clínica Geral", CboCode = "225125" };
            var cardiologia = new Specialty { Name = "Cardiologia", CboCode = "225120" };
            var pediatria = new Specialty { Name = "Pediatria", CboCode = "225124" };

            dbContext.Specialties.AddRange(clinicaGeral, cardiologia, pediatria);

            var ana = new Professional { FullName = "Dra. Ana Paula Silva", Crm = "123456-SP", Specialty = clinicaGeral, Email = "ana.silva@hospital.local" };

            dbContext.Professionals.AddRange(
                ana,
                new Professional { FullName = "Dr. Carlos Mendes", Crm = "654321-SP", Specialty = cardiologia, Email = "carlos.mendes@hospital.local" },
                new Professional { FullName = "Dra. Juliana Costa", Crm = "789012-SP", Specialty = pediatria, Email = "juliana.costa@hospital.local" });

            dbContext.HealthInsurances.AddRange(
                new HealthInsurance { Name = "Particular", LogoUrl = "/insurers/particular.svg" },
                new HealthInsurance { Name = "SUS", LogoUrl = "/insurers/sus.svg" });

            await dbContext.SaveChangesAsync(cancellationToken);
            await HealthInsuranceCatalogSeed.EnsureAsync(dbContext, logger, cancellationToken);

            if (!await dbContext.Users.AnyAsync(cancellationToken))
            {
                SeedUsers(dbContext, ana.Id);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        else if (!await dbContext.Users.AnyAsync(cancellationToken))
        {
            var professional = await dbContext.Professionals.FirstAsync(cancellationToken);
            SeedUsers(dbContext, professional.Id);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Wards.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Aplicando dados da Fase 2...");
            SeedPhase2(dbContext);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.LabExamCatalogs.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Aplicando dados da Fase 3...");
            SeedPhase3(dbContext);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Cid10Catalogs.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Aplicando dados da Fase 4...");
            await SeedPhase4Async(dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await Cid10CatalogSeed.EnsureAsync(dbContext, cancellationToken);

        if (!await dbContext.Suppliers.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Aplicando dados da Fase 5...");
            await SeedPhase5Async(dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Ambulances.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Aplicando dados da Fase 6...");
            await SeedPhase6Async(dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.TussCatalogs.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Aplicando catálogo TUSS e SIGTAP...");
            dbContext.TussCatalogs.AddRange(TissCatalogSeed.Items);
            dbContext.SigtapProcedures.AddRange(TissCatalogSeed.SigtapItems);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (await dbContext.TussCatalogs.CountAsync(cancellationToken) < 40)
        {
            logger.LogInformation("Expandindo catálogo TUSS com procedimentos hospitalares...");
            var existingCodes = await dbContext.TussCatalogs.Select(t => t.Code).ToListAsync(cancellationToken);
            var existingSet = existingCodes.ToHashSet();
            var expanded = TissCatalogExpandedSeed.Items.Where(i => !existingSet.Contains(i.Code)).ToList();
            if (expanded.Count > 0)
            {
                dbContext.TussCatalogs.AddRange(expanded);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        if (!await dbContext.CbhpmProcedures.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Aplicando catálogos CBHPM, Brasíndice e SIMPRO...");
            dbContext.CbhpmProcedures.AddRange(BillingCatalogSeed.CbhpmItems);
            dbContext.BrasindiceItems.AddRange(BillingCatalogSeed.BrasindiceItems);
            dbContext.SimproItems.AddRange(BillingCatalogSeed.SimproItems);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.ConsultingRooms.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Aplicando dados da Fase 7...");
            await SeedPhase7Async(dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.InstrumentKits.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Aplicando dados da Fase 8...");
            await SeedPhase8Async(dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.ChemotherapySessions.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Aplicando dados da Fase 9...");
            await SeedPhase9Async(dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (await dbContext.MedicationCatalogs.AnyAsync(m => m.PackageInsert == null, cancellationToken))
        {
            logger.LogInformation("Aplicando bulas do catálogo de medicamentos...");
            await MedicationBulaSeed.ApplyAsync(dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var consultaRemediosJsonl = ConsultaRemediosBulaImport.ResolveActiveJsonlPath();
        if (File.Exists(consultaRemediosJsonl))
        {
            logger.LogInformation("Importando bulas do Consulta Remédios ({Path})...", consultaRemediosJsonl);
            await ConsultaRemediosBulaImport.ApplyFromJsonlAsync(dbContext, consultaRemediosJsonl, logger, cancellationToken);
        }

        if (await dbContext.Wards.AnyAsync(w => w.Code == null, cancellationToken))
        {
            logger.LogInformation("Atualizando alas por modalidade (SUS, Convênio, Particular)...");
            await WardDetailsSeed.ApplyAsync(dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (enableDemoSeeds && !await dbContext.FinancialAccounts.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Aplicando contas financeiras de demonstração...");
            await SeedFinancialAccountsAsync(dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (enableDemoSeeds && !await dbContext.FinancialPayments.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Aplicando lançamentos de pagamento de demonstração...");
            await SeedFinancialPaymentsAsync(dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.TpaAdministrators.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Aplicando dados iniciais de TPA...");
            var adm = new TpaAdministrator
            {
                Name = "TPA Prime Gestão",
                Cnpj = "11222333000144",
                ContactName = "Operações TPA",
                ContactEmail = "tpa@prime.local",
                CommissionPercent = 5m,
                DiscountPercent = 2m
            };
            dbContext.TpaAdministrators.Add(adm);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (enableDemoSeeds)
            {
                var patient = await dbContext.Patients.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
                var insurance = await dbContext.HealthInsurances.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
                if (patient is not null)
                {
                    dbContext.TpaClaims.Add(new TpaClaim
                    {
                        TpaAdministratorId = adm.Id,
                        PatientId = patient.Id,
                        HealthInsuranceId = insurance?.Id,
                        ServiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                        GrossAmount = 1500m,
                        CommissionAmount = 75m,
                        DiscountAmount = 30m,
                        NetAmount = 1395m,
                        Status = TpaClaimStatus.Submitted,
                        Notes = "Claim inicial de demonstração"
                    });
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }

        await EnsurePurchasingCatalogAsync(dbContext, cancellationToken);
        await ConnectKnowledgeSeed.EnsureAsync(dbContext, cancellationToken);
        await HelpContentSeed.EnsureAsync(dbContext, cancellationToken);
        await ClinicalCatalogSeed.ApplyAsync(dbContext, cancellationToken);
        await HospitalMedicationMasterSeed.EnsureAsync(dbContext, cancellationToken);

        // Catálogo de convênios é essencial (não é seed de pacientes fictícios).
        await RunDemoSeedSafelyAsync("HealthInsuranceCatalog", dbContext, () => HealthInsuranceCatalogSeed.EnsureAsync(dbContext, logger, cancellationToken), logger);

        if (!enableDemoSeeds)
        {
            return;
        }

        await RunDemoSeedSafelyAsync("WarehouseDemo", dbContext, () => WarehouseDemoSeed.EnsureAsync(dbContext, logger, cancellationToken), logger);
        await RunDemoSeedSafelyAsync("MedicationInsurance", dbContext, () => MedicationInsuranceDemoSeed.EnsureAsync(dbContext, logger, cancellationToken), logger);
        await RunDemoSeedSafelyAsync("InventoryLookup", dbContext, () => InventoryLookupDemoSeed.EnsureAsync(dbContext, logger, cancellationToken), logger);
        await RunDemoSeedSafelyAsync("FinanceVariety", dbContext, () => FinanceVarietyDemoSeed.EnsureAsync(dbContext, logger, cancellationToken), logger);
        await RunDemoSeedSafelyAsync("BiDemo", dbContext, () => BiDemoSeed.EnsureAsync(dbContext, logger, cancellationToken), logger);
        await RunDemoSeedSafelyAsync("HrDemo", dbContext, () => HrDemoSeed.EnsureAsync(dbContext, logger, cancellationToken), logger);
        await RunDemoSeedSafelyAsync("HrEventsDemo", dbContext, () => HrEventsDemoSeed.EnsureAsync(dbContext, logger, cancellationToken), logger);
        await RunDemoSeedSafelyAsync("PendencyDemo", dbContext, () => PendencyDemoSeed.EnsureAsync(dbContext, logger, cancellationToken), logger);
        await RunDemoSeedSafelyAsync("WasteDemo", dbContext, () => WasteDemoSeed.EnsureAsync(dbContext, cancellationToken), logger);
        await RunDemoSeedSafelyAsync("ClinicAmbulatory", dbContext, () => ClinicAmbulatorySeed.EnsureAsync(dbContext, logger, cancellationToken), logger);
        await RunDemoSeedSafelyAsync("FeegowPatientPep", dbContext, () => FeegowPatientPepSeed.EnsureAsync(dbContext, logger, cancellationToken), logger);
        await RunDemoSeedSafelyAsync("CashSessionDemo", dbContext, () => CashSessionDemoSeed.EnsureAsync(dbContext, logger, cancellationToken), logger);
        await RunDemoSeedSafelyAsync("OperationalDemo", dbContext, () => OperationalDemoSeed.EnsureAsync(dbContext, logger, cancellationToken), logger);
        await RunDemoSeedSafelyAsync("WaitingRoomDemo", dbContext, () => WaitingRoomDemoSeed.EnsureAsync(dbContext, logger, cancellationToken), logger);
        await RunDemoSeedSafelyAsync("DemoDataConsistencyRepair", dbContext, () => DemoDataConsistencyRepair.EnsureAsync(dbContext, logger, cancellationToken), logger);
    }

    private static async Task ApplyCiMinimalBootstrapAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
        await RolePermissionSeed.EnsureAsync(dbContext, cancellationToken);

        if (!await dbContext.Specialties.AnyAsync(cancellationToken))
        {
            var clinicaGeral = new Specialty { Name = "Clínica Geral", CboCode = "225125" };
            dbContext.Specialties.Add(clinicaGeral);
            var ana = new Professional
            {
                FullName = "Dra. Ana Paula Silva",
                Crm = "123456-SP",
                Specialty = clinicaGeral,
                Email = "ana.silva@hospital.local",
            };
            dbContext.Professionals.Add(ana);
            dbContext.HealthInsurances.AddRange(
                new HealthInsurance { Name = "Particular", LogoUrl = "/insurers/particular.svg" },
                new HealthInsurance { Name = "SUS", LogoUrl = "/insurers/sus.svg" });
            await dbContext.SaveChangesAsync(cancellationToken);
            SeedUsers(dbContext, ana.Id);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (!await dbContext.Users.AnyAsync(cancellationToken))
        {
            var professional = await dbContext.Professionals.FirstAsync(cancellationToken);
            SeedUsers(dbContext, professional.Id);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await EnsureAdminProfessionalLinkAsync(dbContext, cancellationToken);
    }

    private static async Task RunDemoSeedSafelyAsync(string seedName, AppDbContext dbContext, Func<Task> seed, ILogger logger)
    {
        try
        {
            await seed();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Seed {SeedName} falhou — demais seeds continuam.", seedName);
            dbContext.ChangeTracker.Clear();
        }
    }

    private static async Task EnsurePurchasingCatalogAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var catalog = new[]
        {
            new Product { Name = "Reagente hematologia 1L", Sku = "LAB-REA01", Type = ProductType.Supply, Unit = "FR", QuantityOnHand = 8, MinimumStock = 12 },
            new Product { Name = "Contraste iodado 100ml", Sku = "IMG-CON100", Type = ProductType.Supply, Unit = "FR", QuantityOnHand = 15, MinimumStock = 20 },
            new Product { Name = "Kit cirúrgico descartável", Sku = "CIR-KIT01", Type = ProductType.Supply, Unit = "KIT", QuantityOnHand = 25, MinimumStock = 30 },
            new Product { Name = "Roupa hospitalar paciente", Sku = "LAV-ROUP", Type = ProductType.Supply, Unit = "UN", QuantityOnHand = 40, MinimumStock = 60 },
            new Product { Name = "Peça manutenção equipamento", Sku = "ENG-MANUT", Type = ProductType.Supply, Unit = "UN", QuantityOnHand = 2, MinimumStock = 5 },
            new Product { Name = "Suplemento nutricional 200ml", Sku = "NUT-DIETA", Type = ProductType.Supply, Unit = "UN", QuantityOnHand = 35, MinimumStock = 50 },
            new Product { Name = "Desinfetante hospitalar 5L", Sku = "CCIH-DESINF", Type = ProductType.Supply, Unit = "GL", QuantityOnHand = 6, MinimumStock = 10 },
            new Product { Name = "Lençol hospitalar", Sku = "HOTEL-LEN", Type = ProductType.Supply, Unit = "UN", QuantityOnHand = 18, MinimumStock = 40 },
        };

        var existingSkus = await dbContext.Products
            .Where(p => catalog.Select(c => c.Sku).Contains(p.Sku))
            .Select(p => p.Sku)
            .ToListAsync(cancellationToken);

        var missing = catalog.Where(c => !existingSkus.Contains(c.Sku)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        dbContext.Products.AddRange(missing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedFinancialAccountsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var joao = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Cpf == "52998224725", cancellationToken);
        var anderson = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.FullName.Contains("Anderson"), cancellationToken);
        var fallbackPatient = joao
            ?? anderson
            ?? await dbContext.Patients.FirstOrDefaultAsync(cancellationToken);

        if (fallbackPatient is null)
        {
            return;
        }

        dbContext.FinancialAccounts.AddRange(
            new FinancialAccount
            {
                PatientId = (joao ?? fallbackPatient).Id,
                Category = FinancialAccountCategory.Hospitalization,
                Description = "Internação UTI — coparticipação e diárias",
                Notes = "Coparticipação 20% + 3 diárias UTI",
                Amount = 4850m,
                DueDate = DateTime.UtcNow.AddDays(7),
            },
            new FinancialAccount
            {
                PatientId = (anderson ?? fallbackPatient).Id,
                Category = FinancialAccountCategory.Copayment,
                Description = "Consulta cardiologia — Empresarial Premium Nacional",
                Amount = 320m,
                DueDate = DateTime.UtcNow.AddDays(15),
            },
            new FinancialAccount
            {
                PatientId = fallbackPatient.Id,
                Category = FinancialAccountCategory.Exam,
                Description = "Exames laboratoriais — pacote ambulatorial",
                Amount = 185.50m,
                PaidAmount = 85.50m,
                Status = FinancialAccountStatus.PartiallyPaid,
                DueDate = DateTime.UtcNow.AddDays(-3),
            });

        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(cancellationToken);
        if (supplier is not null)
        {
            dbContext.FinancialAccounts.AddRange(
                new FinancialAccount
                {
                    Direction = FinancialAccountDirection.Payable,
                    SupplierId = supplier.Id,
                    Category = FinancialAccountCategory.SupplierPurchase,
                    Description = $"Pedido de compras — {supplier.Name}",
                    Amount = 4280m,
                    DueDate = DateTime.UtcNow.AddDays(12),
                },
                new FinancialAccount
                {
                    Direction = FinancialAccountDirection.Payable,
                    CounterpartyName = "Companhia de Energia",
                    Category = FinancialAccountCategory.Utilities,
                    Description = "Conta de energia elétrica — unidade principal",
                    Amount = 3120.45m,
                    DueDate = DateTime.UtcNow.AddDays(8),
                },
                new FinancialAccount
                {
                    Direction = FinancialAccountDirection.Payable,
                    CounterpartyName = "Folha de pagamento",
                    Category = FinancialAccountCategory.Payroll,
                    Description = "Folha de pagamento — equipe assistencial",
                    Amount = 186500m,
                    PaidAmount = 186500m,
                    Status = FinancialAccountStatus.Paid,
                    PaidAt = DateTime.UtcNow.AddDays(-2),
                    DueDate = DateTime.UtcNow.AddDays(-2),
                });
        }
    }

    private static async Task SeedFinancialPaymentsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var examAccount = await dbContext.FinancialAccounts
            .FirstOrDefaultAsync(
                f => f.Description.Contains("Exames laboratoriais") && f.PaidAmount > 0,
                cancellationToken);

        if (examAccount is not null)
        {
            var paidAt = DateTime.UtcNow.AddDays(-5);
            dbContext.FinancialPayments.Add(new FinancialPayment
            {
                FinancialAccountId = examAccount.Id,
                Amount = examAccount.PaidAmount,
                Method = PaymentMethod.Pix,
                PaidAt = paidAt,
                Notes = "Pagamento parcial — demonstração",
            });
            examAccount.PaidAt = paidAt;
        }

        var payrollAccount = await dbContext.FinancialAccounts
            .FirstOrDefaultAsync(
                f => f.Description.Contains("Folha de pagamento") && f.Status == FinancialAccountStatus.Paid,
                cancellationToken);

        if (payrollAccount is not null && payrollAccount.PaidAmount > 0)
        {
            var paidAt = payrollAccount.PaidAt ?? DateTime.UtcNow.AddDays(-2);
            dbContext.FinancialPayments.Add(new FinancialPayment
            {
                FinancialAccountId = payrollAccount.Id,
                Amount = payrollAccount.PaidAmount,
                Method = PaymentMethod.BankTransfer,
                PaidAt = paidAt,
                Notes = "Transferência bancária — folha mensal",
            });
        }
    }

    private static async Task EnsureAdminProfessionalLinkAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var admin = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "admin@hospital.local", cancellationToken);

        if (admin is null || admin.ProfessionalId.HasValue)
        {
            return;
        }

        var professional = await dbContext.Professionals
            .OrderBy(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (professional is null)
        {
            return;
        }

        admin.ProfessionalId = professional.Id;
        admin.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDemoRoleUsersAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var demoUsers =
            new (string Email, string FullName, string Password, UserRole Role)[]
            {
                ("farmacia@hospital.local", "Farmácia Central", "Farmacia123!", UserRole.Pharmacy),
                ("maqueiro@hospital.local", "Maqueiro João", "Maqueiro123!", UserRole.Porter),
                ("hotelaria@hospital.local", "Hotelaria Maria", "Hotelaria123!", UserRole.Hospitality),
                ("auditor@hospital.local", "Auditoria Clínica", "Auditor123!", UserRole.Auditor),
                ("faturamento@hospital.local", "Faturamento TISS", "Faturamento123!", UserRole.Billing),
            };

        foreach (var (email, fullName, password, role) in demoUsers)
        {
            if (await dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken))
            {
                continue;
            }

            dbContext.Users.Add(new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void SeedUsers(AppDbContext dbContext, Guid doctorProfessionalId)
    {
        dbContext.Users.AddRange(
            new User
            {
                FullName = "Administrador",
                Email = "admin@hospital.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = UserRole.Admin,
                ProfessionalId = doctorProfessionalId
            },
            new User
            {
                FullName = "Recepção Central",
                Email = "recepcao@hospital.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Recepcao123!"),
                Role = UserRole.Reception
            },
            new User
            {
                FullName = "Dra. Ana Paula Silva",
                Email = "medico@hospital.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Medico123!"),
                Role = UserRole.Doctor,
                ProfessionalId = doctorProfessionalId
            });
    }

    private static void SeedPhase2(AppDbContext dbContext)
    {
        WardDetailsSeed.SeedFresh(dbContext);

        dbContext.OperatingRooms.AddRange(
            new OperatingRoom { Name = "Sala 1", Location = "Bloco Cirúrgico — 1º andar" },
            new OperatingRoom { Name = "Sala 2", Location = "Bloco Cirúrgico — 1º andar" },
            new OperatingRoom { Name = "Sala 3", Location = "Bloco Cirúrgico — 1º andar" });

        dbContext.Products.AddRange(
            new Product { Name = "Dipirona 500mg", Sku = "MED-DIP500", Type = ProductType.Medication, Unit = "CP", QuantityOnHand = 500, MinimumStock = 100 },
            new Product { Name = "Soro Fisiológico 500ml", Sku = "MED-SF500", Type = ProductType.Medication, Unit = "FR", QuantityOnHand = 200, MinimumStock = 50 },
            new Product { Name = "Omeprazol 20mg", Sku = "MED-OME20", Type = ProductType.Medication, Unit = "CP", QuantityOnHand = 80, MinimumStock = 100 },
            new Product { Name = "Luvas procedimento M", Sku = "SUP-LUV-M", Type = ProductType.Supply, Unit = "CX", QuantityOnHand = 30, MinimumStock = 10 },
            new Product { Name = "Gaze estéril", Sku = "SUP-GAZ", Type = ProductType.Supply, Unit = "PC", QuantityOnHand = 150, MinimumStock = 40 },
            new Product { Name = "Serum fisiológico equipos", Sku = "SUP-EQP", Type = ProductType.Supply, Unit = "UN", QuantityOnHand = 5, MinimumStock = 20 },
            new Product { Name = "Reagente hematologia 1L", Sku = "LAB-REA01", Type = ProductType.Supply, Unit = "FR", QuantityOnHand = 8, MinimumStock = 12 },
            new Product { Name = "Contraste iodado 100ml", Sku = "IMG-CON100", Type = ProductType.Supply, Unit = "FR", QuantityOnHand = 15, MinimumStock = 20 },
            new Product { Name = "Kit cirúrgico descartável", Sku = "CIR-KIT01", Type = ProductType.Supply, Unit = "KIT", QuantityOnHand = 25, MinimumStock = 30 },
            new Product { Name = "Roupa hospitalar paciente", Sku = "LAV-ROUP", Type = ProductType.Supply, Unit = "UN", QuantityOnHand = 40, MinimumStock = 60 },
            new Product { Name = "Peça manutenção equipamento", Sku = "ENG-MANUT", Type = ProductType.Supply, Unit = "UN", QuantityOnHand = 2, MinimumStock = 5 },
            new Product { Name = "Suplemento nutricional 200ml", Sku = "NUT-DIETA", Type = ProductType.Supply, Unit = "UN", QuantityOnHand = 35, MinimumStock = 50 },
            new Product { Name = "Desinfetante hospitalar 5L", Sku = "CCIH-DESINF", Type = ProductType.Supply, Unit = "GL", QuantityOnHand = 6, MinimumStock = 10 },
            new Product { Name = "Lençol hospitalar", Sku = "HOTEL-LEN", Type = ProductType.Supply, Unit = "UN", QuantityOnHand = 18, MinimumStock = 40 });
    }

    private static void SeedPhase3(AppDbContext dbContext)
    {
        dbContext.LabExamCatalogs.AddRange(
            new LabExamCatalog { Name = "Hemograma completo", TussCode = "40304361", SampleType = "Sangue", ReferenceRange = "Variável", Unit = "-" },
            new LabExamCatalog { Name = "Glicemia em jejum", TussCode = "40302040", SampleType = "Sangue", ReferenceRange = "70-99", Unit = "mg/dL" },
            new LabExamCatalog { Name = "Creatinina", TussCode = "40301630", SampleType = "Sangue", ReferenceRange = "0.7-1.3", Unit = "mg/dL" },
            new LabExamCatalog { Name = "TSH", TussCode = "40316521", SampleType = "Sangue", ReferenceRange = "0.4-4.0", Unit = "mUI/L" },
            new LabExamCatalog { Name = "Urina tipo I", TussCode = "40311210", SampleType = "Urina", ReferenceRange = "Variável", Unit = "-" });

        var labDept = new Department { Name = "Laboratório", Description = "Análises clínicas" };
        var imgDept = new Department { Name = "Diagnóstico por Imagem", Description = "Radiologia e tomografia" };
        var adminDept = new Department { Name = "Administrativo", Description = "Gestão hospitalar" };
        var enfermagem = new Department { Name = "Enfermagem", Description = "Equipe de enfermagem" };

        dbContext.Departments.AddRange(labDept, imgDept, adminDept, enfermagem);

        dbContext.Employees.AddRange(
            new Employee { FullName = "Maria Santos", Role = EmployeeRole.Nurse, Department = enfermagem, BirthDate = new DateOnly(1985, 6, 12), HireDate = new DateOnly(2022, 3, 15), Email = "maria.santos@hospital.local", JobTitle = "Enfermeira" },
            new Employee { FullName = "João Pereira", Role = EmployeeRole.Technician, Department = labDept, BirthDate = new DateOnly(1990, 6, 8), HireDate = new DateOnly(2021, 8, 1), Email = "joao.pereira@hospital.local", JobTitle = "Técnico de laboratório" },
            new Employee { FullName = "Fernanda Lima", Role = EmployeeRole.Technician, Department = imgDept, BirthDate = new DateOnly(1988, 6, 25), HireDate = new DateOnly(2023, 1, 10), Email = "fernanda.lima@hospital.local", JobTitle = "Técnica de imagem" },
            new Employee { FullName = "Ricardo Alves", Role = EmployeeRole.Manager, Department = adminDept, BirthDate = new DateOnly(1975, 5, 20), HireDate = new DateOnly(2020, 5, 20), Email = "ricardo.alves@hospital.local", JobTitle = "Gerente administrativo" });
    }

    private static async Task SeedPhase4Async(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        dbContext.Cid10Catalogs.AddRange(
            new Cid10Catalog { Code = "J06.9", Description = "Infecção aguda das vias aéreas superiores, não especificada", Category = "Respiratório", Keywords = "tosse coriza febre gripe resfriado" },
            new Cid10Catalog { Code = "R50.9", Description = "Febre não especificada", Category = "Sintomas", Keywords = "febre calafrio temperatura" },
            new Cid10Catalog { Code = "R51", Description = "Cefaleia", Category = "Sintomas", Keywords = "dor de cabeça cefaleia enxaqueca" },
            new Cid10Catalog { Code = "I20.9", Description = "Angina pectoris, não especificada", Category = "Cardiovascular", Keywords = "dor no peito angina infarto" },
            new Cid10Catalog { Code = "R10.4", Description = "Dor abdominal e pélvica", Category = "Sintomas", Keywords = "dor abdominal barriga cólica" },
            new Cid10Catalog { Code = "R11", Description = "Náusea e vômito", Category = "Sintomas", Keywords = "vômito náusea enjoo" },
            new Cid10Catalog { Code = "Z00.0", Description = "Exame médico geral", Category = "Preventivo", Keywords = "check-up rotina exame preventivo" });

        var demoPatient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Cpf == "52998224725", cancellationToken);

        if (demoPatient is null)
        {
            demoPatient = new Patient
            {
                FullName = "João da Silva Santos",
                Cpf = "52998224725",
                BirthDate = new DateOnly(1985, 4, 12),
                Gender = Gender.Male,
                Email = "joao.santos@email.com",
                Phone = "11987654321",
                MedicalRecord = new MedicalRecord { RecordNumber = "PEP-2024-0001" }
            };
            dbContext.Patients.Add(demoPatient);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Users.AnyAsync(u => u.Email == "paciente@hospital.local", cancellationToken))
        {
            dbContext.Users.Add(new User
            {
                FullName = demoPatient.FullName,
                Email = "paciente@hospital.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Paciente123!"),
                Role = UserRole.Patient,
                PatientId = demoPatient.Id
            });
        }

        dbContext.IntegrationMessages.Add(new IntegrationMessage
        {
            Type = IntegrationMessageType.Hl7Inbound,
            Status = IntegrationMessageStatus.Processed,
            Source = "LIS-Demo",
            Payload = "MSH|^~\\&|LIS|HOSP|HIS|HOSP|20240608120000||ADT^A01|MSG001|P|2.5\rPID|1||12345^^^HOSP||Silva^Maria||19800101|F",
            ResponsePayload = "HL7 ADT^A01 — Paciente: Maria Silva",
            PatientId = demoPatient.Id
        });
    }

    private static async Task SeedPhase5Async(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var medDist = new Supplier
        {
            Name = "MedDistribuidora Brasil",
            Cnpj = "12345678000199",
            Email = "compras@meddistribuidora.com.br",
            Phone = "1133334444",
            ContactName = "Carlos Souza"
        };
        var insumos = new Supplier
        {
            Name = "Insumos Hospitalares SP",
            Cnpj = "98765432000188",
            Email = "vendas@insumossp.com.br",
            Phone = "1144445555",
            ContactName = "Ana Ribeiro"
        };
        dbContext.Suppliers.AddRange(medDist, insumos);

        var omeprazol = await dbContext.Products.FirstOrDefaultAsync(p => p.Sku == "MED-OME20", cancellationToken);
        var equipos = await dbContext.Products.FirstOrDefaultAsync(p => p.Sku == "SUP-EQP", cancellationToken);

        if (omeprazol is not null && equipos is not null)
        {
            var order = new PurchaseOrder
            {
                OrderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-0001",
                Supplier = medDist,
                Sector = PurchaseSector.Pharmacy,
                Priority = PurchasePriority.Urgent,
                RequestedBy = "Farmácia Central",
                Justification = "Reposição de estoque crítico — Omeprazol e equipos abaixo do mínimo",
                Status = PurchaseOrderStatus.Sent,
                ExpectedAt = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                TotalAmount = 850m,
                Notes = "Entrega prioritária — validar lote na recepção"
            };
            order.Items.Add(new PurchaseOrderItem
            {
                ProductId = omeprazol.Id,
                Quantity = 200,
                UnitPrice = 0.35m
            });
            order.Items.Add(new PurchaseOrderItem
            {
                ProductId = equipos.Id,
                Quantity = 50,
                UnitPrice = 12m
            });
            dbContext.PurchaseOrders.Add(order);
        }

        var patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Cpf == "52998224725", cancellationToken)
            ?? await dbContext.Patients.FirstOrDefaultAsync(cancellationToken);

        if (patient is not null)
        {
            dbContext.EmergencyVisits.Add(new EmergencyVisit
            {
                PatientId = patient.Id,
                ChiefComplaint = "Dor torácica intensa e falta de ar",
                Urgency = TriageUrgency.Emergency,
                Status = EmergencyVisitStatus.Waiting,
                ArrivedAt = DateTime.UtcNow.AddMinutes(-15)
            });
        }

        dbContext.AuditLogs.AddRange(
            new AuditLog
            {
                UserEmail = "admin@hospital.local",
                Action = "POST",
                EntityType = "auth",
                Details = "POST /api/auth/login → 200",
                IpAddress = "127.0.0.1"
            },
            new AuditLog
            {
                UserEmail = "recepcao@hospital.local",
                Action = "POST",
                EntityType = "patients",
                Details = "Cadastro de paciente demo",
                IpAddress = "192.168.0.10"
            });

        var admin = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "admin@hospital.local", cancellationToken);
        if (admin is not null)
        {
            dbContext.Notifications.AddRange(
                new Notification
                {
                    UserId = admin.Id,
                    Title = "Estoque baixo",
                    Message = "Omeprazol 20mg abaixo do mínimo — considere aprovar pedido de compra.",
                    Type = NotificationType.Warning,
                    RelatedEntityType = "Product"
                },
                new Notification
                {
                    UserId = admin.Id,
                    Title = "Emergência — fila PS",
                    Message = "Paciente aguardando atendimento com urgência Emergência.",
                    Type = NotificationType.Alert,
                    RelatedEntityType = "EmergencyVisit"
                });
        }
    }

    private static async Task SeedPhase6Async(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var amb1 = new Ambulance { Code = "AMB-01", Plate = "HSP1A23", BaseLocation = "Garagem — Bloco A" };
        var amb2 = new Ambulance { Code = "AMB-02", Plate = "HSP4B56", BaseLocation = "Garagem — Bloco A" };
        dbContext.Ambulances.AddRange(amb1, amb2);

        dbContext.AmbulanceDispatches.Add(new AmbulanceDispatch
        {
            PatientName = "Maria Oliveira",
            PickupAddress = "Av. Paulista, 1000 — São Paulo",
            Destination = "Pronto-Socorro — Entrada B",
            Status = AmbulanceDispatchStatus.Requested,
            Notes = "Suspeita de AVC — SAMU encaminhado"
        });

        var zoneA = new ParkingZone
        {
            Name = "Estacionamento A — Visitantes",
            TotalSpots = 80,
            HourlyRate = 8m,
            Description = "Piso térreo, entrada principal"
        };
        var zoneB = new ParkingZone
        {
            Name = "Estacionamento B — Funcionários",
            TotalSpots = 40,
            HourlyRate = 4m,
            Description = "Subsolo bloco administrativo"
        };
        dbContext.ParkingZones.AddRange(zoneA, zoneB);

        dbContext.ParkingSessions.Add(new ParkingSession
        {
            ParkingZone = zoneA,
            VehiclePlate = "ABC1D23",
            EnteredAt = DateTime.UtcNow.AddHours(-2)
        });

        var utiBed = await dbContext.Beds
            .Include(b => b.Ward)
            .FirstOrDefaultAsync(b => b.IsActive && b.Status == BedStatus.Available
                && b.Ward.Category == WardCategory.Uti
                && (b.Ward.CoverageModality == WardCoverageModality.Convenio
                    || b.Ward.CoverageModality == WardCoverageModality.Mixed)
                && EF.Functions.ILike(b.Ward.Name, "%UTI%"), cancellationToken)
            ?? await dbContext.Beds
                .Include(b => b.Ward)
                .FirstOrDefaultAsync(b => b.IsActive && b.Status == BedStatus.Available
                    && EF.Functions.ILike(b.Ward.Name, "%UTI%"), cancellationToken);

        var professional = await dbContext.Professionals.FirstAsync(cancellationToken);
        var patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Cpf == "52998224725", cancellationToken)
            ?? await dbContext.Patients.FirstAsync(cancellationToken);

        Hospitalization? icuStay = null;

        if (utiBed is not null)
        {
            utiBed.Status = BedStatus.Occupied;
            icuStay = new Hospitalization
            {
                PatientId = patient.Id,
                BedId = utiBed.Id,
                ProfessionalId = professional.Id,
                Reason = "Insuficiência respiratória",
                Diagnosis = "Pneumonia complicada",
                Status = HospitalizationStatus.Active
            };
            dbContext.Hospitalizations.Add(icuStay);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.VitalSignRecords.Add(new VitalSignRecord
            {
                HospitalizationId = icuStay.Id,
                HeartRate = 112,
                SystolicBp = 145,
                DiastolicBp = 88,
                SpO2 = 91,
                Temperature = 38.2m,
                RespiratoryRate = 24,
                RecordedByProfessionalId = professional.Id,
                Notes = "Monitoramento contínuo — alerta SpO2"
            });

            dbContext.DietOrders.Add(new DietOrder
            {
                HospitalizationId = icuStay.Id,
                DietType = DietType.Liquid,
                MealPeriod = MealPeriod.Lunch,
                MealDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = DietOrderStatus.InPreparation,
                Notes = "Restrição hídica conforme prescrição"
            });
        }
    }

    private static async Task SeedPhase7Async(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var clinicaGeral = await dbContext.Specialties.FirstAsync(s => s.Name == "Clínica Geral", cancellationToken);
        var cardiologia = await dbContext.Specialties.FirstAsync(s => s.Name == "Cardiologia", cancellationToken);
        var ana = await dbContext.Professionals.FirstAsync(p => p.Email == "ana.silva@hospital.local", cancellationToken);
        var carlos = await dbContext.Professionals.FirstAsync(p => p.Email == "carlos.mendes@hospital.local", cancellationToken);

        var room101 = new ConsultingRoom { Name = "Consultório 101", Floor = "1", Building = "Ambulatório", Specialty = clinicaGeral };
        var room102 = new ConsultingRoom { Name = "Consultório 102", Floor = "1", Building = "Ambulatório", Specialty = cardiologia };
        var room201 = new ConsultingRoom { Name = "Consultório 201", Floor = "2", Building = "Ambulatório", Specialty = clinicaGeral };
        dbContext.ConsultingRooms.AddRange(room101, room102, room201);

        dbContext.ConsultingRoomSchedules.AddRange(
            new ConsultingRoomSchedule { ConsultingRoom = room101, Professional = ana, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0) },
            new ConsultingRoomSchedule { ConsultingRoom = room102, Professional = carlos, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(18, 0) });

        dbContext.HospitalityRooms.AddRange(
            new HospitalityRoom { RoomNumber = "H101", Floor = "1", Capacity = 2, DailyRate = 120m },
            new HospitalityRoom { RoomNumber = "H102", Floor = "1", Capacity = 3, DailyRate = 150m },
            new HospitalityRoom { RoomNumber = "H201", Floor = "2", Capacity = 2, DailyRate = 120m });

        var ventilator = new MedicalEquipment { Name = "Ventilador pulmonar", AssetTag = "EQ-VENT-001", Manufacturer = "Dräger", Model = "Evita V500", Location = "UTI", Status = MedicalEquipmentStatus.Maintenance };

        dbContext.MedicalEquipments.AddRange(
            new MedicalEquipment { Name = "Monitor multiparamétrico", AssetTag = "EQ-MON-001", Manufacturer = "Philips", Model = "IntelliVue", Location = "UTI", NextMaintenanceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)) },
            new MedicalEquipment { Name = "Desfibrilador", AssetTag = "EQ-DEF-001", Manufacturer = "Zoll", Model = "R Series", Location = "Emergência" },
            ventilator);

        dbContext.MaintenanceWorkOrders.Add(new MaintenanceWorkOrder
        {
            MedicalEquipment = ventilator,
            Title = "Calibração preventiva",
            Description = "Verificação de sensores e válvulas",
            TechnicianName = "Eng. Roberto Lima"
        });

        dbContext.SecurityIncidents.Add(new SecurityIncident
        {
            Type = SecurityIncidentType.VisitorIssue,
            Location = "Recepção — Portaria principal",
            Description = "Visitante sem identificação tentou acesso à UTI",
            ReportedBy = "Segurança — Turno manhã"
        });

        var patient = await dbContext.Patients.FirstOrDefaultAsync(p => p.Cpf == "52998224725", cancellationToken);
        dbContext.VisitorLogs.Add(new VisitorLog
        {
            VisitorName = "Carlos Santos",
            DocumentNumber = "12345678901",
            Patient = patient,
            Destination = "UTI — Enfermaria",
            BadgeNumber = "V-0042"
        });
    }

    private static async Task SeedPhase8Async(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var kitCirurgico = new InstrumentKit
        {
            Name = "Kit cirúrgico básico",
            Code = "KIT-CIR-001",
            Description = "Bisturi, pinças, tesoura, afastador",
            Status = InstrumentKitStatus.Sterile,
            SterilityExpiration = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))
        };
        var kitOrtopedia = new InstrumentKit
        {
            Name = "Kit ortopedia",
            Code = "KIT-ORT-001",
            Description = "Instrumentos para osteossíntese",
            Status = InstrumentKitStatus.Available
        };
        dbContext.InstrumentKits.AddRange(kitCirurgico, kitOrtopedia);

        dbContext.SterilizationCycles.Add(new SterilizationCycle
        {
            InstrumentKit = kitOrtopedia,
            Method = SterilizationMethod.Steam,
            Status = SterilizationCycleStatus.Pending,
            SterilizerName = "Autoclave CME-01",
            OperatorName = "Téc. Fernanda Alves"
        });

        dbContext.BloodUnits.AddRange(
            new BloodUnit
            {
                UnitCode = "HEMO-001",
                BloodType = BloodType.OPositive,
                Component = BloodComponent.PackedRedCells,
                VolumeMl = 300,
                CollectedAt = DateTime.UtcNow.AddDays(-2),
                ExpiresAt = DateTime.UtcNow.AddDays(40)
            },
            new BloodUnit
            {
                UnitCode = "HEMO-002",
                BloodType = BloodType.ANegative,
                Component = BloodComponent.Platelets,
                VolumeMl = 250,
                CollectedAt = DateTime.UtcNow.AddDays(-1),
                ExpiresAt = DateTime.UtcNow.AddDays(4)
            });

        var patient = await dbContext.Patients.FirstOrDefaultAsync(p => p.Cpf == "52998224725", cancellationToken)
            ?? await dbContext.Patients.FirstAsync(cancellationToken);
        var professional = await dbContext.Professionals.FirstAsync(cancellationToken);
        var hospitalization = await dbContext.Hospitalizations
            .FirstOrDefaultAsync(h => h.PatientId == patient.Id && h.Status == HospitalizationStatus.Active, cancellationToken);

        dbContext.TransfusionRequests.Add(new TransfusionRequest
        {
            Patient = patient,
            Hospitalization = hospitalization,
            RequestingProfessional = professional,
            BloodTypeRequired = BloodType.OPositive,
            Component = BloodComponent.PackedRedCells,
            UnitsRequested = 1,
            Notes = "Anemia pós-operatória — hemoglobina 7.2"
        });

        dbContext.DialysisSessions.Add(new DialysisSession
        {
            Patient = patient,
            Hospitalization = hospitalization,
            MachineNumber = "DIA-03",
            ScheduledAt = DateTime.UtcNow.AddHours(4),
            DryWeightKg = 72.5m,
            NurseName = "Enf. Juliana Costa",
            Notes = "Primeira sessão do dia — acesso FAV esquerdo"
        });

        dbContext.LaundryBatches.AddRange(
            new LaundryBatch
            {
                BatchNumber = "LAV-20260608-1001",
                Origin = LaundryOrigin.Icu,
                OriginDetail = "UTI — Ala A",
                ItemCount = 45,
                WeightKg = 18.5m,
                Status = LaundryBatchStatus.Washing
            },
            new LaundryBatch
            {
                BatchNumber = "LAV-20260608-1002",
                Origin = LaundryOrigin.Surgery,
                OriginDetail = "Centro Cirúrgico — Sala 2",
                ItemCount = 30,
                WeightKg = 12m,
                Status = LaundryBatchStatus.Collected
            });
    }

    private static async Task SeedPhase9Async(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var patient = await dbContext.Patients.FirstOrDefaultAsync(p => p.Cpf == "52998224725", cancellationToken)
            ?? await dbContext.Patients.FirstAsync(cancellationToken);
        var professional = await dbContext.Professionals.FirstAsync(cancellationToken);
        var hospitalization = await dbContext.Hospitalizations
            .FirstOrDefaultAsync(h => h.PatientId == patient.Id && h.Status == HospitalizationStatus.Active, cancellationToken);

        dbContext.ChemotherapySessions.Add(new ChemotherapySession
        {
            Patient = patient,
            Professional = professional,
            Hospitalization = hospitalization,
            ProtocolName = "AC-T (Doxorrubicina + Ciclofosfamida → Paclitaxel)",
            DrugRegimen = "Doxorrubicina 60 mg/m² + Ciclofosfamida 600 mg/m²",
            CycleNumber = 2,
            TotalCycles = 4,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Notes = "Pré-medicação com antiemético conforme protocolo"
        });

        dbContext.PhysiotherapySessions.Add(new PhysiotherapySession
        {
            Patient = patient,
            Hospitalization = hospitalization,
            TherapistName = "Fisio. Ricardo Mendes",
            SessionType = PhysiotherapySessionType.Respiratory,
            ScheduledAt = DateTime.UtcNow.AddHours(2),
            DurationMinutes = 45,
            Goals = "Expansão pulmonar e fortalecimento diafragmático",
            Notes = "Paciente pós-pneumonia — SpO₂ em recuperação"
        });

        dbContext.TelemedicineAppointments.Add(new TelemedicineAppointment
        {
            Patient = patient,
            Professional = professional,
            ScheduledAt = DateTime.UtcNow.AddDays(2),
            ChiefComplaint = "Retorno oncológico — revisão de exames",
            MeetingUrl = "https://meet.google.com/demo-oncolo-gia",
            Notes = "Paciente em domicílio — conexão testada"
        });

        dbContext.InfectionSurveillances.Add(new InfectionSurveillance
        {
            Patient = patient,
            Hospitalization = hospitalization,
            Location = "UTI — Ala A",
            InfectionType = InfectionType.Respiratory,
            Organism = "Klebsiella pneumoniae",
            Site = "Secreção traqueal",
            Status = InfectionSurveillanceStatus.Confirmed,
            ReportedBy = "CCIH — Enf. Epidemiologia",
            Notes = "Cultura positiva — antibiograma pendente"
        });

        dbContext.IsolationPrecautions.Add(new IsolationPrecaution
        {
            Patient = patient,
            Hospitalization = hospitalization,
            PrecautionType = IsolationPrecautionType.Contact,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            Reason = "Klebsiella multirresistente — precaução de contato"
        });
    }

    private static async Task BackfillPatientFieldEncryptionAsync(
        IServiceProvider services, CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var encryption = scope.ServiceProvider.GetRequiredService<IFieldEncryptionService>();

        var patients = await dbContext.Patients
            .Where(p => p.CpfHash == null || !p.Cpf.StartsWith("ENC1:"))
            .ToListAsync(cancellationToken);

        if (patients.Count == 0)
        {
            return;
        }

        foreach (var patient in patients)
        {
            var cpf = new string(patient.Cpf.Where(char.IsDigit).ToArray());
            if (cpf.Length != 11 && encryption.IsEncrypted(patient.Cpf))
            {
                cpf = new string(encryption.Decrypt(patient.Cpf).Where(char.IsDigit).ToArray());
            }

            if (cpf.Length == 11)
            {
                PatientFieldProtection.Protect(patient, encryption, cpf);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

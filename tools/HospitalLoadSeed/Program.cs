using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Infrastructure;
using SistemaHospitalar.Infrastructure.Persistence;

static void PrintHelp()
{
    Console.WriteLine("""
GTH — Seeder de massa de dados para testes de carga

USO:
  dotnet run --project tools/HospitalLoadSeed -- [opções]

OPÇÕES:
  --patients <n>           Pacientes fictícios (padrão: 500)
  --visits <n>             Visitas PS por paciente (padrão: 3)
  --exams <n>              Pedidos lab por paciente (padrão: 4)
  --appointments <n>       Agendamentos por paciente — legado, usa como máximo (padrão: 1)
  --appointments-min <n>   Mínimo de agendamentos por paciente (padrão: 1)
  --appointments-max <n>   Máximo de agendamentos por paciente (padrão: 5)
  --pep <n>                Registros PEP por paciente (padrão: 2)
  --hosp-rate <0-1>        Taxa de internação (padrão: 0.08)
  --batch <n>              Tamanho do lote (padrão: 250)
  --seed <n>               Semente aleatória (padrão: 2026)
  --simulation-days <n>    Dias retroativos da simulação (padrão: 30)
  --no-smart-scheduling    Desativa AppointmentSchedulingEngine na carga
  --simulation             Executa HospitalSimulationSeeder após a carga
  --simulation-only        Apenas simulação (sem gerar novos pacientes)
  --clear                  Remove dados gerados anteriormente (mesmo marcador)
  --migrate-only           Apenas aplica migrations
  --help                   Esta ajuda

SEGURANÇA (obrigatório):
  $env:GTH_ALLOW_LOAD_SEED = "true"
  Use ConnectionStrings__DefaultConnection apontando para banco de TESTE.

EXEMPLO:
  $env:GTH_ALLOW_LOAD_SEED = "true"
  $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=sistema_hospitalar_test;Username=postgres;Password=postgres"
  dotnet run --project tools/HospitalLoadSeed -- --patients 1000 --simulation
""");
}

static string FindApiConfigDirectory()
{
    var dir = AppContext.BaseDirectory;
    for (var i = 0; i < 10; i++)
    {
        var candidate = Path.Combine(dir, "src", "SistemaHospitalar.Api");
        if (Directory.Exists(candidate))
        {
            return Path.GetFullPath(candidate);
        }

        var parent = Directory.GetParent(dir);
        if (parent is null)
        {
            break;
        }

        dir = parent.FullName;
    }

    throw new DirectoryNotFoundException("Não foi possível localizar src/SistemaHospitalar.Api (appsettings).");
}

if (args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp();
    return 0;
}

var configPath = FindApiConfigDirectory();
var configuration = new ConfigurationBuilder()
    .SetBasePath(configPath)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var options = new HospitalLoadDataOptions();
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--patients" when i + 1 < args.Length:
            options.Patients = int.Parse(args[++i]);
            break;
        case "--visits" when i + 1 < args.Length:
            options.VisitsPerPatient = int.Parse(args[++i]);
            break;
        case "--exams" when i + 1 < args.Length:
            options.ExamsPerPatient = int.Parse(args[++i]);
            break;
        case "--appointments" when i + 1 < args.Length:
            options.AppointmentsPerPatient = int.Parse(args[++i]);
            options.AppointmentsPerPatientMax = options.AppointmentsPerPatient;
            break;
        case "--appointments-min" when i + 1 < args.Length:
            options.AppointmentsPerPatientMin = int.Parse(args[++i]);
            break;
        case "--appointments-max" when i + 1 < args.Length:
            options.AppointmentsPerPatientMax = int.Parse(args[++i]);
            break;
        case "--pep" when i + 1 < args.Length:
            options.PepEntriesPerPatient = int.Parse(args[++i]);
            break;
        case "--hosp-rate" when i + 1 < args.Length:
            options.HospitalizationRate = double.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
            break;
        case "--batch" when i + 1 < args.Length:
            options.BatchSize = int.Parse(args[++i]);
            break;
        case "--seed" when i + 1 < args.Length:
            options.RandomSeed = int.Parse(args[++i]);
            break;
        case "--simulation-days" when i + 1 < args.Length:
            options.SimulationDays = int.Parse(args[++i]);
            break;
        case "--clear":
            options.ClearExisting = true;
            break;
        case "--no-smart-scheduling":
            options.UseSmartScheduling = false;
            break;
        case "--simulation":
            options.RunSimulation = true;
            break;
        case "--skip-base-seed":
            options.SkipMigrate = true;
            break;
    }
}

var simulationOnly = args.Contains("--simulation-only");
if (simulationOnly)
{
    options.RunSimulation = true;
    options.Patients = 0;
}

if (!HospitalLoadDataSeeder.IsLoadSeedAllowed())
{
    Console.Error.WriteLine("ERRO: defina GTH_ALLOW_LOAD_SEED=true e use um banco de TESTE.");
    PrintHelp();
    return 1;
}

var connection = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connection))
{
    Console.Error.WriteLine("ERRO: ConnectionStrings:DefaultConnection não configurada.");
    return 1;
}

Console.WriteLine("⚠️  Dados FICTÍCIOS — banco: {0}", connection.Split(';').FirstOrDefault(s => s.StartsWith("Database=", StringComparison.OrdinalIgnoreCase)) ?? "(ver connection string)");
Console.WriteLine();

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddInfrastructure(configuration);

await using var provider = services.BuildServiceProvider();
var db = provider.GetRequiredService<AppDbContext>();
var encryption = provider.GetRequiredService<SistemaHospitalar.Application.Interfaces.IFieldEncryptionService>();
var financialAccounts = provider.GetRequiredService<SistemaHospitalar.Application.Interfaces.IFinancialAccountService>();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("HospitalLoadSeed");

if (args.Contains("--migrate-only"))
{
    await db.Database.MigrateAsync();
    Console.WriteLine("Migrations aplicadas.");
    return 0;
}

if (!args.Contains("--skip-base-seed") && !simulationOnly)
{
    await DatabaseInitializer.InitializeAsync(provider);
}

if (options.Patients > 0)
{
    var result = await HospitalLoadDataSeeder.RunAsync(db, encryption, options, logger);

    Console.WriteLine();
    Console.WriteLine("Carga concluída em {0:F1}s", result.Elapsed.TotalSeconds);
    Console.WriteLine("  Pacientes:     {0}", result.PatientsCreated);
    Console.WriteLine("  Convênios:     {0}", result.PatientInsurancesCreated);
    Console.WriteLine("  Visitas PS:    {0}", result.VisitsCreated);
    Console.WriteLine("  Agendamentos:  {0}", result.AppointmentsCreated);
    Console.WriteLine("  Pedidos lab:   {0} ({1} itens)", result.LabOrdersCreated, result.LabOrderItemsCreated);
    Console.WriteLine("  Registros PEP: {0}", result.PepEntriesCreated);
    Console.WriteLine("  Internações:   {0}", result.HospitalizationsCreated);
    Console.WriteLine("  Sala espera:   {0}", result.WaitingRoomAppointmentsCreated);
    Console.WriteLine("  PS hoje:       {0}", result.TodayEmergencyVisitsCreated);
}

if (options.RunSimulation || simulationOnly)
{
    var simulation = await HospitalSimulationSeeder.RunAsync(db, financialAccounts, options, logger);

    Console.WriteLine();
    Console.WriteLine("Simulação concluída em {0:F1}s", simulation.Elapsed.TotalSeconds);
    Console.WriteLine("  Turnos:        {0}", simulation.EmployeeShiftsCreated);
    Console.WriteLine("  Contas fin.:   {0}", simulation.FinancialAccountsCreated);
    Console.WriteLine("  Propostas:     {0}", simulation.ProposalsCreated);
    Console.WriteLine("  Honorários:    {0}", simulation.HonorariosCreated);
    Console.WriteLine("  Pagamentos:    {0}", simulation.FinancialPaymentsCreated);
    Console.WriteLine("  Contas pagar:  {0}", simulation.PayablesCreated);
    Console.WriteLine("  Produtos:      {0} (total {1})", simulation.ProductsCreated, simulation.ProductsTotal);
    Console.WriteLine("  Estoque mov.:  {0} (total {1})", simulation.StockMovementsCreated, simulation.StockMovementsTotal);
    Console.WriteLine("  Regras CMED:   {0}", simulation.ProductBillingRulesCreated);
    Console.WriteLine("  Dispensações:  {0} / faturas {1}", simulation.PharmacyDispensingsCreated, simulation.PharmacyBillingEntriesCreated);
    Console.WriteLine("  Caixas:        {0} ({1} aberta)", simulation.CashSessionsCreated, simulation.OpenCashSessionsCount);
    Console.WriteLine("  Recibos div.:  {0}", simulation.MiscellaneousReceiptsCreated);
    Console.WriteLine("  Folha:         {0} competências", simulation.PayrollRunsCreated);
    Console.WriteLine("  TPA:           {0}", simulation.TpaClaimsCreated);
    Console.WriteLine("  Guias TISS:    {0}", simulation.TissGuidesCreated);
    Console.WriteLine("  Lotes TISS:    {0}", simulation.TissBatchesCreated);
    Console.WriteLine("  Audit confl.:  {0}", simulation.AuditLogsCreated);
    Console.WriteLine();
    Console.WriteLine("Validação:");
    Console.WriteLine("  Conflitos:     {0}", simulation.ScheduleConflicts);
    Console.WriteLine("  Sala espera:   {0}", simulation.WaitingRoomTodayCount);
    Console.WriteLine("  Contas total:  {0}", simulation.FinancialAccountsTotal);
    Console.WriteLine("  Propostas ab.: {0}", simulation.OpenProposalsCount);
}

return 0;

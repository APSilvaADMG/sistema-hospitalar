namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>Parâmetros para geração de massa de dados fictícia (somente ambiente de teste).</summary>
public sealed class HospitalLoadDataOptions
{
    public const string MarkerPrefix = "gth-load-seed-v1";

    public int Patients { get; set; } = 500;
    public int VisitsPerPatient { get; set; } = 3;
    public int ExamsPerPatient { get; set; } = 4;
    public int AppointmentsPerPatient { get; set; } = 1;
    public int AppointmentsPerPatientMin { get; set; } = 1;
    public int AppointmentsPerPatientMax { get; set; } = 5;
    public int PepEntriesPerPatient { get; set; } = 2;
    /// <summary>0.0–1.0 — fração de pacientes com internação ativa (se houver leito livre).</summary>
    public double HospitalizationRate { get; set; } = 0.08;
    public int BatchSize { get; set; } = 250;
    public bool ClearExisting { get; set; }
    public bool SkipMigrate { get; set; }
    public int RandomSeed { get; set; } = 2026;
    /// <summary>Dias retroativos simulados (turnos, contas financeiras).</summary>
    public int SimulationDays { get; set; } = 30;
    /// <summary>Usa AppointmentSchedulingEngine para evitar conflitos de agenda.</summary>
    public bool UseSmartScheduling { get; set; } = true;
    /// <summary>Executa HospitalSimulationSeeder após carga de pacientes.</summary>
    public bool RunSimulation { get; set; }
}

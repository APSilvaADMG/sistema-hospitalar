using SistemaHospitalar.Infrastructure.TvSignage;
using Xunit;

namespace SistemaHospitalar.Tests;

public class TvCallSpeechTests
{
    [Fact]
    public void FormatDisplayDestination_Empty_ReturnsDefaultLabel()
    {
        Assert.Equal("Consultório indicado", TvCallSpeech.FormatDisplayDestination(null));
        Assert.Equal("Consultório indicado", TvCallSpeech.FormatDisplayDestination("   "));
    }

    [Fact]
    public void FormatDisplayDestination_RoomNumber_PrefixesConsultorio()
    {
        Assert.Equal("Consultório 12", TvCallSpeech.FormatDisplayDestination("12"));
    }

    [Fact]
    public void FormatDisplayDestination_Guiche_KeepsOriginal()
    {
        Assert.Equal("Guichê 3", TvCallSpeech.FormatDisplayDestination("Guichê 3"));
    }

    [Fact]
    public void FormatPatientCall_IncludesPatientAndDestination()
    {
        var speech = TvCallSpeech.FormatPatientCall("Maria Silva", "Sala 5");

        Assert.Contains("Maria Silva", speech);
        Assert.Contains("consultório", speech, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".", speech);
    }

    [Fact]
    public void FormatDisplayDestination_Sala_KeepsOriginal()
    {
        Assert.Equal("Sala 5", TvCallSpeech.FormatDisplayDestination("Sala 5"));
    }

    [Fact]
    public void FormatPatientCall_Sala_ConvertsToConsultorioInSpeech()
    {
        var speech = TvCallSpeech.FormatPatientCall("Pedro", "Sala 8");

        Assert.Contains("consultório 8", speech, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatDisplayDestination_ConsultorioPrefix_KeepsOriginal()
    {
        Assert.Equal("Consultório 7", TvCallSpeech.FormatDisplayDestination("Consultório 7"));
    }
}

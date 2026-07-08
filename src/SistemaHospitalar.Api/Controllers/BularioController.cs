using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/bulario")]
public class BularioController(
    IClinicalCatalogService clinicalCatalogService,
    IBularioService bularioService) : ControllerBase
{
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? nome,
        [FromQuery] int pagina = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await clinicalCatalogService.SearchBularioAsync(nome, pagina, pageSize, cancellationToken));

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken cancellationToken = default)
        => Ok(await clinicalCatalogService.GetBularioStatsAsync(cancellationToken));

    [HttpGet("pesquisar")]
    public async Task<IActionResult> SearchAnvisaLegacy(
        [FromQuery] string nome,
        [FromQuery] int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await bularioService.SearchAsync(nome, pagina, cancellationToken);
        return result is null
            ? StatusCode(503, new { message = "Consulta ANVISA indisponível (bloqueio externo)." })
            : Ok(JsonSerializer.Deserialize<object>(result.RootElement.GetRawText()));
    }

    [HttpGet("medicamento/{numProcesso}")]
    public async Task<IActionResult> GetMedication(string numProcesso, CancellationToken cancellationToken = default)
    {
        var result = await bularioService.GetMedicationAsync(numProcesso, cancellationToken);
        return result is null ? NotFound() : Ok(JsonSerializer.Deserialize<object>(result.RootElement.GetRawText()));
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> GetPdf([FromQuery] string id, CancellationToken cancellationToken = default)
    {
        var pdf = await bularioService.GetPdfAsync(id, cancellationToken);
        return pdf is null ? NotFound() : File(pdf, "application/pdf", "bula.pdf");
    }

    [HttpGet("bula")]
    public async Task<IActionResult> GetPdfLink([FromQuery] string id, CancellationToken cancellationToken = default)
    {
        var url = await bularioService.GetPdfUrlAsync(id, cancellationToken);
        return url is null ? NotFound() : Ok(new { pdf = url });
    }
}

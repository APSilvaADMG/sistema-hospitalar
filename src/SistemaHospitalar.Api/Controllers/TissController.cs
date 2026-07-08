using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Infrastructure.Tiss;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/tiss")]
public class TissController(
    ITissBillingService tissBillingService,
    IInsuranceIntegrationService insuranceIntegrationService,
    ITissClinicalSourceService tissClinicalSourceService,
    ITissExtendedService tissExtendedService) : ControllerBase
{
    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("guide-types")]
    public IActionResult GetGuideTypes()
        => Ok(TissGuideCatalog.All);

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("guides")]
    public async Task<IActionResult> GetGuides(
        [FromQuery] TissGuideStatus? status,
        [FromQuery] Guid? patientId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
        => Ok(await tissBillingService.GetGuidesAsync(status, patientId, search, cancellationToken));

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("guides/{id:guid}")]
    public async Task<IActionResult> GetGuide(Guid id, CancellationToken cancellationToken)
    {
        var guide = await tissBillingService.GetGuideByIdAsync(id, cancellationToken);
        return guide is null ? NotFound() : Ok(guide);
    }

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpPost("guides")]
    public async Task<IActionResult> CreateGuide([FromBody] CreateTissGuideRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await tissBillingService.CreateGuideAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetGuide), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPut("guides/{id:guid}")]
    public async Task<IActionResult> UpdateGuide(
        Guid id, [FromBody] UpdateTissGuideRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await tissBillingService.UpdateGuideAsync(id, request, cancellationToken);
            return result is null ? BadRequest(new { message = "Guia não encontrada ou não está em rascunho." }) : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpDelete("guides/{id:guid}")]
    public async Task<IActionResult> DeleteGuide(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await tissBillingService.DeleteGuideAsync(id, cancellationToken);
        return deleted ? NoContent() : BadRequest(new { message = "Somente guias em rascunho podem ser excluídas." });
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("guides/{id:guid}/close-account")]
    public async Task<IActionResult> CloseGuideAccount(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await tissBillingService.CloseGuideAccountAsync(id, cancellationToken);
            return result is null ? BadRequest(new { message = "Guia não encontrada ou não está em rascunho." }) : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("guides/{id:guid}/send")]
    public async Task<IActionResult> SendGuide(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await tissBillingService.SendGuideAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("guides/{id:guid}/cancel")]
    public async Task<IActionResult> CancelGuide(Guid id, CancellationToken cancellationToken)
    {
        var result = await tissBillingService.CancelGuideAsync(id, cancellationToken);
        return result is null ? BadRequest(new { message = "Guia não pode ser cancelada." }) : Ok(result);
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("guides/{id:guid}/mark-paid")]
    public async Task<IActionResult> MarkGuidePaid(Guid id, CancellationToken cancellationToken)
    {
        var result = await tissBillingService.MarkGuidePaidAsync(id, cancellationToken);
        return result is null ? BadRequest(new { message = "Guia não pode ser marcada como paga." }) : Ok(result);
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("guides/{id:guid}/glosas")]
    public async Task<IActionResult> RegisterGlosa(
        Guid id, [FromBody] RegisterGlosaRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await tissBillingService.RegisterGlosaAsync(id, request, cancellationToken);
            return result is null ? BadRequest(new { message = "Não foi possível registrar a glosa." }) : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPut("glosas/{glosaId:guid}")]
    public async Task<IActionResult> UpdateGlosa(
        Guid glosaId, [FromBody] UpdateGlosaRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await tissBillingService.UpdateGlosaAsync(glosaId, request, cancellationToken);
            return result is null ? BadRequest(new { message = "Glosa não encontrada ou já resolvida." }) : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpDelete("glosas/{glosaId:guid}")]
    public async Task<IActionResult> DeleteGlosa(Guid glosaId, CancellationToken cancellationToken)
    {
        var deleted = await tissBillingService.DeleteGlosaAsync(glosaId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("glosas/{glosaId:guid}/resolve")]
    public async Task<IActionResult> ResolveGlosa(Guid glosaId, CancellationToken cancellationToken)
    {
        var result = await tissBillingService.ResolveGlosaAsync(glosaId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("glosas/{glosaId:guid}/contest")]
    public async Task<IActionResult> ContestGlosa(
        Guid glosaId,
        [FromBody] ContestGlosaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await tissBillingService.ContestGlosaAsync(glosaId, request, cancellationToken);
            return result is null ? BadRequest(new { message = "Glosa não encontrada ou já resolvida." }) : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("tuss-search")]
    public async Task<IActionResult> SearchTuss([FromQuery] string? q, CancellationToken cancellationToken)
        => Ok(await insuranceIntegrationService.SearchTussAsync(q, cancellationToken));

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpPost("suggested-items")]
    public async Task<IActionResult> BuildSuggestedItems(
        [FromBody] SuggestedGuideItemsRequest request,
        CancellationToken cancellationToken)
        => Ok(await insuranceIntegrationService.BuildSuggestedItemsAsync(request, cancellationToken));

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpPost("guide-prefill")]
    public async Task<IActionResult> GetGuidePrefill(
        [FromBody] GuidePrefillRequest request,
        CancellationToken cancellationToken)
        => Ok(await insuranceIntegrationService.GetGuidePrefillAsync(request, cancellationToken));

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("clinical-sources")]
    public async Task<IActionResult> GetClinicalSources(
        [FromQuery] Guid? patientId,
        [FromQuery] ClinicalDocumentKind? documentKind,
        [FromQuery] TissGuideType? guideType,
        [FromQuery] string? reportCode,
        [FromQuery] bool pendingOnly = false,
        CancellationToken cancellationToken = default)
        => Ok(await tissClinicalSourceService.GetSourcesAsync(
            patientId, documentKind, guideType, reportCode, pendingOnly, cancellationToken));

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("clinical-sources/{id:guid}")]
    public async Task<IActionResult> GetClinicalSource(Guid id, CancellationToken cancellationToken)
    {
        var source = await tissClinicalSourceService.GetSourceByIdAsync(id, cancellationToken);
        return source is null ? NotFound() : Ok(source);
    }

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpPost("clinical-sources/lookup")]
    public async Task<IActionResult> LookupClinicalSource(
        [FromBody] ClinicalSourceLookupRequest request,
        CancellationToken cancellationToken)
    {
        var source = await tissClinicalSourceService.FindSourceAsync(request, cancellationToken);
        return source is null ? NotFound() : Ok(source);
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("clinical-sources")]
    public async Task<IActionResult> UpsertClinicalSource(
        [FromBody] UpsertTissClinicalSourceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await tissClinicalSourceService.UpsertSourceAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("clinical-sources/{id:guid}/link-guide")]
    public async Task<IActionResult> LinkClinicalSourceGuide(
        Guid id,
        [FromBody] LinkClinicalSourceGuideRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var source = await tissClinicalSourceService.LinkGeneratedGuideAsync(id, request, cancellationToken);
            return source is null ? NotFound() : Ok(source);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("clinical-sources/{id:guid}/link-artifact")]
    public async Task<IActionResult> LinkClinicalSourceArtifact(
        Guid id,
        [FromBody] LinkClinicalSourceArtifactRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var source = await tissClinicalSourceService.LinkGeneratedArtifactAsync(id, request, cancellationToken);
            return source is null ? NotFound() : Ok(source);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("procedure-lookup")]
    public async Task<IActionResult> LookupProcedure([FromQuery] string? q, CancellationToken cancellationToken)
        => Ok(await insuranceIntegrationService.LookupProcedureAsync(q, cancellationToken));

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("billing-catalog-summary")]
    public async Task<IActionResult> GetBillingCatalogSummary(CancellationToken cancellationToken)
        => Ok(await insuranceIntegrationService.GetBillingCatalogSummaryAsync(cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("eligibility")]
    public async Task<IActionResult> CheckEligibility(
        [FromBody] EligibilityCheckRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await insuranceIntegrationService.CheckEligibilityAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("eligibility")]
    public async Task<IActionResult> GetEligibilityHistory(
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
        => Ok(await insuranceIntegrationService.GetEligibilityHistoryAsync(patientId, cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("authorizations")]
    public async Task<IActionResult> GetAuthorizations(
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? healthInsuranceId,
        CancellationToken cancellationToken)
        => Ok(await insuranceIntegrationService.GetAuthorizationsAsync(patientId, healthInsuranceId, cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("authorizations")]
    public async Task<IActionResult> CreateAuthorization(
        [FromBody] CreateAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await insuranceIntegrationService.CreateAuthorizationAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("authorizations/request-online")]
    public async Task<IActionResult> RequestOnlineAuthorization(
        [FromBody] RequestOnlineAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await insuranceIntegrationService.RequestOnlineAuthorizationAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPut("authorizations/{id:guid}")]
    public async Task<IActionResult> UpdateAuthorization(
        Guid id,
        [FromBody] UpdateAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await insuranceIntegrationService.UpdateAuthorizationAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches(CancellationToken cancellationToken)
        => Ok(await insuranceIntegrationService.GetBatchesAsync(cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("batches/{id:guid}")]
    public async Task<IActionResult> GetBatch(Guid id, CancellationToken cancellationToken)
    {
        var batch = await insuranceIntegrationService.GetBatchByIdAsync(id, cancellationToken);
        return batch is null ? NotFound() : Ok(batch);
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("batches")]
    public async Task<IActionResult> CreateBatch(
        [FromBody] CreateTissBatchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await insuranceIntegrationService.CreateBatchAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("batches/{id:guid}/send")]
    public async Task<IActionResult> SendBatch(Guid id, CancellationToken cancellationToken)
    {
        var result = await insuranceIntegrationService.MarkBatchSentAsync(id, cancellationToken);
        return result is null ? BadRequest(new { message = "Lote não pode ser enviado." }) : Ok(result);
    }

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpPost("batches/{id:guid}/validate")]
    public async Task<IActionResult> ValidateBatch(Guid id, CancellationToken cancellationToken)
    {
        var result = await insuranceIntegrationService.ValidateBatchXmlAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("batches/{id:guid}/xml")]
    public async Task<IActionResult> DownloadBatchXml(Guid id, CancellationToken cancellationToken)
    {
        var batch = await insuranceIntegrationService.GetBatchByIdAsync(id, cancellationToken);
        if (batch is null || string.IsNullOrWhiteSpace(batch.XmlContent))
            return NotFound();

        var bytes = System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(batch.XmlContent);
        return File(bytes, "application/xml", $"{batch.BatchNumber}.xml");
    }

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpPost("validate-xml")]
    public async Task<IActionResult> ValidateXml(
        [FromBody] ValidateTissXmlRequest request,
        CancellationToken cancellationToken)
        => Ok(await insuranceIntegrationService.ValidateXmlAsync(request.XmlContent, cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetConvenioDashboard(CancellationToken cancellationToken)
        => Ok(await insuranceIntegrationService.GetConvenioDashboardAsync(cancellationToken));

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("tuss-catalog")]
    public async Task<IActionResult> GetTussCatalog(
        [FromQuery] string? search,
        [FromQuery] TussTableType? tableType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await tissExtendedService.GetTussCatalogAsync(search, tableType, page, pageSize, cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("tuss-catalog/import")]
    public async Task<IActionResult> ImportTussCatalog([FromBody] ImportTussRequest request, CancellationToken cancellationToken)
        => Ok(new { imported = await tissExtendedService.ImportTussCatalogAsync(request, cancellationToken) });

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("tuss-catalog/import-csv")]
    public async Task<IActionResult> ImportTussCsv([FromBody] ImportTussCsvRequest request, CancellationToken cancellationToken)
        => Ok(await tissExtendedService.ImportTussCsvAsync(request, cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [RequestTimeout("LongRunningImport")]
    [HttpPost("tuss-catalog/import-xlsx")]
    [RequestSizeLimit(120_000_000)]
    public async Task<IActionResult> ImportTussXlsx(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest(new { message = "Envie um arquivo .xlsx do portal ANS/TUSS." });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Formato inválido. Use arquivo .xlsx exportado da ANS." });

        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await tissExtendedService.ImportTussXlsxFileAsync(stream, file.FileName, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [RequestTimeout("LongRunningImport")]
    [HttpPost("tuss-catalog/import-bundled-202601")]
    public async Task<IActionResult> ImportBundledTuss202601(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await tissExtendedService.ImportBundledTuss202601Async(cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("tuss-catalog/seed-expanded")]
    public async Task<IActionResult> SeedExpandedTussCatalog(CancellationToken cancellationToken)
        => Ok(await tissExtendedService.SeedExpandedTussCatalogAsync(cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("tuss-catalog/sample-csv")]
    public IActionResult GetTussSampleCsv()
        => Ok(new { csv = TissCatalogExpandedSeed.SampleCsvContent });

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("operator-profiles")]
    public IActionResult GetOperatorProfiles()
        => Ok(OperatorIntegrationProfiles.All.Select(p => new
        {
            p.OperatorCode,
            names = p.Names,
            p.AuthorizationDeadlineDays,
            p.RequiresOnlineAuthorization,
            p.BusinessRules,
            p.PortalUrl,
        }));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("sigtap")]
    public async Task<IActionResult> GetSigtap(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await tissExtendedService.GetSigtapProceduresAsync(search, page, pageSize, cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("sigtap/summary")]
    public async Task<IActionResult> GetSigtapSummary(CancellationToken cancellationToken = default)
        => Ok(await tissExtendedService.GetSigtapSummaryAsync(cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [RequestTimeout("LongRunningImport")]
    [HttpPost("sigtap/sync-official")]
    public async Task<IActionResult> SyncSigtapOfficial(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await tissExtendedService.SyncSigtapOfficialAsync(cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [RequestTimeout("LongRunningImport")]
    [HttpPost("sigtap/import-zip")]
    [RequestSizeLimit(120_000_000)]
    public async Task<IActionResult> ImportSigtapZip(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest(new { message = "Envie um arquivo .zip ou .txt da tabela SIGTAP." });

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            && !file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Formato inválido. Use arquivo .zip (preferencial) ou .txt SIGTAP." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await tissExtendedService.ImportSigtapZipAsync(stream, file.FileName, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("demonstrativos")]
    public async Task<IActionResult> GetDemonstrativos(CancellationToken cancellationToken)
        => Ok(await tissExtendedService.GetDemonstrativosAsync(cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("demonstrativos/{id:guid}")]
    public async Task<IActionResult> GetDemonstrativo(Guid id, CancellationToken cancellationToken)
    {
        var item = await tissExtendedService.GetDemonstrativoByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("demonstrativos/import")]
    public async Task<IActionResult> ImportDemonstrativo([FromBody] ImportDemonstrativoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await tissExtendedService.ImportDemonstrativoCsvAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("demonstrativos/{id:guid}/process")]
    public async Task<IActionResult> ProcessDemonstrativo(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await tissExtendedService.ProcessDemonstrativoAsync(id, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("demonstrativos/fetch-operator")]
    public async Task<IActionResult> FetchDemonstrativoFromOperator(
        [FromBody] FetchOperatorDemonstrativoRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await tissExtendedService.FetchDemonstrativoFromOperatorAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("guides/{guideId:guid}/annexes")]
    public async Task<IActionResult> GetGuideAnnexes(Guid guideId, CancellationToken cancellationToken)
        => Ok(await tissExtendedService.GetGuideAnnexesAsync(guideId, cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("annexes")]
    public async Task<IActionResult> CreateGuideAnnex([FromBody] CreateTissGuideAnnexRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await tissExtendedService.CreateGuideAnnexAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("integrations")]
    public async Task<IActionResult> GetIntegrations(CancellationToken cancellationToken)
        => Ok(await tissExtendedService.GetInsuranceIntegrationsAsync(cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPut("integrations/{id:guid}")]
    public async Task<IActionResult> UpdateIntegration(
        Guid id,
        [FromBody] UpdateHealthInsuranceIntegrationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await tissExtendedService.UpdateInsuranceIntegrationAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("operator-logs")]
    public async Task<IActionResult> GetOperatorLogs([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
        => Ok(await tissExtendedService.GetOperatorTransactionLogsAsync(limit, cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpGet("reconciliation")]
    public async Task<IActionResult> GetReconciliation(CancellationToken cancellationToken)
        => Ok(await tissExtendedService.GetReconciliationSummaryAsync(cancellationToken));
}

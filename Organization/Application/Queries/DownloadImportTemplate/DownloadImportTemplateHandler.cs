using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rezilio.Modules.Organization.Application.Services;
using Rezilio.Modules.Organization.Domain;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Queries.DownloadImportTemplate;

public sealed class DownloadImportTemplateHandler(IExcelTemplateGenerator generator)
{
    [WolverineGet("/api/organization/import/{entityType}/template")]
    [Authorize]
    public IResult Handle([FromRoute] EntityType entityType)
    {
        byte[] content = generator.GenerateTemplate(entityType);
        string fileName = $"import-template-{entityType.ToString().ToLower()}.xlsx";

        return Results.File(
            content,
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileDownloadName: fileName);
    }
}

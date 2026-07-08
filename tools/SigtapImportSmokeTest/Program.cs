using System.IO.Compression;
using SistemaHospitalar.Infrastructure.Tiss;

var zipPath = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "sample_sigtap.zip");

if (!File.Exists(zipPath))
{
    Console.Error.WriteLine($"ZIP not found: {Path.GetFullPath(zipPath)}");
    return 1;
}

await using var stream = File.OpenRead(zipPath);
var fileName = Path.GetFileName(zipPath);
var result = await SigtapZipImporter.ParseAsync(stream, fileName);

Console.WriteLine($"Competence: {result.Competence}");
Console.WriteLine($"Message: {result.Message}");
Console.WriteLine($"Items: {result.Items.Count}");
if (result.Items.Count > 0)
{
    var sample = result.Items.Take(3);
    foreach (var item in sample)
        Console.WriteLine($"  {item.Code} | {item.Competence} | {item.Description[..Math.Min(50, item.Description.Length)]} | SH={item.HospitalAmount} SA={item.ProfessionalAmount}");
}

return result.Items.Count > 0 ? 0 : 1;

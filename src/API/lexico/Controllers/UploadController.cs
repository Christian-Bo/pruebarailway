using Microsoft.AspNetCore.Mvc;
using Lexico.Application.Contracts;
using Lexico.Domain.Entities;

namespace Lexico.API.Controllers;

[ApiController]
[Route("api/upload")]
public class UploadController : ControllerBase
{
    private readonly IFileRepository _files;

    public UploadController(IFileRepository files) => _files = files;

    [HttpPost]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Upload([FromForm(Name="file")] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Debe enviar un archivo .txt válido.");

        if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Solo se permiten archivos .txt.");

        string contenido;
        using (var reader = new StreamReader(file.OpenReadStream()))
            contenido = await reader.ReadToEndAsync();

        var arch = new Archivo
        {
            Nombre = file.FileName,
            Contenido = contenido,
            FechaSubida = DateTime.UtcNow,
            UsuarioId = null // se integrará con autenticación luego
        };

        var id = await _files.InsertAsync(arch);
        return Ok(new { Mensaje = "Archivo cargado", ArchivoId = id, Nombre = arch.Nombre });
    }
}

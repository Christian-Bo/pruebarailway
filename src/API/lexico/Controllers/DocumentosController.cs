// Lexico.API/Controllers/DocumentosController.cs
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Lexico.Application.Contracts;
using Lexico.Domain.Entities;

namespace Lexico.API.Controllers;

[ApiController]
[Route("api/documentos")]
public class DocumentosController : ControllerBase
{
    private readonly IIdiomaRepository _repoIdioma;
    private readonly IDocumentoRepository _repoDoc;
    private readonly ILogProcesamientoRepository _repoLog;

    public DocumentosController(IIdiomaRepository repoIdioma, IDocumentoRepository repoDoc, ILogProcesamientoRepository repoLog)
    {
        _repoIdioma = repoIdioma;
        _repoDoc = repoDoc;
        _repoLog = repoLog;
    }

    /// <summary>
    /// Sube un txt y lo inserta en 'documentos'. Requiere: usuarioId y codigoIso (es|en|ru).
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Subir([FromForm(Name="file")] IFormFile file, [FromForm] int usuarioId, [FromForm] string codigoIso)
    {
        if (file == null || file.Length == 0) return BadRequest("Archivo vacío.");
        if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) return BadRequest("Solo .txt.");
        if (string.IsNullOrWhiteSpace(codigoIso)) return BadRequest("Debe indicar codigoIso (es|en|ru).");

        var idioma = await _repoIdioma.GetByCodigoAsync(codigoIso.ToLower());
        if (idioma is null) return BadRequest("Idioma no soportado o inactivo.");

        string contenido;
        using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            contenido = await reader.ReadToEndAsync();

        // Hash SHA256 del contenido
        string hash;
        using (var sha = SHA256.Create())
            hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(contenido))).ToLower();

        var doc = new Documento
        {
            UsuarioId = usuarioId,
            NombreArchivo = file.FileName,
            ContenidoOriginal = contenido,
            IdiomaId = idioma.Id,
            TamanoArchivo = (int)file.Length,
            HashDocumento = hash,
            Estado = "cargado",
            FechaCarga = DateTime.UtcNow
        };
        var docId = await _repoDoc.InsertAsync(doc);

        // log
        await _repoLog.InsertAsync(new LogProcesamiento {
            DocumentoId = docId, Etapa = "carga_documento", Estado = "completado", Mensaje = "Documento cargado", TiempoInicio = DateTime.UtcNow, TiempoFin = DateTime.UtcNow, DuracionMs = 0
        });

        return Ok(new { Mensaje = "Documento cargado", DocumentoId = docId, Idioma = codigoIso.ToLower(), Hash = hash });
    }
}

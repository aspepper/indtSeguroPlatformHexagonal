namespace Contratacao.Api.Controllers;

/// <summary>
/// Adaptador de Entrada (Primary/Driving Adapter) HTTP para efetivação de contratação de propostas de seguro.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ContratacoesController : ControllerBase
{
    private readonly IContratarPropostaUseCase _useCase;

    public ContratacoesController(IContratarPropostaUseCase useCase)
    {
        _useCase = useCase;
    }

    /// <summary>
    /// Efetiva a contratação de uma proposta de seguro aprovada.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ContratacaoResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Contratar([FromBody] ContratarPropostaDto dto, CancellationToken ct)
    {
        try
        {
            var resultado = await _useCase.ExecutarAsync(dto, ct);
            return Created($"/api/contratacoes/{resultado.Id}", resultado);
        }
        catch (DomainException ex)
        {
            if (ex.Message.Contains("não encontrada", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { erro = ex.Message });
            }

            return BadRequest(new { erro = ex.Message });
        }
    }
}

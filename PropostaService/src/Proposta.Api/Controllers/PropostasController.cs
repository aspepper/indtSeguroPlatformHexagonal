namespace Proposta.Api.Controllers;

/// <summary>
/// Adaptador de Entrada (Primary/Driving Adapter) HTTP para o gerenciamento de propostas de seguro.
/// Não contém nenhuma lógica de negócio: atua puramente traduzindo requisições HTTP para chamadas aos
/// Casos de Uso (Ports/In) e tratando exceções de domínio (DomainException) para status HTTP adequados.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PropostasController : ControllerBase
{
    private readonly ICriarPropostaUseCase _criarPropostaUseCase;
    private readonly IListarPropostasUseCase _listarPropostasUseCase;
    private readonly IConsultarPropostaUseCase _consultarPropostaUseCase;
    private readonly IAlterarStatusPropostaUseCase _alterarStatusPropostaUseCase;

    public PropostasController(
        ICriarPropostaUseCase criarPropostaUseCase,
        IListarPropostasUseCase listarPropostasUseCase,
        IConsultarPropostaUseCase consultarPropostaUseCase,
        IAlterarStatusPropostaUseCase alterarStatusPropostaUseCase)
    {
        _criarPropostaUseCase = criarPropostaUseCase;
        _listarPropostasUseCase = listarPropostasUseCase;
        _consultarPropostaUseCase = consultarPropostaUseCase;
        _alterarStatusPropostaUseCase = alterarStatusPropostaUseCase;
    }

    /// <summary>
    /// Cria uma nova proposta de seguro.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PropostaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromBody] CriarPropostaDto dto, CancellationToken ct)
    {
        try
        {
            var resultado = await _criarPropostaUseCase.ExecutarAsync(dto, ct);
            return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Id }, resultado);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    /// <summary>
    /// Lista todas as propostas de seguro cadastradas.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PropostaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var resultados = await _listarPropostasUseCase.ExecutarAsync(ct);
        return Ok(resultados);
    }

    /// <summary>
    /// Consulta os detalhes de uma proposta por seu ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PropostaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId([FromRoute] Guid id, CancellationToken ct)
    {
        var resultado = await _consultarPropostaUseCase.ExecutarAsync(id, ct);
        if (resultado == null)
        {
            return NotFound(new { erro = $"Proposta com Id '{id}' não encontrada." });
        }

        return Ok(resultado);
    }

    /// <summary>
    /// Altera o status de uma proposta de seguro.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(PropostaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarStatus([FromRoute] Guid id, [FromBody] AlterarStatusPropostaDto dto, CancellationToken ct)
    {
        try
        {
            var resultado = await _alterarStatusPropostaUseCase.ExecutarAsync(id, dto, ct);
            return Ok(resultado);
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

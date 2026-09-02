using IndtSeguro.Contracts.Events;
using Contratacao.Application.DTOs;
using Contratacao.Application.Ports.In;
using MassTransit;
using Microsoft.Extensions.Logging;
using Contratacao.Domain.Exceptions;

namespace Contratacao.Infrastructure.Messaging;

public class PropostaAprovadaConsumer : IConsumer<PropostaAprovadaEvent>
{
    private readonly IContratarPropostaUseCase _contratarUseCase;
    private readonly ILogger<PropostaAprovadaConsumer> _logger;

    public PropostaAprovadaConsumer(IContratarPropostaUseCase contratarUseCase, ILogger<PropostaAprovadaConsumer> logger)
    {
        _contratarUseCase = contratarUseCase ?? throw new ArgumentNullException(nameof(contratarUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<PropostaAprovadaEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Received PropostaAprovadaEvent for PropostaId {PropostaId}", msg.PropostaId);

        try
        {
            var dto = new ContratarPropostaDto(msg.PropostaId);
            await _contratarUseCase.ExecutarAsync(dto, context.CancellationToken);
            _logger.LogInformation("Contratacao created for PropostaId {PropostaId}", msg.PropostaId);
        }
        catch (DomainException dex)
        {
            // Domain issues (e.g., already contracted) are expected and logged at Info level
            _logger.LogWarning(dex, "DomainException while processing PropostaAprovadaEvent for PropostaId {PropostaId}: {Message}", msg.PropostaId, dex.Message);
        }
        catch (Exception ex)
        {
            // Unexpected errors should be logged and can be moved to DLQ by MassTransit policies
            _logger.LogError(ex, "Unexpected error while processing PropostaAprovadaEvent for PropostaId {PropostaId}", msg.PropostaId);
            throw; // rethrow so MassTransit can apply retry/poison message policies
        }
    }
}

using IndtSeguro.Contracts.Events;
using MassTransit;
using Proposta.Application.Ports.Out;

namespace Proposta.Infrastructure.Messaging;

public class MassTransitPropostaEventPublisher : IPropostaEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitPropostaEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    }

    public async Task PublicarAsync<TEvento>(TEvento evento, CancellationToken ct = default) where TEvento : class
    {
        if (evento is null) throw new ArgumentNullException(nameof(evento));
        await _publishEndpoint.Publish(evento, ct).ConfigureAwait(false);
    }
}

using IndtSeguro.Contracts.Events;
using Proposta.Application.DTOs;
using Proposta.Application.Mappers;
using Proposta.Application.Ports.In;
using Proposta.Application.Ports.Out;
using Proposta.Domain.Exceptions;

namespace Proposta.Application.UseCases;

public class AlterarStatusPropostaUseCase : IAlterarStatusPropostaUseCase
{
    private readonly IPropostaRepository _repository;
    private readonly IPropostaEventPublisher _publisher;

    // Primary ctor used in production (inject publisher)
    public AlterarStatusPropostaUseCase(IPropostaRepository repository, IPropostaEventPublisher publisher)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    // Backwards-compatible ctor used by older tests that only provide repository
    public AlterarStatusPropostaUseCase(IPropostaRepository repository)
        : this(repository, new NoopPropostaEventPublisher())
    {
    }

    public async Task<PropostaResponseDto> ExecutarAsync(Guid id, AlterarStatusPropostaDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var proposta = await _repository.ObterPorIdAsync(id, ct)
            ?? throw new DomainException($"Proposta com Id '{id}' não encontrada.");

        // A transição de estado e suas validações são executadas dentro da entidade PropostaSeguro
        proposta.AlterarStatus(dto.NovoStatus);

        await _repository.AtualizarAsync(proposta, ct);

        // Publica evento de integração dependendo do novo status
        if (dto.NovoStatus == Domain.Enums.StatusProposta.Aprovada)
        {
            var evt = new PropostaAprovadaEvent(proposta.Id, DateTime.UtcNow);
            await _publisher.PublicarAsync(evt, ct);
        }
        else if (dto.NovoStatus == Domain.Enums.StatusProposta.Rejeitada)
        {
            var evt = new PropostaRejeitadaEvent(proposta.Id, DateTime.UtcNow);
            await _publisher.PublicarAsync(evt, ct);
        }

        return proposta.ParaDto();
    }

    // Simple no-op publisher to preserve backward compatibility with tests that don't provide a publisher.
    private class NoopPropostaEventPublisher : IPropostaEventPublisher
    {
        public Task PublicarAsync<TEvento>(TEvento evento, CancellationToken ct = default) where TEvento : class
        {
            return Task.CompletedTask;
        }
    }
}

using Contratacao.Application.DTOs;
using Contratacao.Application.Ports.In;
using Contratacao.Application.Ports.Out;
using Contratacao.Domain.Exceptions;
using ContratacaoEntity = Contratacao.Domain.Entities.Contratacao;

namespace Contratacao.Application.UseCases;

/*
====================================================================================================
  POR QUE ESTA ORQUESTRAÇÃO ENTRE MICROSSERVIÇOS VIVE NO USE CASE DO CONTRATACAOSERVICE?
====================================================================================================
1. Autonomia do Bounded Context (DDD): O serviço de Contratação é o dono do processo de contratação.
   Ele precisa garantir as pré-condições do seu próprio domínio (a proposta precisa existir e estar aprovada,
   e não pode ser contratada duas vezes) antes de emitir um contrato válido.

2. Padrão Coreografia vs Orquestração Centralizada: Em arquiteturas de microsserviços descentralizadas,
   evita-se criar um "orquestrador/BPM" externo pesado para fluxos síncronos simples entre dois serviços.
   Cada serviço é responsável por verificar suas dependências de negócio diretamente através de abstrações (Ports/Out).

3. Encapsulamento na Arquitetura Hexagonal: A chamada remota para o PropostaService é isolada atrás da interface
   IPropostaServiceClient (Port/Out). O Use Case apenas consome essa interface sem saber se ela é realizada via
   HTTP REST, gRPC ou mensagem de barramento.
====================================================================================================
*/

/// <summary>
/// Caso de uso responsável pela efetivação da contratação de uma proposta de seguro aprovada.
/// </summary>
public class ContratarPropostaUseCase : IContratarPropostaUseCase
{
    private readonly IPropostaServiceClient _propostaServiceClient;
    private readonly IContratacaoRepository _contratacaoRepository;

    public ContratarPropostaUseCase(
        IPropostaServiceClient propostaServiceClient,
        IContratacaoRepository contratacaoRepository)
    {
        _propostaServiceClient = propostaServiceClient;
        _contratacaoRepository = contratacaoRepository;
    }

    public async Task<ContratacaoResponseDto> ExecutarAsync(ContratarPropostaDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // 1. Busca o status da proposta via IPropostaServiceClient (Port/Out)
        var proposta = await _propostaServiceClient.ObterStatusPropostaAsync(dto.PropostaId, ct);

        // 2. Se a proposta não existir, lança DomainException
        if (proposta == null)
        {
            throw new DomainException("Proposta não encontrada.");
        }

        // 3. Valida se o status da proposta é "Aprovada"
        if (!string.Equals(proposta.Status, "Aprovada", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Somente propostas aprovadas podem ser contratadas.");
        }

        // 4. Verifica se já existe uma contratação ativa para esta proposta
        var contratacaoExistente = await _contratacaoRepository.ObterPorPropostaIdAsync(dto.PropostaId, ct);
        if (contratacaoExistente != null)
        {
            throw new DomainException("Proposta já contratada.");
        }

        // 5. Cria a entidade Contratacao (Domain), persiste via repositório e retorna o DTO de resposta
        var contratacao = new ContratacaoEntity(dto.PropostaId);

        await _contratacaoRepository.AdicionarAsync(contratacao, ct);

        return new ContratacaoResponseDto(
            contratacao.Id,
            contratacao.PropostaId,
            contratacao.DataContratacao
        );
    }
}

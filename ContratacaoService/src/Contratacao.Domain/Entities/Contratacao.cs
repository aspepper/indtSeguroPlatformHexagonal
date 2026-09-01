using Contratacao.Domain.Exceptions;

namespace Contratacao.Domain.Entities;

/// <summary>
/// Aggregate Root que representa a Contratação de uma Proposta de Seguro.
/// </summary>
public class Contratacao
{
    public Guid Id { get; private set; }
    public Guid PropostaId { get; private set; }
    public DateTime DataContratacao { get; private set; }

    /// <summary>
    /// Construtor privado sem parâmetros exigido pelo Entity Framework Core.
    /// </summary>
    private Contratacao() { }

    /// <summary>
    /// Construtor público para efetivação de uma nova contratação de proposta.
    /// </summary>
    /// <param name="propostaId">Identificador único da proposta aprovada a ser contratada.</param>
    /// <exception cref="DomainException">Lançada caso propostaId seja Guid.Empty.</exception>
    public Contratacao(Guid propostaId)
    {
        if (propostaId == Guid.Empty)
        {
            throw new DomainException("O ID da proposta é obrigatório para realizar a contratação.");
        }

        Id = Guid.NewGuid();
        PropostaId = propostaId;
        DataContratacao = DateTime.UtcNow;
    }
}

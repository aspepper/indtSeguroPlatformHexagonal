using Proposta.Domain.Enums;
using Proposta.Domain.Exceptions;

namespace Proposta.Domain.Entities;

/// <summary>
/// Aggregate Root que representa uma Proposta de Seguro.
/// 
/// Na Arquitetura Hexagonal e no DDD (Domain-Driven Design), o Modelo de Domínio é Rico (Rich Domain Model).
/// Todas as validações, regras de negócio e transições de estado vivem exclusivamente dentro das entidades.
/// Isso garante o encapsulamento, protege as invariantes do sistema e impede que a aplicação ou infraestrutura
/// manipulem o estado da entidade de maneira inconsistente.
/// </summary>
public class PropostaSeguro
{
    public Guid Id { get; private set; }
    public string NomeSegurado { get; private set; } = string.Empty;
    public string CpfSegurado { get; private set; } = string.Empty;
    public decimal ValorCobertura { get; private set; }
    public StatusProposta Status { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public DateTime? DataAtualizacao { get; private set; }

    /// <summary>
    /// Construtor privado sem parâmetros exigido pelo Entity Framework Core para a reconstrução (materialização) da entidade.
    /// </summary>
    private PropostaSeguro() { }

    /// <summary>
    /// Construtor público para criação de uma nova proposta de seguro validando todas as invariantes iniciais de negócio.
    /// </summary>
    /// <param name="nomeSegurado">Nome completo do segurado.</param>
    /// <param name="cpfSegurado">CPF do segurado (com exatamente 11 dígitos numéricos).</param>
    /// <param name="valorCobertura">Valor total da cobertura pretendida (deve ser maior que zero).</param>
    /// <exception cref="DomainException">Lançada caso qualquer regra de validação seja violada.</exception>
    public PropostaSeguro(string nomeSegurado, string cpfSegurado, decimal valorCobertura)
    {
        if (string.IsNullOrWhiteSpace(nomeSegurado))
        {
            throw new DomainException("O nome do segurado é obrigatório.");
        }

        var cpfNumerico = string.Concat((cpfSegurado ?? string.Empty).Where(char.IsDigit));
        if (cpfNumerico.Length != 11)
        {
            throw new DomainException("O CPF do segurado deve conter exatamente 11 dígitos numéricos.");
        }

        if (valorCobertura <= 0)
        {
            throw new DomainException("O valor da cobertura deve ser maior que zero.");
        }

        Id = Guid.NewGuid();
        NomeSegurado = nomeSegurado.Trim();
        CpfSegurado = cpfNumerico;
        ValorCobertura = valorCobertura;
        Status = StatusProposta.EmAnalise;
        DataCriacao = DateTime.UtcNow;
    }

    /// <summary>
    /// Altera o status da proposta aplicando as regras de transição de estado do domínio.
    /// A validação garante a consistência do ciclo de vida da proposta sem depender de serviços externos.
    /// </summary>
    /// <param name="novoStatus">Novo status a ser aplicado.</param>
    /// <exception cref="DomainException">Lançada em transições de status inválidas.</exception>
    public void AlterarStatus(StatusProposta novoStatus)
    {
        if (Status == StatusProposta.Rejeitada)
        {
            throw new DomainException("Não é possível alterar o status de uma proposta que já foi rejeitada.");
        }

        if (Status == StatusProposta.Aprovada && novoStatus == StatusProposta.EmAnalise)
        {
            throw new DomainException("Não é possível alterar uma proposta de Aprovada para Em Analise.");
        }

        if (Status == novoStatus)
        {
            throw new DomainException($"A proposta já está no status {novoStatus}.");
        }

        Status = novoStatus;
        DataAtualizacao = DateTime.UtcNow;
    }

    /// <summary>
    /// Verifica se a proposta foi aprovada.
    /// </summary>
    /// <returns>True se o status for Aprovada, caso contrário False.</returns>
    public bool EstaAprovada()
    {
        return Status == StatusProposta.Aprovada;
    }
}

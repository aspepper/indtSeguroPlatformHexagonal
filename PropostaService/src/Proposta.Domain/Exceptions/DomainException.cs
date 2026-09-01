namespace Proposta.Domain.Exceptions;

/// <summary>
/// Exceção de domínio lançada sempre que uma regra de negócio ou invariante da entidade for violada.
/// Na Arquitetura Hexagonal com DDD, as regras residem no núcleo do domínio e as exceções
/// são capturadas pelo adaptador de entrada (Controllers) para serem traduzidas em status HTTP (ex: 400 Bad Request).
/// </summary>
public class DomainException : Exception
{
    public DomainException(string mensagem) : base(mensagem)
    {
    }
}

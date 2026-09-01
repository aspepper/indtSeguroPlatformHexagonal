namespace Contratacao.Domain.Exceptions;

/// <summary>
/// Exceção de domínio lançada quando uma regra de negócio ou validação do modelo é violada no contexto de contratação.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string mensagem) : base(mensagem)
    {
    }
}

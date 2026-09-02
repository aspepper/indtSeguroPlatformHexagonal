using Proposta.Domain.Entities;
using Proposta.Domain.Enums;
using Proposta.Domain.Exceptions;
using Xunit;

namespace Proposta.UnitTests.Domain;

public class PropostaSeguroTests
{
    [Fact]
    public void Deve_Criar_Proposta_Valida_Com_Status_EmAnalise()
    {
        // Arrange
        var nome = "Estela Pimenta";
        var cpf = "06291540070";
        var valor = 50000m;

        // Act
        var proposta = new PropostaSeguro(nome, cpf, valor);

        // Assert
        Assert.NotEqual(Guid.Empty, proposta.Id);
        Assert.Equal(nome, proposta.NomeSegurado);
        Assert.Equal(cpf, proposta.CpfSegurado);
        Assert.Equal(valor, proposta.ValorCobertura);
        Assert.Equal(StatusProposta.EmAnalise, proposta.Status);
        Assert.True(proposta.DataCriacao <= DateTime.UtcNow);
        Assert.Null(proposta.DataAtualizacao);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Deve_Lancar_DomainException_Quando_NomeSegurado_For_Vazio(string? nomeInvalido)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => new PropostaSeguro(nomeInvalido!, "14168770028", 1000m));
        Assert.Contains("nome do segurado é obrigatório", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("1234567890")] // 10 dígitos
    [InlineData("123456789012")] // 12 dígitos
    [InlineData("CPF-INVALIDO")]
    public void Deve_Lancar_DomainException_Quando_CpfSegurado_Nao_Tiver_11_Digitos(string cpfInvalido)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => new PropostaSeguro("Maria Oliveira", cpfInvalido, 1000m));
        Assert.Contains("11 dígitos", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Deve_Lancar_DomainException_Quando_ValorCobertura_For_Menor_Ou_Igual_A_Zero(decimal valorInvalido)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => new PropostaSeguro("Maria Oliveira", "14168770028", valorInvalido));
        Assert.Contains("maior que zero", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deve_Permitir_Transicao_De_EmAnalise_Para_Aprovada()
    {
        // Arrange
        var proposta = new PropostaSeguro("Arthur Pimenta", "89982292005", 20000m);

        // Act
        proposta.AlterarStatus(StatusProposta.Aprovada);

        // Assert
        Assert.Equal(StatusProposta.Aprovada, proposta.Status);
        Assert.NotNull(proposta.DataAtualizacao);
        Assert.True(proposta.EstaAprovada());
    }

    [Fact]
    public void Deve_Permitir_Transicao_De_EmAnalise_Para_Rejeitada()
    {
        // Arrange
        var proposta = new PropostaSeguro("Carlos Santos", "89982292005", 20000m);

        // Act
        proposta.AlterarStatus(StatusProposta.Rejeitada);

        // Assert
        Assert.Equal(StatusProposta.Rejeitada, proposta.Status);
        Assert.NotNull(proposta.DataAtualizacao);
        Assert.False(proposta.EstaAprovada());
    }

    [Fact]
    public void Deve_Lancar_DomainException_Ao_Tentar_Voltar_De_Aprovada_Para_EmAnalise()
    {
        // Arrange
        var proposta = new PropostaSeguro("Arthur Pimenta", "89982292005", 20000m);
        proposta.AlterarStatus(StatusProposta.Aprovada);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => proposta.AlterarStatus(StatusProposta.EmAnalise));
        Assert.Contains("Não é possível alterar uma proposta de Aprovada para Em Analise", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deve_Lancar_DomainException_Ao_Tentar_Alterar_Status_De_Proposta_Rejeitada()
    {
        // Arrange
        var proposta = new PropostaSeguro("Arthur Pimenta", "89982292005", 20000m);
        proposta.AlterarStatus(StatusProposta.Rejeitada);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => proposta.AlterarStatus(StatusProposta.Aprovada));
        Assert.Contains("rejeitada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deve_Lancar_DomainException_Ao_Tentar_Setar_Mesmo_Status_Atual()
    {
        // Arrange
        var proposta = new PropostaSeguro("Arthur Pimenta", "89982292005", 20000m);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => proposta.AlterarStatus(StatusProposta.EmAnalise));
        Assert.Contains("já está no status", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}

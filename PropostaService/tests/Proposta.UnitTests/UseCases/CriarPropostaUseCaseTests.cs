using Moq;
using Proposta.Application.DTOs;
using Proposta.Application.Ports.Out;
using Proposta.Application.UseCases;
using Proposta.Domain.Entities;
using Proposta.Domain.Enums;
using Proposta.Domain.Exceptions;
using Xunit;

namespace Proposta.UnitTests.UseCases;

public class CriarPropostaUseCaseTests
{
    private readonly Mock<IPropostaRepository> _repositoryMock;
    private readonly CriarPropostaUseCase _useCase;

    public CriarPropostaUseCaseTests()
    {
        _repositoryMock = new Mock<IPropostaRepository>();
        _useCase = new CriarPropostaUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task Deve_Chamar_AdicionarAsync_Uma_Vez_Quando_Proposta_For_Valida()
    {
        // Arrange
        var dto = new CriarPropostaDto("Ana Lima", "98765432100", 75000m);

        // Act
        var resposta = await _useCase.ExecutarAsync(dto);

        // Assert
        Assert.NotNull(resposta);
        Assert.Equal(dto.NomeSegurado, resposta.NomeSegurado);
        Assert.Equal(dto.CpfSegurado, resposta.CpfSegurado);
        Assert.Equal(dto.ValorCobertura, resposta.ValorCobertura);
        Assert.Equal(StatusProposta.EmAnalise, resposta.Status);

        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<PropostaSeguro>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deve_Propagar_DomainException_Quando_Dados_Forem_Invalidos_Sem_Chamar_AdicionarAsync()
    {
        // Arrange (nome vazio para forçar exceção de domínio)
        var dto = new CriarPropostaDto("", "98765432100", 75000m);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _useCase.ExecutarAsync(dto));

        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<PropostaSeguro>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

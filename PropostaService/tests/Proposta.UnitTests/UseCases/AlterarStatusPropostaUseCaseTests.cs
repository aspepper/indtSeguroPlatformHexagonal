using Moq;
using Proposta.Application.DTOs;
using Proposta.Application.Ports.Out;
using Proposta.Application.UseCases;
using Proposta.Domain.Entities;
using Proposta.Domain.Enums;
using Proposta.Domain.Exceptions;
using Xunit;

namespace Proposta.UnitTests.UseCases;

public class AlterarStatusPropostaUseCaseTests
{
    private readonly Mock<IPropostaRepository> _repositoryMock;
    private readonly AlterarStatusPropostaUseCase _useCase;

    public AlterarStatusPropostaUseCaseTests()
    {
        _repositoryMock = new Mock<IPropostaRepository>();
        _useCase = new AlterarStatusPropostaUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task Deve_Lancar_DomainException_Nao_Encontrada_Quando_ObterPorIdAsync_Retornar_Null()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();
        var dto = new AlterarStatusPropostaDto(StatusProposta.Aprovada);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(idInexistente, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropostaSeguro?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => _useCase.ExecutarAsync(idInexistente, dto));

        Assert.Contains("não encontrada", ex.Message, StringComparison.OrdinalIgnoreCase);
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<PropostaSeguro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deve_Chamar_AtualizarAsync_Quando_Transicao_De_Status_For_Valida()
    {
        // Arrange
        var proposta = new PropostaSeguro("Lucas Mendes", "11122233344", 30000m);
        var dto = new AlterarStatusPropostaDto(StatusProposta.Aprovada);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(proposta.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposta);

        // Act
        var resposta = await _useCase.ExecutarAsync(proposta.Id, dto);

        // Assert
        Assert.NotNull(resposta);
        Assert.Equal(StatusProposta.Aprovada, resposta.Status);
        Assert.NotNull(resposta.DataAtualizacao);

        _repositoryMock.Verify(r => r.AtualizarAsync(It.Is<PropostaSeguro>(p => p.Id == proposta.Id && p.Status == StatusProposta.Aprovada), It.IsAny<CancellationToken>()), Times.Once);
    }
}

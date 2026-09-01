using Contratacao.Application.DTOs;
using Contratacao.Application.Ports.Out;
using Contratacao.Application.UseCases;
using Contratacao.Domain.Exceptions;
using Moq;
using Xunit;
using ContratacaoEntity = Contratacao.Domain.Entities.Contratacao;

namespace Contratacao.UnitTests.UseCases;

public class ContratarPropostaUseCaseTests
{
    private readonly Mock<IPropostaServiceClient> _propostaServiceClientMock;
    private readonly Mock<IContratacaoRepository> _repositoryMock;
    private readonly ContratarPropostaUseCase _useCase;

    public ContratarPropostaUseCaseTests()
    {
        _propostaServiceClientMock = new Mock<IPropostaServiceClient>();
        _repositoryMock = new Mock<IContratacaoRepository>();

        _useCase = new ContratarPropostaUseCase(
            _propostaServiceClientMock.Object,
            _repositoryMock.Object
        );
    }

    [Fact]
    public async Task Deve_Lancar_DomainException_Se_IPropostaServiceClient_Retornar_Null()
    {
        // Arrange
        var propostaId = Guid.NewGuid();
        var dto = new ContratarPropostaDto(propostaId);

        _propostaServiceClientMock
            .Setup(x => x.ObterStatusPropostaAsync(propostaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropostaStatusDto?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => _useCase.ExecutarAsync(dto));
        Assert.Contains("Proposta não encontrada", ex.Message, StringComparison.OrdinalIgnoreCase);

        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<ContratacaoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("EmAnalise")]
    [InlineData("Rejeitada")]
    [InlineData("Pendente")]
    public async Task Deve_Lancar_DomainException_Se_Status_Da_Proposta_Nao_For_Aprovada(string statusInvalido)
    {
        // Arrange
        var propostaId = Guid.NewGuid();
        var dto = new ContratarPropostaDto(propostaId);

        _propostaServiceClientMock
            .Setup(x => x.ObterStatusPropostaAsync(propostaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropostaStatusDto(propostaId, statusInvalido));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => _useCase.ExecutarAsync(dto));
        Assert.Contains("Somente propostas aprovadas podem ser contratadas", ex.Message, StringComparison.OrdinalIgnoreCase);

        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<ContratacaoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deve_Lancar_DomainException_Se_Ja_Existir_Contratacao_Para_A_Mesma_PropostaId()
    {
        // Arrange
        var propostaId = Guid.NewGuid();
        var dto = new ContratarPropostaDto(propostaId);

        _propostaServiceClientMock
            .Setup(x => x.ObterStatusPropostaAsync(propostaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropostaStatusDto(propostaId, "Aprovada"));

        _repositoryMock
            .Setup(r => r.ObterPorPropostaIdAsync(propostaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContratacaoEntity(propostaId));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => _useCase.ExecutarAsync(dto));
        Assert.Contains("Proposta já contratada", ex.Message, StringComparison.OrdinalIgnoreCase);

        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<ContratacaoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deve_Chamar_AdicionarAsync_Uma_Vez_Quando_Tudo_For_Valido_E_Retornar_DTO_Correto()
    {
        // Arrange
        var propostaId = Guid.NewGuid();
        var dto = new ContratarPropostaDto(propostaId);

        _propostaServiceClientMock
            .Setup(x => x.ObterStatusPropostaAsync(propostaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropostaStatusDto(propostaId, "Aprovada"));

        _repositoryMock
            .Setup(r => r.ObterPorPropostaIdAsync(propostaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContratacaoEntity?)null);

        // Act
        var resposta = await _useCase.ExecutarAsync(dto);

        // Assert
        Assert.NotNull(resposta);
        Assert.NotEqual(Guid.Empty, resposta.Id);
        Assert.Equal(propostaId, resposta.PropostaId);
        Assert.True(resposta.DataContratacao <= DateTime.UtcNow);

        _repositoryMock.Verify(r => r.AdicionarAsync(It.Is<ContratacaoEntity>(c => c.PropostaId == propostaId), It.IsAny<CancellationToken>()), Times.Once);
    }
}

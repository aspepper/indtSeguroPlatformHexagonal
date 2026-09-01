using System.Net;
using System.Text.Json;
using Contratacao.Application.DTOs;
using Contratacao.Application.Ports.Out;
using Contratacao.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Contratacao.Infrastructure.ExternalServices;

/// <summary>
/// Adaptador de Saída (Driven Adapter) que implementa a comunicação HTTP com o PropostaService.
/// Encapsula as chamadas de rede, serialização/desserialização JSON e tratamento de resiliência e exceções.
/// </summary>
public class PropostaServiceHttpClient : IPropostaServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PropostaServiceHttpClient> _logger;

    public PropostaServiceHttpClient(HttpClient httpClient, ILogger<PropostaServiceHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PropostaStatusDto?> ObterStatusPropostaAsync(Guid propostaId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/propostas/{propostaId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var rawJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            var id = root.TryGetProperty("id", out var idProp) || root.TryGetProperty("Id", out idProp)
                ? idProp.GetGuid()
                : propostaId;

            string statusStr = string.Empty;

            if (root.TryGetProperty("status", out var statusElem) || root.TryGetProperty("Status", out statusElem))
            {
                if (statusElem.ValueKind == JsonValueKind.String)
                {
                    statusStr = statusElem.GetString() ?? string.Empty;
                }
                else if (statusElem.ValueKind == JsonValueKind.Number)
                {
                    var statusNum = statusElem.GetInt32();
                    statusStr = statusNum switch
                    {
                        1 => "EmAnalise",
                        2 => "Aprovada",
                        3 => "Rejeitada",
                        _ => statusNum.ToString()
                    };
                }
            }

            return new PropostaStatusDto(id, statusStr);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Falha na comunicação HTTP ao verificar o status da proposta '{PropostaId}'.", propostaId);
            throw new DomainException("Não foi possível verificar o status da proposta no momento.");
        }
    }
}

namespace Proposta.Application.Ports;

/*
====================================================================================================
  DIFERENÇA ENTRE PORTS/IN (DRIVING PORTS) E PORTS/OUT (DRIVEN PORTS) NA ARQUITETURA HEXAGONAL
====================================================================================================

1. PORTS/IN (Driving Ports / Portas de Entrada):
   - O que são: Interfaces que representam os Casos de Uso expostos pela nossa aplicação para o mundo externo.
   - Quem chama: Os adaptadores de entrada (Primary / Driving Adapters), como Controllers REST (API), CLI, 
     Worker Services ou consumidores de filas.
   - Propósito: Definem O QUE a aplicação é capaz de FAZER. A aplicação é "guiada" (driven) por essas chamadas.
   - Exemplo: ICriarPropostaUseCase, IAlterarStatusPropostaUseCase.

2. PORTS/OUT (Driven Ports / Portas de Saída):
   - O que são: Interfaces de dependência que a aplicação necessita para interagir com recursos externos.
   - Quem implementa: Os adaptadores de saída (Secondary / Driven Adapters) localizados na camada de Infrastructure, 
     como repositórios com EF Core, clientes HTTP (HttpClient), envio de e-mails ou publicação em barramentos.
   - Propósito: Definem do que a aplicação PRECISA para funcionar. A aplicação "guia" (drives) esses recursos externos.
   - Exemplo: IPropostaRepository, IPropostaServiceClient.

Resumo para entrevista técnica:
"Ports/In são as portas que a API/Controller usa para ENTRAR no núcleo da aplicação e executar um caso de uso.
Ports/Out são as portas que a aplicação usa para SAIR do núcleo e acessar banco de dados ou APIs externas."
====================================================================================================
*/
internal static class ExplicacaoPorts
{
}

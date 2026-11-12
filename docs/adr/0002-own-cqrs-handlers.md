# 0002. CQRS leve com handlers próprios (sem MediatR)

- **Status:** Aceito
- **Data:** 2026-11-11

## Contexto

Os casos de uso ficam na camada `Application`, separados em leitura (queries) e escrita (commands). O padrão mais comum em .NET para despachar essas mensagens é o MediatR, mas a partir da versão 13 ele passou a ter licença comercial. O projeto precisa de pouco do que ele oferece: localizar o handler certo e, eventualmente, aplicar comportamentos transversais (validação, logs).

## Decisão

Usar interfaces próprias e mínimas:

```csharp
public interface IQuery<TResponse>;
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
```

(e o equivalente `ICommand`/`ICommandHandler` quando houver escrita). Os handlers são registrados por convenção com **Scrutor** (varredura do assembly), e os endpoints recebem o handler concreto por injeção de dependência. Comportamentos transversais, quando necessários, serão decorators registrados com `services.Decorate(...)`.

## Consequências

- **Positivas:** nenhuma dependência com licença comercial; nenhuma "mágica": o endpoint declara exatamente qual handler usa, o que facilita a navegação no código. Handlers continuam simples de testar.
- **Negativas:** sem pipeline pronto; cada comportamento transversal é um decorator escrito à mão. Para o número de casos de uso deste projeto, é pouco código.

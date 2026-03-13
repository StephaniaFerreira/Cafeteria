## Cafeteria - Exemplo de Mensageria com RabbitMQ e .NET 8

Projeto de portfólio que simula uma **cafeteria** usando **mensageria** com RabbitMQ.

- `Cliente`: aplicativo console que representa o **cliente** fazendo pedidos (Producer).
- `Funcionario`: aplicativo console que representa o **funcionário** processando pedidos (Consumer).
- `Cafeteria.Tests`: projeto de **testes de integração** com xUnit, exercitando RabbitMQ de verdade.

Os pedidos são enviados para uma fila RabbitMQ (`pedidos-cafeteria`) e consumidos pelo funcionário.

---

## Tecnologias utilizadas

- **.NET 8** (console apps)
- **RabbitMQ** (mensageria)
- **Pacote** `RabbitMQ.Client` (versão 6.8.1)
- **xUnit** para testes de integração
- **Docker** para subir rapidamente o RabbitMQ local
- **Cursor** como IDE/assistente de IA para apoiar na refatoração, organização de configuração compartilhada e criação de testes de integração

---

## Estrutura do projeto

- `Cliente/` – Producer (cliente da cafeteria)
- `Funcionario/` – Consumer (funcionário da cafeteria)
- `AppSettings.cs` – configuração compartilhada entre os projetos
- `cafeteria.config.json` – arquivo de configuração (por exemplo, `HostName` do RabbitMQ)
- `Cafeteria.Tests/` – testes de integração

---

## Configuração do RabbitMQ com Docker

1. Garanta que o Docker esteja instalado e rodando.
2. Suba o container do RabbitMQ com o plugin de management:

```bash
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management
```

- Porta **5672**: usada pelos aplicativos .NET.
- Porta **15672**: painel web (`http://localhost:15672`), login padrão `guest` / `guest`.

---

## Configuração da aplicação

O arquivo `cafeteria.config.json` na raiz define o host do RabbitMQ:

```json
{
  "RabbitMq": {
    "HostName": "localhost"
  }
}
```

Esse arquivo é carregado pela classe/record `AppSettings` (em `AppSettings.cs`) e **compartilhado** por:

- `Cliente/Program.cs`
- `Funcionario/Program.cs`
- `Cafeteria.Tests`

Se for necessário alterar o endereço do RabbitMQ, basta mudar **um único lugar** (`cafeteria.config.json`), evitando duplicação de configuração.

---

## Como rodar o projeto

Na pasta raiz (`Cafeteria`):

1. **Compilar tudo (opcional):**

```bash
dotnet build
```

2. **Rodar o Consumer (Funcionário)** em um terminal:

```bash
dotnet run --project Funcionario/Funcionario.csproj
```

3. **Rodar o Producer (Cliente)** em outro terminal:

```bash
dotnet run --project Cliente/Cliente.csproj
```

4. Fazer pedidos pelo console do `Cliente` e acompanhar o processamento no console do `Funcionario`.

---

## Como rodar os testes de integração

Com o RabbitMQ rodando (Docker ou instalação local), execute:

```bash
dotnet test Cafeteria.Tests/Cafeteria.Tests.csproj
```

Os testes fazem:

- **Ping de conexão** com o RabbitMQ usando a configuração de `AppSettings`.
- **Publicação e consumo reais** de mensagem em uma fila de teste (`cafeteria-integration-tests`).

---

## Decisões e refatorações importantes

Considerando que utilizei este projeto para experimentar o desenvolvimento com cursor enviei o prompt para implementar a API, após a implementação eu pedi alterações nas decisões a seguir para que o projeto ficasse melhor modelado.

### 1. De tupla para tipo forte (`MenuItem`)

Inicialmente, os itens do cardápio eram representados como uma **tupla** no dicionário, algo como:

- `Dictionary<int, (string Nome, decimal Preco)>`

Isso funciona, mas fica mais legível e extensível ter um **tipo dedicado**.  
Por isso, foi criado o `record`:

- `record MenuItem(string Nome, decimal Preco);`

E o dicionário passou a ser:

- `Dictionary<int, MenuItem>`

**Motivos:**

- Representa melhor o **domínio da cafeteria** (um item de menu com nome e preço).
- Facilita evoluir (por exemplo, adicionar `Categoria`, `TempoPreparo`, `Disponivel`).
- Deixa o código de leitura e escrita de propriedades mais claro (`item.Nome`, `item.Preco`).

### 2. De `string` para `int` como chave do dicionário

No começo, o dicionário do cardápio usava `string` como chave:

- `Dictionary<string, ...>` com chaves `"1"`, `"2"`, `"3"`, etc.

Como a escolha do usuário no menu é **um número**, faz mais sentido usar:

- `Dictionary<int, MenuItem>`

e fazer `int.TryParse` da entrada do console.

**Motivos:**

- `int` é um tipo mais adequado para representar **códigos numéricos** de opções de menu.
- Evita comparar strings `"1"`, `"2"` quando, conceitualmente, estamos lidando com números.
- Fica mais consistente e expressivo do ponto de vista de domínio.

### 3. Configuração compartilhada do RabbitMQ para evitar duplicação

Inicialmente, o `HostName` do RabbitMQ (`"localhost"`) estava **“hard-coded”** em cada projeto.  
Isso significa que:

- `Cliente` tinha um `const string HostName = "localhost";`
- `Funcionario` também tinha o mesmo `const`.

Para evitar duplicação e facilitar manutenção, foi extraída uma configuração compartilhada:

- Arquivo `cafeteria.config.json` na raiz.
- Tipo `AppSettings` em `AppSettings.cs`, com método estático `Load()`:
  - Lê o JSON.
  - Faz o bind para `AppSettings` / `RabbitMqSettings`.

Ambos os projetos (`Cliente` e `Funcionario`) agora fazem:

```csharp
var settings = AppSettings.Load();
var factory = new ConnectionFactory { HostName = settings.RabbitMq.HostName };
```

**Motivos:**

- **Evitar duplicação** de configuração (DRY).
- Permitir mudar o host do RabbitMQ em **um único ponto**.
- Reaproveitar a mesma configuração também nos **testes de integração**.

---

## Próximos passos possíveis

Algumas ideias para evoluir este portfólio:

- Adicionar **confirmação de recebimento** de pedidos para o cliente.
- Modelar mais entidades (por exemplo, `Pedido` compartilhado entre projects).
- Implementar **dead-letter queue** ou filas de erro.
- Expor um **endpoint HTTP** (Web API) como fachada para o producer.


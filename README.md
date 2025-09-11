# BankMore
 
# BankMore - Sistema Bancário Distribuído

## 📖 Visão Geral
O **BankMore** é um sistema bancário de demonstração que implementa uma arquitetura **DDD + CQRS** usando **.NET 8**, **MediatR** e comunicação assíncrona via **Kafka**.  
O sistema é composto por múltiplos serviços:
- **AccountService**: gerenciamento de contas, autenticação JWT, saldos e movimentações.
- **TransferService**: processamento de transferências entre contas e publicação de eventos.
- **TariffService**: aplicação de tarifas sobre transferências via consumidores Kafka.
- **Shared**: contratos e utilitários comuns, incluindo validador de CPF.
- **IntegrationTests**: testes de integração com **xUnit**.

---

## 🏗 Arquitetura
- **DDD + CQRS**: Separação entre comandos e consultas, com `MediatR` para handlers.
- **Eventos Assíncronos**:  
  - `TransferService` → envia evento `transfers` para Kafka.
  - `TariffService` → consome `transfers`, grava tarifas e publica `tariffs`.
  - `AccountService` → consome `tariffs`, debita contas.
- **Validação de CPF**: algoritmo completo no projeto `Shared`.
- **Idempotência**: evita processamento duplicado em movimentações e transferências.
- **Polly**: retries e backoff exponencial para consumidores Kafka.
- **JWT**: autenticação para APIs.

---

## 🛠 Tecnologias Principais
- **.NET 8**, **ASP.NET Core Web API**
- **MediatR** (CQRS + Handlers)
- **Dapper** (acesso ao SQLite)
- **Confluent.Kafka** + **Polly** (resiliência)
- **Swagger / OpenAPI** (documentação)
- **Docker + docker-compose**
- **xUnit + FluentAssertions** (testes)

---

## 📂 Estrutura do Projeto

BankMore/

├── src/

│ ├── Shared/ # Contratos, JwtSettings, CpfValidator

│ ├── AccountService/ # API + Handlers + Consumidor tariffs

│ ├── TransferService/ # API + Handlers + Produtor transfers

│ ├── TariffService/ # API + Consumidor transfers -> produtor tariffs

│ └── docker-compose.yml # Subida local com Kafka/Zookeeper

├── tests/

│ └── IntegrationTests/ # Testes xUnit + WebApplicationFactory

├── README.md

└── BankMore.sln # Solution única


---

## 🚀 Executando o Projeto

### 1. Pré-requisitos
- **Docker** e **Docker Compose**
- **.NET 8 SDK**

### 2. Subindo ambiente local
```bash
docker compose build
docker compose up
```

### 3. Acessando serviços

| Serviço         | Porta | Swagger URL                                                    |
| --------------- | ----- | -------------------------------------------------------------- |
| AccountService  | 8081  | [http://localhost:8081/swagger](http://localhost:8081/swagger) |
| TransferService | 8082  | [http://localhost:8082/swagger](http://localhost:8082/swagger) |
| TariffService   | 8083  | [http://localhost:8083/swagger](http://localhost:8083/swagger) |
| Kafka           | 9092  | PLAINTEXT://localhost:9092                                     |
| Zookeeper       | 2181  | PLAINTEXT://localhost:2181                                     |

### 🧪 4. Testes

```bash
cd tests/IntegrationTests
dotnet test
```

### 🔒 Segurança

-Troque a chave JWT em appsettings.json para produção.
-Configure Kafka com autenticação/TLS se necessário.
-Adicione DLQ (Dead Letter Queue) para mensagens que falharem após retries.

### 📌 Próximos Passos

 Adicionar testes de contrato para eventos Kafka
 Suporte a KafkaFlow para abstração dos consumidores
 Monitoramento e métricas com Prometheus/Grafana
 Pipeline CI/CD com GitHub Actions

### 🧪 Roteiro de Testes
1. Criar contas

Endpoint: POST /account/register (AccountService)

Body:
```bash
{
  "cpf": "12345678909",
  "nome": "João",
  "senha": "123"
}
```

Esperado:
HTTP 201
Conta criada no banco
Retornar idContaCorrente
Repita para criar outra conta (destino da transferência).

2. Login

Endpoint: POST /account/login

Body:
```bash
{
  "cpf": "12345678909",
  "senha": "123"
}
```

Esperado:
HTTP 200
Retorna token JWT
Guarde esse token para as próximas chamadas.

3. Fazer depósito inicial

Endpoint: POST /account/movement
Header: Authorization: Bearer {token}

Body:
```bash
{
  "idRequisicao": "req-dep1",
  "tipo": "C",
  "valor": 1000
}
```
Esperado:
HTTP 204
Saldo da conta atualizado


4. Consultar saldo

Endpoint: GET /account/balance
Header: Authorization: Bearer {token}

Esperado:
HTTP 200
Retorna saldo >= 1000

5. Fazer transferência

Endpoint: POST /transfer
Header: Authorization: Bearer {token}

Body:
```bash
{
  "idRequisicao": "req-transf1",
  "idContaOrigem": "{conta_origem_id}",
  "idContaDestino": "{conta_destino_id}",
  "valor": 100
}
```

Esperado:
HTTP 204

Evento enviado para o Kafka no tópico transfers
TariffService consome e publica evento tariffs
AccountService consome tariffs e debita tarifa automaticamente

6. Validar tarifação automática

Endpoint: GET /account/balance
Header: Authorization: Bearer {token}

Esperado:
Saldo = 1000 - 100 (transferência) - tarifa (ex.: 2.0)

7. Verificar Kafka (opcional)

Se tiver Kafka UI:
Acesse http://localhost:8085

Veja tópicos:
transfers
tariffs
Confirme que eventos foram publicados.

8. Testar idempotência

Repita a mesma chamada de POST /transfer com mesmo idRequisicao.

Esperado:
HTTP 204
Nenhuma transferência duplicada
Nenhuma tarifa duplicada

9. Testes de falha

Tente transferir valor maior que saldo
Tente logar com senha errada
Tente acessar endpoint sem token
Esperado: mensagens de erro adequadas.

### 🧰 Testes Automatizados (xUnit)

Você pode rodar:
```bash
dotnet test tests/IntegrationTests
```

### 📝 Licença

Este projeto é apenas para fins educacionais/demonstração.

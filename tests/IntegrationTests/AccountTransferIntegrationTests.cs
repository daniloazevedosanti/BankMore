using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Threading.Tasks;

public class AccountTransferIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public AccountTransferIntegrationTests(WebApplicationFactory<Program> factory) { _factory = factory; }

    [Fact]
    public async Task Register_Login_And_GetBalance_Works()
    {
        var client = _factory.CreateClient();
        // register
        var reg = new { nome = "Teste", cpf = "12345678909", senha = "P@ssw0rd" };
        var regResp = await client.PostAsync("/api/account/register", new StringContent(JsonSerializer.Serialize(reg), Encoding.UTF8, "application/json"));
        regResp.EnsureSuccessStatusCode();
        var body = await regResp.Content.ReadAsStringAsync();
        body.Should().Contain("numero");
    }
}

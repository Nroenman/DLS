using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AirportSystem.Flights.Services.Auth;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;

namespace AirportSystem.Tests.Services;

public class KeycloakServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:BaseUrl"]          = "http://keycloak:8080",
                ["Keycloak:Realm"]            = "airport-system",
                ["Keycloak:ClientId"]         = "airport-api",
                ["Keycloak:ClientSecret"]     = "airport-api-secret",
                ["Keycloak:FrontendClientId"] = "airport-frontend"
            })
            .Build();

    private static string TokenJson(string token = "mock-access-token") =>
        JsonSerializer.Serialize(new
        {
            access_token  = token,
            refresh_token = "mock-refresh-token",
            expires_in    = 86400,
            token_type    = "Bearer"
        });

    /// <summary>
    /// Build a KeycloakService whose underlying HttpClient has all HTTP calls
    /// intercepted by the provided handler mock.
    /// </summary>
    private static (KeycloakService service, Mock<HttpMessageHandler> handlerMock)
        Build(params (HttpMethod method, string urlPart, HttpResponseMessage response)[] stubs)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Loose);

        foreach (var (method, urlPart, response) in stubs)
        {
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r =>
                        r.Method == method &&
                        r.RequestUri!.ToString().Contains(urlPart)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        var client  = new HttpClient(handlerMock.Object);
        var service = new KeycloakService(client, BuildConfig());
        return (service, handlerMock);
    }

    private static HttpResponseMessage Ok(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage Created(string locationSuffix = "/users/new-user-uuid") =>
        new(HttpStatusCode.Created)
        {
            Headers = { Location = new Uri($"http://keycloak:8080/admin/realms/airport-system{locationSuffix}") }
        };

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenResponse()
    {
        var (service, _) = Build(
            (HttpMethod.Post, "openid-connect/token", Ok(TokenJson())));

        var result = await service.LoginAsync("user@test.com", "Password1!");

        result.AccessToken.Should().Be("mock-access-token");
        result.ExpiresIn.Should().Be(86400);
    }

    [Fact]
    public async Task Login_BadCredentials_ThrowsUnauthorizedAccessException()
    {
        var errorBody = JsonSerializer.Serialize(new
        {
            error             = "invalid_grant",
            error_description = "Invalid user credentials"
        });

        var (service, _) = Build(
            (HttpMethod.Post, "openid-connect/token",
             new HttpResponseMessage(HttpStatusCode.Unauthorized)
             {
                 Content = new StringContent(errorBody, Encoding.UTF8, "application/json")
             }));

        var act = async () => await service.LoginAsync("bad@test.com", "wrong");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid user credentials*");
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidInput_CreatesUserAndReturnsToken()
    {
        var roleJson = JsonSerializer.Serialize(new { id = "role-uuid", name = "Passenger" });

        var (service, handlerMock) = Build(
            // 1. Service-account token for admin calls
            (HttpMethod.Post, "openid-connect/token", Ok(TokenJson("admin-token"))),
            // 2. Create user → 201 Created
            (HttpMethod.Post, "admin/realms/airport-system/users", Created()),
            // 3. Look up role representation
            (HttpMethod.Get,  "admin/realms/airport-system/roles/Passenger", Ok(roleJson)),
            // 4. Assign role
            (HttpMethod.Post, "admin/realms/airport-system/users/new-user-uuid/role-mappings", Ok("[]")),
            // 5. Login after registration
            (HttpMethod.Post, "openid-connect/token", Ok(TokenJson("user-token"))));

        var result = await service.RegisterAsync("newuser", "new@test.com", "Pass123!", "Passenger");

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_DuplicateUser_ThrowsInvalidOperationException()
    {
        var (service, _) = Build(
            // Admin token
            (HttpMethod.Post, "openid-connect/token", Ok(TokenJson("admin-token"))),
            // Keycloak returns 409 Conflict for duplicate username/email
            (HttpMethod.Post, "admin/realms/airport-system/users",
             new HttpResponseMessage(HttpStatusCode.Conflict)
             {
                 Content = new StringContent("{}", Encoding.UTF8, "application/json")
             }));

        var act = async () =>
            await service.RegisterAsync("dup", "dup@test.com", "Pass123!");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    // ── Claims transformer ────────────────────────────────────────────────────

    [Fact]
    public async Task KeycloakClaimsTransformer_AddsRoleClaimsFromRealmAccess()
    {
        var transformer = new KeycloakClaimsTransformer();

        var realmAccessJson = JsonSerializer.Serialize(new
        {
            roles = new[] { "Admin", "Passenger", "offline_access" }
        });

        var identity = new System.Security.Claims.ClaimsIdentity(
            new[]
            {
                new System.Security.Claims.Claim("realm_access", realmAccessJson),
                new System.Security.Claims.Claim("sub", Guid.NewGuid().ToString())
            },
            "Bearer");

        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        var transformed = await transformer.TransformAsync(principal);

        transformed.IsInRole("Admin").Should().BeTrue();
        transformed.IsInRole("Passenger").Should().BeTrue();
        // Keycloak system roles should also pass through
        transformed.IsInRole("offline_access").Should().BeTrue();
    }

    [Fact]
    public async Task KeycloakClaimsTransformer_MissingRealmAccess_DoesNotThrow()
    {
        var transformer = new KeycloakClaimsTransformer();
        var identity    = new System.Security.Claims.ClaimsIdentity(
            new[] { new System.Security.Claims.Claim("sub", Guid.NewGuid().ToString()) },
            "Bearer");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        var act = async () => await transformer.TransformAsync(principal);

        await act.Should().NotThrowAsync();
    }
}

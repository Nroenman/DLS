using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AirportSystem.Flights.Services.Auth;

// ── DTOs returned to callers ───────────────────────────────────────────────────

public record KeycloakTokenResponse(
    [property: JsonPropertyName("access_token")]  string AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("expires_in")]    int ExpiresIn,
    [property: JsonPropertyName("token_type")]    string TokenType
);

// ── Interface ──────────────────────────────────────────────────────────────────

public interface IKeycloakService
{
    /// <summary>Login a user via Keycloak's Resource Owner Password Credentials grant.</summary>
    Task<KeycloakTokenResponse> LoginAsync(string email, string password);

    /// <summary>Register a new user via the Keycloak Admin REST API, then return a token.</summary>
    Task<KeycloakTokenResponse> RegisterAsync(
        string username, string email, string password, string role = "Passenger");
}

// ── Implementation ─────────────────────────────────────────────────────────────

public class KeycloakService : IKeycloakService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    // Shorthand config helpers
    private string BaseUrl       => _config["Keycloak:BaseUrl"]!;
    private string Realm         => _config["Keycloak:Realm"]!;
    private string ClientId      => _config["Keycloak:ClientId"]!;
    private string ClientSecret  => _config["Keycloak:ClientSecret"]!;
    private string FrontendClientId => _config["Keycloak:FrontendClientId"]!;

    public KeycloakService(HttpClient http, IConfiguration config)
    {
        _http   = http;
        _config = config;
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    public async Task<KeycloakTokenResponse> LoginAsync(string email, string password)
    {
        // We use the public frontend client (no secret) with ROPC grant.
        // NOTE: ROPC is fine for development / school use but should be replaced
        // by an Authorization Code + PKCE flow in a real production frontend.
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"]  = FrontendClientId,
            ["username"]   = email,
            ["password"]   = password,
            ["scope"]      = "openid email profile"
        };

        return await PostTokenAsync(form);
    }

    // ── Register ──────────────────────────────────────────────────────────────

    public async Task<KeycloakTokenResponse> RegisterAsync(
        string username, string email, string password, string role = "Passenger")
    {
        var adminToken = await GetAdminTokenAsync();

        // 1. Create user
        var newUser = new
        {
            username    = username,
            email       = email,
            enabled     = true,
            emailVerified = true,
            credentials = new[]
            {
                new { type = "password", value = password, temporary = false }
            }
        };

        var createRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{BaseUrl}/admin/realms/{Realm}/users")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(newUser),
                Encoding.UTF8,
                "application/json")
        };
        createRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        var createResponse = await _http.SendAsync(createRequest);

        if (!createResponse.IsSuccessStatusCode)
        {
            var body = await createResponse.Content.ReadAsStringAsync();
            // 409 = username/email already exists
            if ((int)createResponse.StatusCode == 409)
                throw new InvalidOperationException(
                    "A user with that username or email already exists in Keycloak.");
            throw new InvalidOperationException(
                $"Keycloak user creation failed ({createResponse.StatusCode}): {body}");
        }

        // 2. Retrieve the new user's ID from the Location header
        var locationHeader = createResponse.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("Keycloak did not return a user location.");
        var newUserId = locationHeader.Split('/').Last();

        // 3. Assign role
        await AssignRealmRoleAsync(newUserId, role, adminToken);

        // 4. Return a token so the caller is immediately logged in
        return await LoginAsync(email, password);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtains a short-lived admin token using the confidential api client's
    /// client_credentials grant (service account).
    /// </summary>
    private async Task<string> GetAdminTokenAsync()
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"]    = "client_credentials",
            ["client_id"]     = ClientId,
            ["client_secret"] = ClientSecret
        };

        var response = await PostTokenAsync(form);
        return response.AccessToken;
    }

    private async Task AssignRealmRoleAsync(string userId, string roleName, string adminToken)
    {
        // Look up the role representation first (we need its id + name)
        var roleRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{BaseUrl}/admin/realms/{Realm}/roles/{roleName}");
        roleRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        var roleResponse = await _http.SendAsync(roleRequest);
        roleResponse.EnsureSuccessStatusCode();

        var roleJson = await roleResponse.Content.ReadAsStringAsync();

        // Assign the role to the user
        var assignRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{BaseUrl}/admin/realms/{Realm}/users/{userId}/role-mappings/realm")
        {
            Content = new StringContent(
                $"[{roleJson}]",
                Encoding.UTF8,
                "application/json")
        };
        assignRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        var assignResponse = await _http.SendAsync(assignRequest);
        assignResponse.EnsureSuccessStatusCode();
    }

    private async Task<KeycloakTokenResponse> PostTokenAsync(
        Dictionary<string, string> formValues)
    {
        var response = await _http.PostAsync(
            $"{BaseUrl}/realms/{Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(formValues));

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // Keycloak returns { "error": "...", "error_description": "..." }
            string? description = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                doc.RootElement.TryGetProperty("error_description", out var d);
                description = d.GetString();
            }
            catch { /* ignore */ }

            throw new UnauthorizedAccessException(
                description ?? $"Keycloak token request failed ({response.StatusCode}).");
        }

        return JsonSerializer.Deserialize<KeycloakTokenResponse>(body)!;
    }
}

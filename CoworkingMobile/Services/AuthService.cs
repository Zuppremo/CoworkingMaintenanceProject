using System.Net.Http.Json;
using System.Text.Json;

namespace CoworkingMobile.Services;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResult
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}

public class AuthService
{
    private readonly HttpClient httpClient;

    public AuthService(IHttpClientFactory httpClientFactory)
    {
        this.httpClient = httpClientFactory.CreateClient("Api");
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var loginModel = new LoginRequest { Email = email, Password = password };
            var response = await httpClient.PostAsJsonAsync("login?useCookies=false", loginModel);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResult>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result == null || string.IsNullOrWhiteSpace(result.AccessToken))
            {
                return false;
            }

            await SecureStorage.Default.SetAsync("authToken", result.AccessToken);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return false;
        }
    }

    public void Logout()
    {
        SecureStorage.Default.Remove("authToken");
    }

    public async Task<string?> GetTokenAsync()
    {
        return await SecureStorage.Default.GetAsync("authToken");
    }
}

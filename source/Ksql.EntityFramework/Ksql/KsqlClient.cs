using System.Text;
using System.Text.Json;

namespace Ksql.EntityFramework.Ksql;

/// <summary>
/// Client for interacting with KSQL server.
/// </summary>
internal class KsqlClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _ksqlServerUrl;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KsqlClient"/> class.
    /// </summary>
    /// <param name="ksqlServerUrl">The URL of the KSQL server.</param>
    public KsqlClient(string ksqlServerUrl)
    {
        _ksqlServerUrl = ksqlServerUrl ?? throw new ArgumentNullException(nameof(ksqlServerUrl));
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Executes a KSQL statement.
    /// </summary>
    /// <param name="ksqlStatement">The KSQL statement to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteKsqlAsync(string ksqlStatement)
    {
        var requestObject = new
        {
            ksql = ksqlStatement,
            streamsProperties = new Dictionary<string, string>
            {
                { "ksql.streams.auto.offset.reset", "earliest" }
            }
        };

        var requestJson = JsonSerializer.Serialize(requestObject);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{_ksqlServerUrl}/ksql", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new KsqlException($"Failed to execute KSQL statement. Status code: {response.StatusCode}. Error: {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"KSQL response: {responseJson}");
        }
        catch (HttpRequestException ex)
        {
            throw new KsqlException($"Failed to connect to KSQL server: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Disposes the client.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the client.
    /// </summary>
    /// <param name="disposing">Whether the method is being called from Dispose.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }

            _disposed = true;
        }
    }
}

/// <summary>
/// Exception thrown when a KSQL operation fails.
/// </summary>
public class KsqlException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KsqlException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public KsqlException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KsqlException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public KsqlException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
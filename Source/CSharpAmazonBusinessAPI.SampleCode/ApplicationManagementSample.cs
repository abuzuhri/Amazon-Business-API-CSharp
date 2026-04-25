using CSharpAmazonBusinessAPI;

namespace CSharpAmazonBusinessAPI.SampleCode;

public class ApplicationManagementSample
{
    private readonly AmazonBusinessConnection _connection;

    public ApplicationManagementSample(AmazonBusinessConnection connection)
    {
        _connection = connection;
    }

    // Triggers Amazon to send a new LWA client secret to the developer-registered SQS queue.
    // After picking up the new secret, call _connection.Credentials.RotateClientSecret(newSecret)
    // to swap it in place — the cached access token is invalidated automatically.
    public Task RotateClientSecretAsync() =>
        _connection.Applications.RotateApplicationClientSecretAsync();
}

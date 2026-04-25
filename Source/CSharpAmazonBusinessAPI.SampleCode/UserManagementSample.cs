using CSharpAmazonBusinessAPI;
using CSharpAmazonBusinessAPI.Model.UserManagement;

namespace CSharpAmazonBusinessAPI.SampleCode;

public class UserManagementSample
{
    private readonly AmazonBusinessConnection _connection;

    public UserManagementSample(AmazonBusinessConnection connection)
    {
        _connection = connection;
    }

    public Task<CreateBusinessUserAccountResponse> CreateBusinessUserAsync(
        string email, string givenName, string familyName, string groupId, BusinessRole role)
    {
        var request = new CreateBusinessUserAccountRequest
        {
            AccountHolder = new AccountHolder
            {
                Email = email,
                GivenName = givenName,
                FamilyName = familyName,
            },
            GroupId = new BusinessGroupIdentifier
            {
                IdType = BusinessGroupIdentifierIdType.GroupId,
                Id = groupId,
            },
            Role = role,
            Region = Region.US,
        };
        return _connection.Users.CreateBusinessUserAccountAsync(request);
    }
}

using SimpleAuthNet.Models;

namespace SimpleAuthNet;

public interface IPostRegistrationHandler
{
    Task HandleAsync(AppUser user, bool isFirstUser);
}

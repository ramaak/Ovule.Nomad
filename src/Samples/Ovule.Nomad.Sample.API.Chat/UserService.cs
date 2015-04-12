using System.Collections.Generic;
using System.Linq;

namespace Ovule.Nomad.Sample.API.Chat
{
  public class UserService
  {
    [NomadIgnore]
    private static IList<User> _signedInUsers = new List<User>();

    public static void SignIn(User user)
    {
      if (_signedInUsers.FirstOrDefault(u => u.Equals(user)) == default(User))
        _signedInUsers.Add(user);
    }

    public static void SignOut(User user)
    {
      User toRemove = _signedInUsers.FirstOrDefault(u => u.Equals(user));
      if (toRemove != default(User))
        _signedInUsers.Remove(toRemove);
    }

    public IList<User> GetSignedInUsers()
    {
      return _signedInUsers;
    }
  }
}

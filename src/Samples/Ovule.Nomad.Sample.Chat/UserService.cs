using System.Collections.Generic;
using System.Linq;

namespace Ovule.Nomad.Sample.Chat
{
  public class UserService
  {
    [NomadIgnore]
    private static IList<User> _signedInUsers = new List<User>();

    [NomadMethod(NomadMethodType.Repeat, true)]
    public static void SignIn(User user)
    {
      if (_signedInUsers.FirstOrDefault(u => u.Equals(user)) == default(User))
        _signedInUsers.Add(user);
    }

    [NomadMethod(NomadMethodType.Repeat, true)]
    public static void SignOut(User user)
    {
      User toRemove = _signedInUsers.FirstOrDefault(u => u.Equals(user));
      if (toRemove != default(User))
        _signedInUsers.Remove(toRemove);
    }

    [NomadMethod]
    public IList<User> GetSignedInUsers()
    {
      return _signedInUsers;
    }
  }
}

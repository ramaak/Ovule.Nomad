using System;

namespace Ovule.Nomad.Sample.API.Chat
{
  [Serializable]
  public class User
  {
    public Uri Uri { get; private set; }
    public string Id { get { return Uri == null ? "" : Uri.ToString(); } }

    public User(Uri uri)
    {
      this.ThrowIfArgumentIsNull(() => uri);

      Uri = uri;
    }

    public override string ToString()
    {
      return Id;
    }

    public override bool Equals(object obj)
    {
      return obj is User && ((User)obj).Uri == this.Uri;
    }

    public override int GetHashCode()
    {
      if (Uri == null)
        return base.GetHashCode();
      return Uri.GetHashCode();
    }
  }
}

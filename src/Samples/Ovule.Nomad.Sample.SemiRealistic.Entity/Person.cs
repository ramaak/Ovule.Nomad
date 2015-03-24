using System;

namespace Ovule.Nomad.Sample.SemiRealistic.Entity
{
  [Serializable]
  public class Person
  {
    public enum Salutation { Mr, Ms, Miss, Mrs, Dr, Prof }

    public string Forename { get; set; }
    public string Surname { get; set; }
    public Salutation? Title { get; set; }
    public DateTime? DateOfBirth { get; set; }
  }
}

using System;

namespace Ovule.Nomad.Sample.SemiRealistic.Entity
{
  [Serializable]
  public class Employee : Person
  {
    public enum StaffRole { Cleaner, Secretary, Accountant, Janitor, SalesRep, Manager }

    public int? EmployeeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public StaffRole? Role { get; set; }
    public decimal? Salary { get; set; }
    public string Notes { get; set; }

    public bool IsValid()
    {
      return
        !string.IsNullOrWhiteSpace(Forename) &&
        !string.IsNullOrWhiteSpace(Surname) &&
        Title != null &&
        DateOfBirth != null &&
        StartDate != null &&
        Role != null &&
        Salary.GetValueOrDefault(0) > 0;
    }
  }
}

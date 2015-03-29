using Ovule.Nomad.Sample.SemiRealistic.Data;
using Ovule.Nomad.Sample.SemiRealistic.Entity;
using System;

namespace Ovule.Nomad.Sample.SemiRealistic.Business
{
  public class EmployeeService
  {
    public int Save(Employee employee)
    {
      if (!employee.IsValid())
        throw new ArgumentException("The employee is invalid and cannot be saved");

      Console.WriteLine("Saving employee: {0} {1}", employee.Forename, employee.Surname);

      return new EmployeeDataAccess().Save(employee);
    }

    public Employee Get(int employeeId)
    {
      Employee employee = new EmployeeDataAccess().Get(employeeId);

      Console.WriteLine("Got employee: {0} {1}", employee.Forename, employee.Surname);

      return employee;
    }
  }
}

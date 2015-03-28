using Ovule.Nomad.Sample.SemiRealistic.Entity;
using Ovule.Nomad.Sample.SemiRealistic.Business;
using System;

namespace Ovule.Nomad.Sample.SemiRealistic.Client
{
  class Program
  {
    //see [assembly:NomadAssembly] in Ovule.Nomad.Sample.SemiRealistic.Business.AssemblyInfo
    static void Main(string[] args)
    {
      char userInput = 'z';
      int i = 0;
      do
      {
        Console.WriteLine("Saving and loading employee...");

        Employee employee = new Employee()
        {
          Title = Person.Salutation.Mr,
          Forename = "Fred" + ++i,
          Surname = "Jones" + i,
          DateOfBirth = new DateTime(1945, 8, 25),
          Role = Employee.StaffRole.Manager,
          Salary = 10000,
          StartDate = DateTime.Today
        };

        EmployeeService empServ = new EmployeeService();
        int employeeId = empServ.Save(employee);
        employee = empServ.Get(employeeId);

        Console.WriteLine("Saved & loaded employee: Id {0}, {1} {2} {3} [{4}], Notes: {5}",
          employee.EmployeeId, employee.Title, employee.Forename, employee.Surname, employee.Role, employee.Notes);

        Console.WriteLine("\r\nEnter '0' to quit, any other key to repeat the test\r\n");
        userInput = Console.ReadKey().KeyChar;
      } while (userInput != '0');
    }
  }
}

using Ovule.Nomad.Sample.SemiRealistic.Entity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ovule.Nomad.Sample.SemiRealistic.Data
{
  public class EmployeeDataAccess
  {
    public const string DataStorePath = @"c:\temp\EmployeeData\";

    private void CheckDatastoreAvaialble()
    {
      if (!Directory.Exists(DataStorePath))
      {
        string message = string.Format("Cannot find datastore directory.  Either create '{0}' or alter the source code", DataStorePath);
        Console.WriteLine(message);
        throw new Exception(message);
      }
    }

    public int Save(Employee employee)
    {
      CheckDatastoreAvaialble();

      employee.Notes = string.Format("Saved by process '{0}'", Process.GetCurrentProcess().ProcessName);
      if (employee.EmployeeId.GetValueOrDefault(0) <= 0)
        employee.EmployeeId = GetNextId();

      byte[] serEmp = Serialiser.SerialiseToBytes(employee);

      string filename = Path.Combine(DataStorePath, string.Format("{0}.emp", employee.EmployeeId.Value));
      File.WriteAllBytes(filename, serEmp);

      return employee.EmployeeId.Value;
    }

    public Employee Get(int employeeId)
    {
      CheckDatastoreAvaialble();

      string filename = Path.Combine(DataStorePath, string.Format("{0}.emp", employeeId));
      if (!File.Exists(filename))
        throw new InvalidOperationException(string.Format("No employee exists with id '{0}'", employeeId));

      byte[] serEmp = File.ReadAllBytes(Path.Combine(DataStorePath, filename));
      if (serEmp == null)
        throw new InvalidDataException(string.Format("Failed to read data for employee with id '{0}' from datastore", employeeId));

      Employee employee = (Employee)Serialiser.DeserialiseBytes(serEmp);
      return employee;
    }

    private int GetNextId()
    {
      CheckDatastoreAvaialble();

      IEnumerable<string> employeeFiles = Directory.EnumerateFiles(DataStorePath, "*.emp");
      if (employeeFiles != null && employeeFiles.Any())
      {
        int currMaxId = employeeFiles.Max((filename) =>
        {
          int id = -1;
          if (int.TryParse(Path.GetFileNameWithoutExtension(filename), out id))
            return id;
          return -1;
        });
        return ++currMaxId;
      }
      return 1;
    }
  }
}
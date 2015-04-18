using System.ServiceModel;

namespace Ovule.Nomad.Sample.API.NetPerformance.Shared
{
  [ServiceContract]
  public interface IMyMathsService
  {
    [OperationContract]
    int Add(int a, int b);

    [OperationContract]
    int Subtract(int a, int b);
  }
}

# News
2015-04-14
Major changes have been checked in.  Due to feedback Nomad is no longer a tool but a framework.  See sample code below and take a look at checked in sample projects.  Updated videos to follow soon.

Follow on Twitter: [@OvuleNomad](https://twitter.com/OvuleNomad)

# Ovule.Nomad [.Net]
Nomad is a framework which helps you to create efficient distributed applications.  Currently the main benefits it provides are: 

* The number of calls over the network can be reduced dramatically, with very little effort.
* There is support for distributed memory.
* Large workloads can be split across clusters/grids using an implementation of MapReduce with virtually no setup.
* It happily co-exists with other technologies such as WCF and so can be used in your existing projects.
* It allows for very rapid development of distributed systems.
* It is possible to fall-back into an "offline mode" if the network connection goes down.

Please be aware that Nomad is currently not stable and there is a lot of refinement and testing required before it can be deemed production worthy. 

If you feel you could help contribute towards this project in any way please get in touch.

#Security Warning
Under the hood Nomad uses WCF (by default) and so all security features of WCF are available.  Having said this, Nomad can be used for developing systems where remote nodes have no prior knowledge of the code they are going to execute.  This obviously places those nodes in a very dangerous position.  Security is being taken very seriously and v1.0 won't be released until safeguards are in place but in the meantime please be careful.  

#Hello World

```csharp 
class Program
  {
    static void Main(string[] args)
    {
      BasicRemoteMethodExecuter exec = new BasicRemoteMethodExecuter(new Uri("net.tcp://localhost:8557/NomadService"));

      //as the method name suggests, SayHello() is executed both on the local machine and remotely
      exec.ExecuteLocalAndRemote(() => SayHello());
      Console.ReadLine();
    }

    static void SayHello()
    {
      Console.WriteLine("Hello from process '{0}'!", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
    }
  }
```

#Basic Distributed Memory 

```csharp 
class Program
  {
    private static string ProcessName { get; set; }
    private static string _alphabet;

    static void Main(string[] args)
    {
      BasicRemoteMethodExecuter exec = new BasicRemoteMethodExecuter(new Uri("net.tcp://localhost:8557/NomadService"));

      _alphabet = "abcdefg";

      //when this method runs remotely it will see _alphabet with the value "abcdefg"
      exec.Execute(() => ProcessMemberVariables());
      Console.WriteLine("Process name is '{0}'.{1}Reversed alphabet is '{2}'", ProcessName, Environment.NewLine, _alphabet);
      Console.ReadLine();
    }

    static void ProcessMemberVariables()
    {
      //the caller will see ProcessName read as the remote host process name
      ProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

      //the caller will see this change to _alphabet
      _alphabet = new string(_alphabet.Reverse().ToArray());
    }
  }
```

#Basic Load Distribution

```csharp 
class Program
  {
    static void Main(string[] args)
    {
      Uri[] remoteUris = new Uri[] { 
        new Uri("net.tcp://localhost:8557/NomadService"), new Uri("net.tcp://localhost:8558/NomadService"), 
        new Uri("net.tcp://localhost:8559/NomadService"), new Uri("net.tcp://localhost:8560/NomadService")
      };
      ParallelRemoteMethodExecuter exec = new ParallelRemoteMethodExecuter(remoteUris);
      string[] corpus = File.ReadAllLines("TestCorpus.txt");

      //this will result in 'corpus' being split evenly and the parts being sent to 
      //each of the 4 Uri's shown above
      exec.DistributeArray<string>(PrintLines, corpus);

      Console.WriteLine("Done");
      Console.ReadLine();
    }

    static void PrintLines(string[] lines)
    {
      Console.WriteLine("Line Count: {0}", lines.Length);
      Console.WriteLine("1st line: {0}", lines[0]);
      Console.WriteLine("Last line: {0}", lines[lines.Length - 1]);
    }
  }
```

#Basic MapReduce

```csharp
public class CharCounter
  {
    private Uri[] _remoteUris;
    private string _corpusPath;
    private char _countChar;
    private int _corpusLength;

    public CharCounter()
    {
      _remoteUris = new Uri[] { 
        new Uri("net.tcp://localhost:8557/NomadService"), new Uri("net.tcp://localhost:8558/NomadService"),
        new Uri("net.tcp://localhost:8559/NomadService"), new Uri("net.tcp://localhost:8560/NomadService")
      };
    }

    public int Run(string corpusPath, char countChar)
    {
      _corpusPath = corpusPath;
      _countChar = countChar;
      _corpusLength = (int)new FileInfo(_corpusPath).Length;
      ParallelRemoteMethodExecuter exec = new ParallelRemoteMethodExecuter(_remoteUris);

      int result = 0;

      //GetRemoteJobPart will be called once per remote node with values like 1/4, 2/4, etc.
      //DistributeOperation sends each RemoteJob to a seperate node and captures all results
      int[] results = exec.DistributeOperation<int>(GetRemoteJobPart);

      //a further simple reduce to sum the char counts
      foreach (int res in results)
        result += res;
      return result;
    }

    private RemoteJob GetRemoteJobPart(int part, int of)
    {
      int blockSize = _corpusLength / of;
      int blockStart = (part - 1) * blockSize;
      if (part == of)
        blockSize = _corpusLength - blockSize;

      //this RemoteJob will be executed on one of the remote notes
      return new RemoteJob(() => MapReduce(_countChar, _corpusPath, blockStart, blockSize));
    }

    private int MapReduce(char countChar, string filePath, int startPos, int length)
    {
      int result = Reduce(countChar, Map(filePath, startPos, length));

      Console.WriteLine("Counted '{0}' occurences of '{1}'", result, countChar);
      return result;
    }

    private char[] Map(string filePath, int startPos, int length)
    {
      using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        using (StreamReader rdr = new StreamReader(fs))
        {
          char[] buffer = new char[length];
          fs.Position = startPos;
          int readChars = rdr.ReadBlock(buffer, 0, length);

          Console.WriteLine("Read '{0}' characters", readChars);
          return buffer;
        }
      }
    }

    private int Reduce(char searchChar, char[] chars)
    {
      int charCount = 0;
      foreach (char c in chars)
      {
        if (c == searchChar)
          charCount++;
      }
      return charCount;
    }
  }
```

#Videos
Due to recent changes the original videos are now out of date.  New ones will be made over the next few days (2015-04-14)  

#Potential 
There is huge future potential for Nomad above what's already been described.  Here are just a few use cases:

1.	Super Cheap "Super Computing": Create a cluster of Raspberry Pi's running Mono (or when Windows 10 is released to it). 

2.	Mobile Frameworks: It's currently possible to execute code both locally and remotely.  This offers the potential for falling into an "offline mode" when connections drop.   

3.	Peer-to-peer networking: Basic P2P features are already built in, see the Chat sample application.  Also see the Roadmap for hints on how this could be taken as far as having decentralised “processing marts” where nodes can bid for rights to execute operations on behalf of other nodes.

#Release Roadmap
What follows is the planned release roadmap – which is admittedly very ambitious given the level of resources (i.e. currently a single developer with very limited free time) available.  

Timescales cannot be given however it is likely to be at months rather than days before v1.0 is available.

#Release 0.8.0 – Available in Dev branch
This is the current preview release and includes all features described in the documentation and demonstrated in the samples.  It is not to be considered stable so should only be used for experimentation.

#Release v1.0 – (ETA: maybe a couple of months)
A complete review of the existing codebase is needed, refinement, unit testing to be caught up and extensive regression testing.  

#Release v1.1
The ability to pass parameters to nomadic methods by reference and to have updates to these references reflected on the client is to be completed.  

#Release v2.0
The P2P features (which are available in current version) will be expanded upon and routing functionality built in.  This will allow peers to effectively construct a decentralised network.  This routing facility and the allowance for dynamic execution would offer huge opportunities.   

#Release v3.0
The main feature here would be decentralised load balancing.  Each “server” node would advertise their capacity, predicted future capacity, willingness to accept additional load, etc.

#Release v4.0
To be honest if I get past v1.0 alive I'll be happy!!

#3rd Party Components Nomad Uses

Mono.Cecil - https://github.com/jbevain/cecil

A great library! If Mono.Cecil wasn't arount it would have easily taken twice as long to get this project to where it is.

AE.Net.Mail - https://github.com/andyedinborough/aenetmail

This isn't a core component of Nomad but is used with an implementation of a Nomad client and server which communicate over email.  This implementation is more for fun and a demonstration that Nomad is flexible enough cope with unusual demands.


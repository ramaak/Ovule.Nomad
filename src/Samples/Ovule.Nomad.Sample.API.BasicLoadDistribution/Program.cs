﻿using Ovule.Nomad.Client;
using System;
using System.IO;

namespace Ovule.Nomad.Sample.API.BasicLoadDistribution
{
  class Program
  {
    static void Main(string[] args)
    {
      Uri[] remoteUris = new Uri[] { 
        new Uri("net.tcp://localhost:8557/NomadService"), new Uri("net.tcp://localhost:8558/NomadService"), 
        new Uri("net.tcp://localhost:8559/NomadService"), new Uri("net.tcp://localhost:8560/NomadService")
      };
      ParallelRemoteMethodExecuter exec = new ParallelRemoteMethodExecuter(remoteUris);

      string[] corpusLines = File.ReadAllLines("TestCorpus.txt");
      exec.For<string>(0, corpusLines.Length, corpusLines, PrintLines);

      Console.WriteLine("Done");
    }

    static void PrintLines(string[] lines)
    {
      Console.WriteLine("Line Count: {0}", lines.Length);
      Console.WriteLine("1st line: {0}", lines[0]);
      Console.WriteLine("Last line: {0}", lines[lines.Length-1]);
    }
  }
}
# News

2015-04-11
After collating feedback on the project (which was the main purpose of publishing the preview) some fairly big changes are now underway.  Rather than being a tool Nomad will become a framework, which will give developers more flexibility and also make them feel more in control.  To enjoy the main benefits of this change the roadmap is going to change a bit.  What was planned for release v1.1 is no longer required but features for release v1.5 and v2.0 will be brought forward to the first release.  Things aren't too far from here as it stands.  This page will be updated soon to reflect the changes and a paper is planned.

Follow on Twitter: [@OvuleNomad](https://twitter.com/OvuleNomad)

# Ovule.Nomad [.Net]
With Nomad distributed application code is cleaner and leaner, easier to write, understand and maintain.  In addition to this Nomad can demonstrably increase overall distributed application performance while at the same time allowing for code reuse maximisation.

Nomad is not a framework, it is a tool which enforces no constraints on developers.  In the most basic use-case you can convert a single assembly with a single type into a distributed application (see the Hello World and P2P videos below).  More typically you will develop well-structured applications consisting of a number of assemblies and choose which parts (assemblies, types, methods) you want to execute remotely (see Nomad Assemblies video below).  

With Nomad you develop applications as if they were standalone and then pass them through the Nomad Processor.  It creates a server for you from the aspects you choose to execute remotely and a client configured to communicate with it.  In addition to this, since you develop the application as if it were standalone it's possible to fall-back into standalone mode should network connectivity fail, making it perfectly suited for occasionally-connected applications.  If the methods you want to execute remotely access class level properties or fields this is no problem, Nomad works these things out for you keeping your data consistent.

The overall number of network calls can be dramatically reduced with Nomad, with much less effort than with traditional means of distributed systems development while at the same time keeping your code base very lean.  If you find a client side method is making a number of trips to the server you can very easily make that method run on the server too, meaning only a single network transaction is required (see the "Chatty and Chunky" video link below).  You'll never have to craft non-functional code purely to reduce the number of server trips. 

Nomad comes with a practically flat learning curve and there is very little needed in the way of instructions.  However, many developers will rightly want to understand what is going on under the hood and so documentation is needed.  Information will be added to the Wiki over the coming weeks in preparation for the first formal release.

Please be aware that Nomad is currently available only for preview purposes.  There is still work to be done before it reaches version 1.0 and therefore shouldn’t be used in applications intended for end-users. 

If you feel you could help contribute towards this project in any way please get in touch.  Extensive testing is required in order to take the project to the v1.0 stage and the more black-box testers there are the merrier ;)    

#What's so different to RPC, Web Services, etc?

Nomad does have similarities to RPC technologies like .Net Remoting and web services technologies like WCF.  In reality it is both...and neither.  It's even easier to use than Remoting, you have the security and performance of WCF and also it can be extended upon even more, meaning you’re not limited to protocols like HTTP, TCP, Named Pipes and UDP.   

Nomad approaches things from a slightly different angle to the norm.  Normally with distributed applications an instance of an object lives within a single process.  You may be able to access the object from a seperate process and make requests of it however you won't have some methods within the same instance exeucting in process X and others executing within process Y.  You may be asking yourself, "Why on earth would I want to do that!?".  Well, a common compromise that must be made is that of reusability versus performance.  For optimimum reusability we really want our server to expose lots of small methods which we can use in many different scenarios, where we call them in different combinations.  This obviously isn't great for performance as each time we make a call over the network it comes at a price.  For optimum performance we want chunky methods on the server that perform a lot of work per network transaction. This isn't good for reusability as these large tasks are likely only useful in one or two situations.  Another unfortunate aspect of this "chunky" design is that we're writing methods that serve no functional purpose.  They add no value to our types however they are costing us time/money to develop.

With Nomad this compromise doesn't have to be made.  If you want a client side method to make multiple calls to a server you can just decorate it with a [NomadMethod] attribute and this means it'll execute remotely too (even though other methods on the object don't).  There will be only one network transaction.  If your client side method calls other methods or accesses class member variables this is absolutely fine as Nomad will manage this for you.  To your client side process it will appear that the method executed locally even though all work was carried out on the server.  You no longer have to write code just to reduce the number of network connections and therefor your codebase is cleaner, less bloated and easier to understand and maintain.

You don't have to make the decision to use Nomad up front - it's not a framework.  An assembly, type or method that you write today may not benefit from the features offered by Nomad.  If however the story's different tomorrow then just decorate the assembly, type or method with the appropriate Nomad attribute and you’re done.  Once the attribute is in place the rest is handled for you.

#Hello World

This is as simple as it get's!  Main(...) is executed locally and SayHello() on a server.  Download the samples from the respository and view the videos for more interesting examples.

```csharp 
class Program
{
  static void Main(string[] args)
  {
    SayHello();
  }

  //because of this attribute the method will execute remotely
  [NomadMethod] 
  static void SayHello()
  {
    Console.WriteLine("Hello from process '{0}'!", Process.GetCurrentProcess().ProcessName);
  }
}
```
#Forum

http://ost.io/@tony-dinucci/Ovule.Nomad

#Videos

The following (very rushed) videos have been made available.  Hopefully they give you a reasonable idea of this projects potential.  More to follow in due course.

1: First Steps - https://youtu.be/T0_OzOTGGVc

2: Hello World - https://youtu.be/w6gn6q2Rpeg

3: Member Variables - https://youtu.be/EKuMy4-OJWM

4: Nomad Assemblies - https://youtu.be/SnNpfZM3Lxo

5: P2P Part 1 - https://youtu.be/7mUOyC2YB1c

   P2P Part 2 - https://youtu.be/5rg8CdcZdCE
   
   P2P Part 3 - https://youtu.be/bF0GFtpQkUw
   (Please note, that unless you update the configuration files - as per WCF specification - you will only be able to send files under 64KB)
   
6: Chatty and Chunky - https://youtu.be/8WiLAJ3ufj4

#Potential 
There is huge potential for Nomad above what's already been described.  Here are just a few use cases:

1.	Super-Cheap Super-Computing: There are already frameworks in existence for distributed execution, they all however require developers to adhere to a framework and therefore come with considerable cost (at least in terms of time to learn and being locked into the framework afterwards). Nomad is not a framework and distributed execution can be acheived easily at at an extremely low cost.  In addition to this, there will be opportunities such as building a farm of Raspberry Pi’s.

2.	Mobile Frameworks: It is possible for the client to execute nomadic methods itself if it chooses to, instead of asking a server to execute them.  This means mobile frameworks could be developed quite easily.  For example, code can be written to write to a form of data store that can live on the client and server.  If the client is online then it executes the method on the server, if it goes offline it executes the method on itself.  Obviously data synchronisation would have to be implemented separately.

3.	Peer-to-peer networking: Basic P2P features are already built in, see the Chat sample application.  Also see the Roadmap for hints on how this could be taken as far as having decentralised “processing marts” where nodes can bid for rights to execute operations on behalf of other nodes.

#Release Roadmap
What follows is the planned release roadmap – which is admittedly very ambitious given the level of resources (i.e. currently a single developer with very limited free time) available.  

The fundamental aim  of this project is that writing distributed software should be as easy as it is to write standalone software.  None of the features developed for any Nomad release will affect the way you write software.  A certain amount of configuration (outwith application code) may be required for certain features but simplicity will always be the goal.  

Timescales cannot be given however it should not be too long before version 1.5 is available.  Each major version after this may take considerable time unless additional resources can be sourced.

#Release 0.7.1 – Available in Dev branch
This is the current preview release and includes all features described in the documentation and demonstrated in the samples.  It is not to be considered stable so should only be used for experimentation.

#Release v1.0 – (ETA: fairly soon)
A complete review of the existing codebase is needed, unit testing to be caught up and extensive regression testing (with MS .Net and Mono).  Some improvements to the Processor GUI are planned made however no plans for new features.

#Release v1.1
The processor backend and GUI will be enhanced so that users have a visual representation of the assemblies, types and methods that their application consists of.  Through the GUI they will be able to choose which aspects to make nomadic.  This will remove the need for the current [NomadMethod], [NomadType] and [NomadAssembly] attributes so developers won’t be required to reference any Nomad assemblies.  The big benefit here is that Nomad will impose no requirements on developers and is therefore purely a tool.  This also offers the opportunity to use Nomad with programs where the source code is unavailable – it will be up to the developer to ensure they are not breaking any contracts/laws by doing this!

#Release v1.2
The ability to pass parameters to nomadic methods by reference and to have updates to these references reflected on the client is to be completed.  Also the option to make the client aware of events that fire in nomadic methods will be provided.

#Release v1.5
The main enhancement planned for this release is assembly pruning.  Currently the assembly that’s generated for placement on a server may contain a lot of code that will never be required.  When pruning has been fully implemented this assembly will contain only elements which are useful, reducing the overall server assembly size (potentially dramatically depending on the use case).  Apart from the obvious benefit this will also help with a later feature – which is to allow for nomadic methods to be sent to servers dynamically (as opposed to the server having a static copy of all code it can execute).

#Release v2.0
The ability to dynamically submit nomadic methods to servers will be provided.  This obviously opens a large can of worms in terms of security.  It is currently envisaged that servers will advertise the amount of risk they are willing to take.  One server may choose to disallow any dynamic code while another may allow anything bar registry access.  

#Release v3.0
The P2P features (which are available in current version) will be expanded upon and routing functionality built in.  This will allow peers to effectively construct a decentralised network.  This routing facility and the allowance for dynamic execution would offer huge opportunities.  Just one possibility is a “processing mart” where peers send requests to execute operations through the network and peers bid for execution rights.  

The use of the language here should not be taken to mean that the end goal is to remove traditional servers completely.  An organisation may want to have a collection of high-spec servers which execute all nomadic methods and this routing functionality is a stepping stone towards decentralised load balancing.

#Release v3.5
The main feature here would be decentralised load balancing.  Each “server” node would advertise their capacity, predicted future capacity, willingness to accept additional load, etc.
This again takes things a step closer to having a “processing mart” however the immediate use would be load balancing within a controlled environment.  

Just one opportunity this functionality allows is for super-cheap-super-computing.  With Windows moving to the likes of the Raspberry Pi it will be possible to build a “processing farm” from a collection of cheap Pi’s.

#Release v4.0
Let’s wait and see!!

#3rd Party Components Nomad Uses

Mono.Cecil - https://github.com/jbevain/cecil

A great library! If Mono.Cecil wasn't arount it would have easily taken twice as long to get this project to where it is.

AE.Net.Mail - https://github.com/andyedinborough/aenetmail

This isn't a core component of Nomad but is used with an implementation of a Nomad client and server which communicate over email.  This implementation is more for fun and a demonstration that Nomad is flexible enough cope with unusual demands.


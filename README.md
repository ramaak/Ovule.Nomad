# Ovule.Nomad
There are a huge number of reasons to want to develop distributed applications but development productivity is not one of them.  The plumbing required and constraints placed on architects and developers make it an arduous, costly, complex and error prone task.  As a result no organisation would ever choose to develop a distributed application unless there was a compelling reason to do so.

Nomad allows you to develop distributed software in exactly the same way you develop standalone applications.  Application code is cleaner and leaner, easier to write, understand and maintain.  In addition to this Nomad can demonstrably increase overall system performance while at the same time allowing for code reuse maximisation.

Nomad is not a framework.  It is a tool that processes compiled .Net programs and converts them to efficient distributed applications.   

Please take a look at the demonstration videos to see it in action.

Be aware that Nomad is currently available only for preview purposes.  There is still work to be done before it reaches version 1.0 and therefore shouldn’t be used in applications intended for end-users. 

Nomad comes with a practically flat learning curve and there is very little needed in the way of instructions.  However, many developers will rightly want to understand what is going on under the hood and so documentation is needed.  Information will be added to the Wiki over the coming weeks in preparation for the first formal release.

If you feel you could help contribute towards this project in any way please get in touch.  Extensive testing is required in order to take the project to the v1.0 stage and the more black-box testers there are the merrier ;)    

#Simplicity
Most introductions to programming topics start off with an example of some form of “Hello World”.  To create a standalone console application that prints “Hello World” you can write a few short lines of code and be finished in seconds.  To create a networked version of “Hello World” you will need to write at least 10’s of lines of code, create multiple classes and/or interfaces and also potentially spend time on configuration.  Rather than taking seconds your application may now have taken the best part of an hour.  Also since it’s much more complex it’s harder to understand and there’s an increased risk of bugs.

Using Nomad your networked “Hello World” application (and indeed every other application) looks just like the standalone version.

#Performance & Reuse
Typically a trade-off between performance and reuse must be made when developing distributed software.  

If you develop a “chatty” system you can potentially enjoy a high level of operation reuse.  Operations are small units of work that may be useful in many situations.  The concept is a very good one and a central tenat of the philosophy behind Unix and later operating systems.  Unfortunately this principal comes with a very negative side-effect when speaking about distributed software.  Every network connection comes with a cost and therefore a chatty distributed system suffers performance-wise.

If you develop a “chunky” system you can potentially enjoy a high level of performance.  Operations are large units of work and what would cost multiple network transactions for a chatty system would cost only a single network transaction with a chunky design.  However there will be many fewer scenarios where a large operation can be reused and so much more code must be written.

With Nomad you don’t have to make a trade.  Code can be written according to the best OO practices, allowing for maximum reuse while at the same time network transactions can be kept to the minimum.

#Potential 
There is a massive number use cases where Nomad could be used to simplify systems development.  Here are just a few:

1.	Super-Cheap Super-Computing: There are already frameworks in existence for distributed execution, they all however require developers to adhere to a framework and therefore come with considerable cost (at least in terms of time to learn and being locked into the framework afterwards). Nomad is not a framework and distributed execution can be acheived easily at at an extremely low cost.  In addition to this, there will be opportunities such as building a farm of Raspberry Pi’s when Windows 10 arrives on that platform.

2.	Mobile Frameworks: It is possible for the client to execute nomadic methods itself if it chooses to, instead of asking a server to execute them.  This means mobile frameworks could be developed quite easily.  For example, code can be written to write to a form of data store that can live on the client and server.  If the client is online then it executes the method on the server, if it goes offline it executes the method on itself.  Obviously data synchronisation would have to be implemented separately.

3.	Peer-to-peer networking: Basic P2P features are already built in, see the Chat sample application.  Also see the Roadmap for hints on how this could be taken as far as having decentralised “processing marts” where nodes can bid for rights to execute operations on behalf of other nodes.

#Release Roadmap
What follows is the planned release roadmap – which is admittedly very ambitious given the level of resources (i.e. free time and developers) available.  

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
This isn't really core component of Nomad but is used with an implementation of a Nomad client and server which communicates over email.  The implementation is more meant for fun and a demonstration that Nomad is flexible enough cope with unusual demands.


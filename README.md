###News
2014-04-19 - Nomad is now compatible with Mono and runs on Linux - thanks BlackCentipede.  
N.B. It's not yet been verified to work with mixed OS networks.  This will be tested/sorted very soon.

###Ovule.Nomad [.Net] [![Build status](https://ci.appveyor.com/api/projects/status/kocanm4n512cid52/branch/dev?svg=true)](https://ci.appveyor.com/project/tony-dinucci/ovule-nomad/branch/dev)

Nomad is an easy to use .Net distributed execution framework which also supports distributed memory.  Currently the main features are: 

* Highly optimised connection utilisation - what would typically involve multiple network trips can be acheived with a single trip using Nomad.  It can also slot nicely into existing programs without you needing to refactor.
* Distributed memory model which can be maintained in stateless environments (good for reliability especially when failover is necessary).
* Large workloads can be split across clusters/grids using  - MapReduce and other algorithms supported with virtually no setup.
* Fault tolerance measures are available such as failover and retry.
* It happily co-exists with existing technologies such as WCF and so can be used in existing projects.
* It allows for very rapid development of distributed systems.
* It is possible to fall-back into an "offline mode" if the network connection goes down.

Please be aware that Nomad is currently not stable and there is still a lot of refinement, optimisation and testing required before it can be deemed production worthy. The code has been released at this early stage so that community feedback can be gathered, helping to move it forward in the most positive direction.  

I love to hear feedback, positive or negative - constructive criticism is what is actually the most valuable to the project at this stage!  Please get in touch if there's anything you'd like to say.  Also, if you feel you could help with this project in any way please let me know.

###What/Why?
In the simplest sense Nomad lets you distribute the execution of code across multiple processes (typically on remote machines but not necessarily). Not only is code execution distributed but memory is too, meaning class member fields and properties are kept synchronised across disparate processes.  To the master process it will appear as if all code executed within it, even though it may have been spread across many machines.    

It can be used to create anything from simple multi-process applications running on a single machine up to high performance, horizontally scalable, fault tolerant distributed systems running on clusters or grids of an arbitrary size (in theory, it's not yet been tested on massive clusters/grids - if you have one going spare get in touch!)

In addition to allowing for easy distributed execution it can be used to very efficiently keep the number of network transactions under control.  A typical design trade-off with networked applications is flexibility/reusability versus performance.  If a remote process offers up lots of small operations then this is very flexible because external processes can consume those small operations in many different ways and combinations.  This isn't particularly efficient in terms of network utilisation though as there will be a lot of chatter over the network.  Remote processes that facilitate the execution of large operations are typically not very flexbible however network utlisation is much more efficient.  With Nomad this trade-off doesn't have to be made.  It takes a novel approach where the caller can decide which operations they want to execute remotely, meaning many small method calls can be bundled into a single larger distributed call.  Since Nomad can happily co-exist with other technologies it can be used purely as a way to optimise existing systems. If you have a method which is making a number of network calls then just execute with Nomad, which will move the execution of it entirely to the remote machine meaning there's only one network transaction (see an example of this in the [Wiki](https://github.com/tony-dinucci/Ovule.Nomad/wiki)).  

###Security Warning
Under the hood Nomad uses WCF (by default) and so all security features of WCF are available.  Having said this, Nomad can be used for developing systems where remote nodes have no prior knowledge of the code they are going to execute.  This obviously places those nodes in a dangerous position.  Security is being taken very seriously and v1.0 won't be officially released until safeguards are in place but obviously in the meantime don't put it into production.  

###Potential 
There is huge future potential for Nomad above what's already been described.  Here are just a few use cases:

1.	Super Cheap "Super Computing": Create a cluster of Raspberry Pi's running Mono (or when Windows 10 is released to it). 

2.	Mobile Frameworks: It's currently possible to execute code both locally and remotely.  This offers the potential for falling into an "offline mode" when connections drop.   

3.	Peer-to-peer networking: Basic P2P features are already built in, see the Chat sample application.  Also see the Roadmap for hints on how this could be taken as far as having decentralised “processing marts” where nodes can bid for rights to execute operations on behalf of other nodes.

###Release Roadmap
What follows is the planned release roadmap – which is admittedly very ambitious given the level of resources (i.e. currently a single developer with very limited free time) available.  

Timescales cannot be given however it is likely to be at months rather than days before v1.0 is available.

#####Release 0.8.0 – Available in Dev branch
This is the current preview release and includes all features described in the documentation and demonstrated in the samples.  It is not to be considered stable so should only be used for experimentation.

#####Release v1.0 – (ETA: maybe a couple of months)
A complete review of the existing codebase is needed, refinement, unit testing to be caught up and extensive regression testing.  

#####Release v1.1
The ability to pass parameters to nomadic methods by reference and to have updates to these references reflected on the client is to be completed.  

#####Release v2.0
The P2P features (which are available in current version) will be expanded upon and routing functionality built in.  This will allow peers to effectively construct a decentralised network.   

#####Release v3.0
The main feature here would be decentralised load balancing.  Each node would advertise their capacity, predicted future capacity, willingness to accept additional load, etc.

###3rd Party Components Nomad Uses

Mono.Cecil - https://github.com/jbevain/cecil

A great library! If Mono.Cecil wasn't arount it would have easily taken twice as long to get this project to where it is.

AE.Net.Mail - https://github.com/andyedinborough/aenetmail

This isn't a core component of Nomad but is used with an implementation of a Nomad client and server which communicate over email.  This implementation is more for fun and a demonstration that Nomad is flexible enough cope with unusual demands.


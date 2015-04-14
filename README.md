###News
2015-04-14
Fairly major changes have been made.  Due to feedback Nomad is no longer a tool but a framework.  See sample code below and take a look at checked in sample projects.  Updated videos to follow soon.

A start has been made on the Wiki!  Please take a look at the examples [here](https://github.com/tony-dinucci/Ovule.Nomad/wiki)

Follow on Twitter: [@OvuleNomad](https://twitter.com/OvuleNomad)

###Ovule.Nomad [.Net]
Nomad is an easy to use .Net distributed execution framework which also supports distributed memory.  Currently the main benefits it provides are: 

* The number of calls over the network can be reduced dramatically, with very little effort.
* There is support for distributed memory.
* Large workloads can be split across clusters/grids using an implementation of MapReduce with virtually no setup.
* It happily co-exists with other technologies such as WCF and so can be used in your existing projects.
* It allows for very rapid development of distributed systems.
* It is possible to fall-back into an "offline mode" if the network connection goes down.

Please be aware that Nomad is currently not stable and there is a lot of refinement, optimisation and testing required before it can be deemed production worthy. 

I love to hear feedback, positive or negative - constructive criticism is what is actually the most valuable for me at this stage!  Please get in touch if there's anything you'd like to say.  Also, if you feel you could help with this project in any way please give me a shout.

###What/Why?
In the simplest sense Nomad lets you distribute the execution of code across multiple processes (typically on remote machines but not necessarily). Not only is the code executed remotely but it's executed within the context of the original process, i.e. memory looks the same to both processes (class member fields, properties, etc.) and both are free to modify all memory.  Once execution of the remote code completes the local process context is synchronised so that things appear as if all code executed locally.  Context synchronisation is kept efficient by only considering memory that can possibly be touched by the code being distributed.

The types of application that can be developed range from basic client/server or P2P applications to massively distributed systems using MapReduce to spread workload effectively over a large number of machines.  See the code snippets below for an example of how a distributed MapReduce system can be achieved with a single class.

In addition to allowing for easy distributed execution it can be used to very efficiently keep the number of network transactions under control.  If you want to execute a number of methods on some remote machine then you can just group these and execute them with a single trip over the network.  You can achieve the reusability of a chatty server interface but with the performance of a chunky one.  

###Security Warning
Under the hood Nomad uses WCF (by default) and so all security features of WCF are available.  Having said this, Nomad can be used for developing systems where remote nodes have no prior knowledge of the code they are going to execute.  This obviously places those nodes in a very dangerous position.  Security is being taken very seriously and v1.0 won't be released until safeguards are in place but in the meantime please be careful.  

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
The P2P features (which are available in current version) will be expanded upon and routing functionality built in.  This will allow peers to effectively construct a decentralised network.  This routing facility and the allowance for dynamic execution would offer huge opportunities.   

#####Release v3.0
The main feature here would be decentralised load balancing.  Each “server” node would advertise their capacity, predicted future capacity, willingness to accept additional load, etc.

#####Release v4.0
To be honest if I get past v1.0 alive I'll be happy!!

###3rd Party Components Nomad Uses

Mono.Cecil - https://github.com/jbevain/cecil

A great library! If Mono.Cecil wasn't arount it would have easily taken twice as long to get this project to where it is.

AE.Net.Mail - https://github.com/andyedinborough/aenetmail

This isn't a core component of Nomad but is used with an implementation of a Nomad client and server which communicate over email.  This implementation is more for fun and a demonstration that Nomad is flexible enough cope with unusual demands.


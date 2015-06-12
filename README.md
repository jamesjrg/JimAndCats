#Jim and Cats

Nominally this repository is a bunch of web services providing a RESTful API for a fictional crowd funding website.

More practically this is a playground for trying out different approaches to problems, and for trying out various technologies and techniques to improve my knowledge.

##F# and functional programming style

1. The system is written entirely in F#, demonstrating that F# works well not only for complex logic and mathematical problems, for also for web endpoints, unit tests, build systems, database access, etc. I have endeavoured to avoid explicit .NET classes altogether and do everything with functions, which has been greatly aided by the [Suave](www.suave.io) web server, which uses combinators rather controller classes. As part of this I wrote a substantial amount of the introductory documentation on the Suave website, though I'm still a long way from being as comfortable with F# as I am with C#.

#CQRS and event sourcing

These are terms which seem to mean very different things to different people. Some of the differences I have seen in implementations are:

a) Write models and inconsistency

-> One very simple option is to simply assume that commands are always accepted without regard to business rules, and to deal with all states that break business rules after-the-fact, rather than trying to keep all the data consistent all of the time. For certain types of command this is perfectly reasonable, but to say that this has to be true for literally every single command may be problematic.

-> Another very simple option is by making everything entirely consistent for a given aggregate type or even for a given tenant by sharding servers by aggregate type or by tenant, and then ensuring both reads and writes are always handled by the same server. This is perhaps too boring and untrendy, but I'm sure there are many cases where more complex designs are unnecessary.

-> The model that aligns with Domain Driven Design is to split up the domain into aggregate roots, each of which must always be consistent in itself, but to deal with inconsistencies involving multiple aggregates in an eventual manner, as above. If persisting an event would lead to an invalid aggregate root then the command does not take place. Often, though not necessarily, a command is always considered invalid if anyone else has edited the aggregate since the user last saw the item - for instance, a wiki page may not let you save your page edits until you refresh the page with someone else's recent edits. Alternatively the system may automatically try replaying the command after reloading the current state of the aggregate, though this will still hinder performance.

Care must be taken when deciding on aggregate root boundaries. If the aggregates are too small then there are very few rules that can be enforced, which has the same end result as the previous option. But if aggregates are too large then managing concurrency is much more difficult, because multiple users will often be editing the same aggregate, resulting in exceptions when a command handler attempts to persist the new events, and then either requiring the user to retry their action or otherwise automatically retrying it for them.

-> One possibility is for the command handler to use the eventually consistent read-model as a first line of defence against inconsistent states, with eventual-consistency checks used only to mop up those inconsistencies that still get through. This is a bit messy, but I suspect many (perhaps most) real world event sourced systems systems end up using this is in some way. For instance the UI may not allow creating an item with the same name as another item, even if the list of current item names is only eventually consistent and duplicate names may still sometimes occur.

b) Read models

-> If aggregates are kept fairly small, and real-time queries only ever take place against a single aggregate at a time, then queries can be served by reconstituting aggregates directly from the event stream without need for a separate read model. Even if queries require access to multiple aggregates, the use of a FIFO cache may still allow queries to be served directly from event streams, rather than requiring the manual management of keeping a read model up to date. Things like search can be provided by pre-existing tools like Elasticsearch, and so even then an explicit separate read model is still unnecessary.

-> If real-time read model queries are complex and need access to data from numerous aggregates, but the total size of all the data required to serve read-model queries is fairly small - i.e. it fits in a few gigabytes - then it is perfectly plausible to keep the read model(s) entirely in RAM. This also makes for a pleasingly simple architecture, because queries are served using plain old objects, and there is no need to maintain a separate database to the main event stream. This does however require a well-thought-out approach for failover for when servers restart and the read models need to be rebuilt.

-> One project I worked on wrote all events for a given aggregate type to the same stream and then had a listener than watch for events and use them to maintain a single SQL Server database of the current state of the world. To me this rather defeats the point of event-sourcing - if you ultimately serve all queries from a single RDBMS anyway then event sourcing is acting as nothing more than a persistent message bus and a write-ahead log for the database. It does make it easier to replay past data, and it does remove the need for a separate message queue when updating other services about changes, but to me it seems a very complex pattern to use soley for these reasons. IMHO SQL read models should only be used for certain specialist purposes such as searching for violations of business rules across multiple aggregate roots, or providing queryable reporting functionality, not as the main source of data for clients.

c) Communication between services

-> xxx TODO every service maintains its own relatively copy of the state relevant to it- makes complex things simpler but at the expense of making simple changes much more difficult than when using a shared database

-> xxx TODO Technolgoies like EventStore have the nice property that they act as both the canonical source of persisted events and as something that other services can subscribe to for updates, and therefore at least in principle they can replace complex architectures that tru to keep SQL databases and message queues in sync, with a single, simpler technology. But I think in reality most businesses will have demands that will require the use of messaging queues and relational databases anyway, so EventStore may end up adding even more complexity, rather than reducing it. 

d) What I did here

The services here started out by using a single event stream per aggregate type, and then building different in-memory read models of the state of the world in different services (using EventStore projections to combine multiple aggregate types into a single in-memory model where necessary). Initially all commands were allowed to succeed all of the time. I then started toyed with enforcing rules across a certain aggregate type (e.g. unique names) by only allowing one write per aggregate type at any one time. Despite being uncool I suspect that this would actually work for a lot of real systems - a single async agent responsible for a single aggregate type can still handle a fairly high throughput.

I then rewrote the services to store everything in SQL Server rather than in RAM, having been badly influenced by my workplace at the time, but it was a lot of work for little benefit.

I'm now some way into rewriting everything to use fairly small aggregate roots with a separate stream for each one. Simple diagnostic queries are served directly from EventStore, whilst more complex queries will use EventStore projections combined with some sort of FIFO cache like Redis. SQL Server is used only to search for aggregates with duplicate names or email addresses.

##Authentication/authorization and microservices

I have spent some time reading about and toying with different approaches to managing centralized authorization for a set of web services, again currently still a work in progress as I try out various options.

///

If this was a real live project with lots of developers each project would be in separate Visual Studio solutions, but as a personal experiment it's easier to just keep them all together. Below is a brief description of each service, though this document is not always up-to-date:

####Jim.CommandHandler: Just Identity Management Command Handler

* Manages commands relating to user authentication details and basic identity info (i.e. people's full name).

* Commands result in events being written to a private stream in an Event Store cluster. An Event Store projection takes the private identity events resulting from commands and maps them to new events on a public stream for use by other services (currently the Cats service). All data and events relating to password hashes are omitted.

* The service may be scaled horizontally - if multiple edits are made to the same user at the same time then the most recent change always wins. Both loading and saving of entities happens via soley via Event Store, though to optimistically prevent users being created with duplicate email addresses a database table lookup is also required.

* Because different servers may create users at the same time, before the table for checking duplicates is updated, it may sometimes happen that two users are simultaneously created with the same email address. Adding logic to deal with this is still TODO.

####Jim.DuplicateCheckingUpdater: Just Identity Management Duplicate Checking Updater

* Listens to the private Jim event stream and uses it to update a SQL Server table for use by the Jim command query services. There can only be one running instance to avoid conflicting writes to the read model.

####Jim.QueryHandler: Just Identity Management Query Handler

* Verifies auth tokens for other services (though this is only half-way implemented at the moment)

* Also allows admins to query user details (currently all users are admins...).

* All queries are currently served by reconstituting entities directly from events. In a large scale system there might well be some sort of caching strategy, be it a manually maintained in-memory map, a distributed in-memory cache, or a database of some variety.

####Cats.CommandHandler: Crowdfunding Ask Templates Command Handler

* Manages a collection of projects asking for crowdfunding.

* Currently it can trivially be horizontally scaled across multiple instances without worrying about eventual consistency because there is no command that will get the system into an illegal state, even when the aggregate checked by a command handler is out of date.

####Cats.QueryHandler: Crowdfunding Ask Templates Command Handler

###There are also some shared libraries:

####GenericErrorHandling

* Generic types for representing successes and failures

####EventStore.YetAnotherClient

* Yet another EventStore client, built on top of the official .NET TCP client.

####Suave.Extensions

Some handy utilities for making Suave web services.

####TestingHelpers

Some utility methods using the official Suave.Testing library for writing tests on web server endpoints, as well as some minimal BDD helper functions for testing domain logic.

###Other:
Most of the projects have an associated unit test project. These do not require access to a real EventStore instance, and run any required web server in-process.

There is also a separate solution called IntegrationTests in its own folder, containing tests which start the services in separate processes and interact with them soley via REST. These tests verify that the different services are successfully coordinated via EventStore.
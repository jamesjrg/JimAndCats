#Jim and Cats

Nominally this repository is a bunch of web services providing a RESTful API for a fictional crowd funding website.

More practically this is a playground for trying out different approaches to problems, and for trying out various technologies and techniques to improve my knowledge.

##F# and functional programming style

1. The system is written entirely in F#, demonstrating that F# works well not only for complex logic and mathematical problems, for also for web endpoints, unit tests, build systems, database access, etc. I have endeavoured to avoid object-oriented programming altogether and do everything in a functional style, which has been greatly aided by the [Suave](www.suave.io) web server, which uses combinators rather controller classes. As part of this I wrote a substantial amount of the introductory documentation on the Suave website, though I'm still a long way from being as comfortable with F# as I am with C#.

#CQRS and event sourcing

These are terms which seem to mean very different things to different people. Some of the differences I have seen in implementations are:

a) Write models and inconsistency

-> One simple option is to simply assume that commands are always accepted without regard to business rules, and to deal with all states that break business rules after-the-fact, rather than trying to keep all the data consistent all of the time. For certain types of command this is perfectly reasonable, but to say that this has to be true for literally every single command may be problematic.

-> Regardless of whether or not all commands are considered valid, another way to simplify things is shard command-handling servers by aggregate or by tenant, and then ensuring writes for a particular entity are always handled by the same server (i.e. the single writer principle). This is perhaps too boring and untrendy, but I'm sure there are many cases where more complex designs are unnecessary. Eventually consistent read models may still be scaled horizontally to any degree.

-> The model that aligns with Domain Driven Design is to split up the domain into aggregate roots, each of which must always be consistent in itself, but to deal with inconsistencies involving multiple aggregates in an eventual manner, as above. If persisting an event would lead to an invalid aggregate root then the command does not take place. Care must be taken when deciding on aggregate root boundaries. If the aggregates are too small then there are very few rules that can be enforced, which is little different from the simpler option of just accepting all commands regardless. But if aggregates are too large then managing concurrency is much more difficult, because multiple users will often be editing the same aggregate, resulting in exceptions when a command handler attempts to persist the new events.

-> Regardless of whether there are business rules that allow a command to fail, and whether or not there are multiple writers, some sort of logic needs to be in place for a command on an entity that someone else has edited since the user started editing - for instance, a wiki page may not let you save your page edits until you refresh the page with someone else's recent edits. One solution is that the system may automatically try replaying the command after reloading the current state of the aggregate, or if there are no business rules then it may always append the event regardless of the id of the latest event in the stream, though such solutions may cause their own problems.

-> In addition to the business logic rules around aggregate state at at command time, and after-the-fact eventual consistency business logic run against the whole system, a system may also do some checking of business checking at command time via the eventually consistent read-model. For instance if a user is only allowed to do something if they have admin privileges it may be possible to check for those privileges against an eventually-consistent read model, allowing the occasional command to unexpectedly succeed or fail if a user has just had privileges added or revoked and the model of privileges has not yet been updated. Or perhaps the UI will disallow actions that look invalid according to the read model, even if this means there is a delay between the action becoming valid and the UI allowing the user to carry it out.

b) Read models

-> If aggregates are kept fairly small, and real-time queries only ever take place against a single aggregate at a time, then queries can be served by reconstituting aggregates directly from the event stream without need for a separate read model. Even if queries require access to multiple aggregates, the use of a FIFO cache may still allow queries to be served directly from event streams, rather than requiring the manual management of keeping a read model up to date. Things like search can be provided by pre-existing tools like Elasticsearch, and so even then an explicit separate read model is still unnecessary.

-> If real-time read model queries are complex and need access to data from numerous aggregates, but the total size of all the data required to serve read-model queries is fairly small - i.e. it fits in a few gigabytes - then it is perfectly plausible to keep the read model(s) entirely in RAM. This also makes for a pleasingly simple architecture, because queries are served using plain old objects, and there is no need to maintain a separate database to the main event stream. This does however require a well-thought-out approach for fail over for when servers restart and the read models need to be rebuilt.

-> One project I worked on wrote all events for a given aggregate type to the same stream and then had a listener than watch for events and use them to maintain a single SQL Server database of the current state of the world. To me this rather defeats the point of event-sourcing - if you ultimately serve all queries from a single RDBMS anyway then event sourcing is acting as nothing more than a persistent message bus and a write-ahead log for the database. It does make it easier to replay past data, and it does remove the need for a separate message queue when updating other services about changes, but to me it seems a very complex pattern to use soley for these reasons. IMHO SQL read models should only be used for certain specialist purposes such as searching for violations of business rules across multiple aggregate roots, or providing queryable reporting functionality, not as the main source of data for clients.

c) Communication between services

-> The combination of CQRS and event sourcing leads towards every component of a system maintaining its own local copy of state relevant to its actions. Whereas more traditional system designs tend to find out about the state in other systems via RPC or RESTful calls, CQRS+ES pushes designs towards all actions resulting in events, and then all systems interested in those events can subscribe to those events and build up their own model of state. In some ways this is helpful - it means services do not need to worry about how they are consumed as long as they post all relevant events, and it makes services more autonomous both in terms of resiliance to other services going down and in terms of not relying on shared service endpoint contracts (though the events themselves still need a contract).

But in some ways this can make seemingly simple changes much harder to carry out, because the logic to turn events into the current state of the world is no longer centralized, but replicated in multiple services.

Of course, the dichotomy is not in reality quite as extreme as this - read models with RESTful endpoints may still be shared between multiple services, and these read models may well be updated at the same time as the event model is changed.

-> Technologies like EventStore have the nice property that they act as both the canonical source of persisted events and as something that other services can subscribe to for updates, and therefore at least in principle they can replace complex architectures that need to keep SQL databases and message queues in sync with a single, simpler technology. But I think in reality most businesses will have demands that will require the use of messaging queues and relational databases anyway, so EventStore may end up adding even more complexity, rather than reducing it. 

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

* The service may be scaled horizontally - if multiple edits are made to the same user at the same time then the most recent change always wins. Both loading and saving of entities happens via solely via Event Store, though to optimistically prevent users being created with duplicate email addresses a database table lookup is also required.

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
#Jim and Cats

Nominally this repository is a bunch of web services providing a RESTful API for a fictional crowd funding website.

More practically this is a playground for trying out different approaches to problems, and for trying out various technologies and techniques to improve my knowledge.

In particular:

1. The system is written entirely in F#, demonstrating that F# works well not only for complex logic and mathematical problems, for also for web endpoints, unit tests, build systems, database access, etc.

2. The system experiments with event sourcing, a term which seems to mean different things to different people. Versions of "event sourcing" that I have been exposed to commerically are:

    a) The way usually recommended by well-known authors on the subject is to give each aggregate its own event stream, and then aggregates are reconsistuted directly from the events whenever a command is executed against them. The event stream on disk then functions as both the canonical source of truth, and also as a database for building aggregates. Where clients need to return information taken from multiple aggregates, a separate read model is created that gathers information from multiple streams to provide a cache of certain views of the data. Care must be taken when deciding on aggregate boundaries. If the aggregates are too small then all consistency violations must be dealt with after-the-case, which is much more work than simply making it impossible for data to become inconsistent. But if aggregates are too large then managing concurrency is much more difficult, as multiple users will often be editing the same aggregate.

	a) Also possible is to use an event stream per aggregate-type, and then to create both query models and command models in RAM. By allowing developers to write code against plain old in-memory objects this is in some ways actually simpler than a traditional SQL-based-approach, and debugging is very easy. Modern machines have so much RAM this is a plausible solution for many problems, and also allows for very high performance. But there are definite limits if there are a very large number of aggregate instances, and it needs a well-thought-out approach to failover for when servers go down. The fact it clearly isn't "web scale" also makes it very untrendy.

	c) One place I worked had event listeners watch event streams for each aggregate-type, which then updated tables of aggregates in SQL Server. Logic for both commands and queries was written in SQL, with the event stream used as service bus for sending events to other services. In my opinion isn't really proper event sourcing at all - it adds a lot of complexity without offering much benefit over a more standard approach of a SQL database + AMQP message bus.

	The services here started out as (b), then I stated borrowing some elements from (c) to try to make it a bit more enterprisey, and now I am entirely re-writing them as (a). As such the entire codebase is currently undergoing a major rewrite and doesn't even compile.

3. I have spent some time reading about and toying with different approaches to managing centralized authorization for a set of web services, again currently still a work in progress as I try out various options.

4. I have been trying out EventStore. My current workplace uses EventStore, but at the time I started this project it was basically just used as a service bus/persistent messaging mechanism, and no use was made of projections.

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
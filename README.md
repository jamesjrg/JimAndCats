#Jim and Cats

Nominally this repository is a bunch of web services providing a RESTful API for a fictional crowd funding website.

More practically this is a playground for trying out different approaches to problems, and for trying out various technologies and techniques to improve my knowledge.

In particular:

1. The system is written entirely in F#, demonstrating that F# works well not only for complex logic and mathematical problems, for also for web endpoints, unit tests, build systems, database access, etc.

2. The system experiments with event sourcing, a term which seems to mean different things to different people. Versions of "event sourcing" that I have been exposed to are:

a) giving each aggregate its own event stream, allowing aggregates to be reconsistuted when requested from that small subset of events relevant to the aggregate. Optionally some or all aggregates can be transiently cached in memory or in database in the query model.

b) restoring all aggregates from a persistent event stream whenever a new server is started up, and thereafter serving both commands and queries on all aggregates from state stored in RAM

c) having event listeners watch all event streams and update aggregates in SQL Server, and then writing all service logic directly against the SQL, using the event stream only for interacting with other services and maintaining a history log (which in my opinion isn't really event sourcing at all)

The services here started out as (b), then started borrowing some elements (c), and are now being entirely re-written as (a). This is such a major change that the entire codebase is currently undergoing a major rewrite and doesn't even compile.

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

####Jim.UserRepository

* Doc TODO

####EventStore.YetAnotherClient

* Yet another EventStore client, built on top of the official .NET TCP client.

####Suave.Extensions

Some handy utilities for making Suave web services.

####TestingHelpers

Some utility methods using the official Suave.Testing library for writing tests on web server endpoints, as well as some minimal BDD helper functions for testing domain logic.

###Other:
Most of the projects have an associated unit test project. These do not require access to a real EventStore instance, and run any required web server in-process.

There is also a separate solution called IntegrationTests in its own folder, containing tests which start the services in separate processes and interact with them soley via REST. These tests verify that the different services are successfully coordinated via EventStore.
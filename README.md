#Jim and Cats

Nominally this repository is a bunch of microservices providing a RESTful API for a fictional crowd funding website.

More practically this is a playground for trying out different approaches to problems, and for trying out various technologies and techniques to improve my knowledge.

In particular:

1. The system is written entirely in F#, demonstrating that F# works well not only for complex logic and mathematical problems, for also for web endpoints, unit tests, build systems, database access, etc.

2. The system experiments with event sourcing, a term which seems to mean different things to different people. Versions of "event sourcing" that I have been exposed to commercially are:

a) restoring all aggregates from a persistent event stream whenever a new server is started up, and thereafter serving both commands and queries on all aggregates from state stored in RAM

b) giving each aggregate its own event stream, allowing aggregates to be reconsistuted when requested from that small subset of events directly referring to the aggregate. Optionally some or all aggregates could be cached in memory or in database in the query model.

c) having event listeners watch all event streams and update aggregates in SQL Server, and then writing all service logic directly against the SQL, using the event stream only for sharing events with other services (which in my opinion isn't really event sourcing at all)

The services here are currently a broken mish-mash of the above ideas as I have experimented with the pros and cons of each.

3. I have spent some time reading about and toying with different approaches to managing centralized authorization for a set of microservices, again currently still a work in progress as I try out various options.

4. I have been trying out EventStore. My current workplace uses EventStore, but at the time I started this project it was basically just used as a service bus/persistent messaging mechanism, and no use was made of projections.

///

If this was a real live project with lots of developers each project would be in separate Visual Studio solutions, but as a personal experiment it's easier to just keep them all together. Below is a brief description of each service, though this document is not always up-to-date:

####Jim.CommandHandler: Just Identity Management Command Handler

* Manages commands relating to user authentication details and basic identity info (i.e. people's full name).

* Commands result in events being written to a private stream in an Event Store cluster. An Event Store projection takes the private identity events resulting from commands and maps them to new events on a public stream for use by other services (currently the Cats service). All data and events relating to password hashes are omitted.

* The service may be scaled horizontally - if multiple edits are made to the same user at the same time then the most recent change always wins. However, a store is maintained to check that users are not created with duplicate email addresses. This could feasibly be a database table that stores the email address set and nothing else, or just an in-memory hash set maintained in RAM on each server, but for it currently just re-uses the same user repository implementation as the read model service.

* Because different servers may create users at the same time, before the table for checking duplicates is updated, it may sometimes happen that two users are simultaneously created with the same email address. Adding logic to deal with this is still TODO.

####Jim.ReadModelUpdater: Just Identity Management Read Model Updater

* Listens to the private Jim event stream and uses it to update a SQL Server table for use by the Jim command query services. There can only be one running instance to avoid conflicting writes to the read model.

####Jim.QueryHandler: Just Identity Management Query Handler

* Verifies auth tokens for other microservice (though this is only half-way implemented at the moment)

* Also allows admins to query user details (currently all users are admins...).

* All queries are served via the SQL Server table maintained by the read model updater service. If the total number of users was never going to be especially large the read model should perhaps be stored entirely in-memory instead, with each instance building its cache directly from the Event Store. This could make the system easier to maintain and debug, though it might then require the management of Event Store snapshots and/or giving each aggregate its own event stream to make it easy to rebuild aggregates from scratch.

####Cats: Crowdfunding Ask Templates

* Manages a collection of projects asking for crowdfunding.

* Handles both queries and commands. It can trivially be horizontally scaled across multiple instances without worrying about eventual consistency because there is no command that will get the system into an illegal state, even when the aggregate checked by a command handler is out of date.

* Could easily be split into separate read and write services in the same way as the user service, but this hasn't been done yet

* As with the user query service, the query model could feasibly be stored entirely in memory, but currently it uses SQL Server.

####Cats.ReadModelUpdater:

* Listens to the private Cats event stream, the public identity event stream and the public pledges event stream. It uses them to update SQL Server tables for use by Cats services. There can only be one running instance to avoid conflicting writes to the read models.

####Pledges:

(TODO, just a stub project at the moment)

* Allows people to make pledges to cats. It can be horizontally scaled across multiple instances because there is no command that will get the system into an illegal state.

* Events are written to a private event stream, where an Event Store projection maps them to a public stream for consumption by other services (currently the Cats service).

* Handles only commands.

###There are also some shared libraries:

####GenericErrorHandling

* Generic types for representing successes and failures

####Jim.UserRepository

* Used by Jim.QueryHandler, Jim.CommandHandler and Jim.ReadModelUpdater. Given that the command handler never needs to write to the repository and also never needs to query anything other than email addresses from the repository (to check for duplicates), and that the query handler also never needs to write, it should probably be split into as many as three separate services, but as a shortcut they all share the same repository for the time being.

It contains both a SQL Server implementation and an in-memory implementation for use by tests.

####EventStore.YetAnotherClient

* Yet another EventStore client, built on top of the official .NET TCP client.

####Suave.Extensions

Some handy utilities for making Suave web services.

####TestingHelpers

Some utility methods using the official Suave.Testing library for writing tests on web server endpoints, as well as some minimal BDD helper functions for testing domain logic.

###Other:
Most of the projects have an associated unit test project. These do not require access to a real EventStore instance, and run any required web server in-process.

There is also a separate solution called IntegrationTests in its own folder, containing tests which start the services in separate processes and interact with them soley via REST. These tests verify that the different services are successfully coordinated via EventStore.
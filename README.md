#Jim and Cats

A bunch of F# microservices with a web API.

If this was a real thing with lots of developers they would be in separate Visual Studio solutions, but as a personal project it's easier to just keep them all together.

###The services are:

####Jim: Just Identity Management.

* Manages user authentication details and basic identity info (i.e. people's full name)

* Commands result in events being written to a private stream in an Event Store cluster. An EventStore projection takes the private identity events resulting from commands and maps them to new events on a public stream (omitting events ment)

* The command processor currently checks the legality of commands using and in-memory read model of all user aggregates. I may change this to use a SQL Server database at some point.

* Commands are intentionally synchronous (via an F# MailboxProcessor) to avoid the creation of conflicting users via concurrent events (e.g. two users with the same email address). Because of this the service is not currently horizontally scalable. Given that everything other than the creation of new users will continue to function even if the service goes down, this

* xxx public stream

####Cats: Crowdfunding Ask Templates.

Manages a collection of projects asking for crowdfunding.

####Cats.ReadModelUpdater:

####Pledges:

Allows people to make pledges to cats

###There are also some shared libraries:

####MicroCQRS.Common

Some generic code for making F# microservices backed by EventStore and fronted by a Suave web server

####Suave.Extensions

Some handy utilities for making Suave web services

####MicroCQRS.Common.Testing

Some utilities for testing Suave/EventStore microservices

###Other:
Almost all the projects have an associated unit test project. These do not require access to a real EventStore instance, and run any required web server in-process.

There is also a separate solution called IntegrationTests in its own folder, containing tests which start the services in separate processes and interact with them soley via REST. These tests verify that the different services are successfully coordinated via EventStore.
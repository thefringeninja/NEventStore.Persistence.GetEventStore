NEventStore.Persistence.GetEventStore
=====================================

GetEventStore persistence engine for NEventStore.

Before you build, please grab the [embedded-client](https://github.com/thefringeninja/EventStore/tree/embedded-client) branch and build it according to the build instructions. After that, run scripts\package-windows\merge-assemblies.ps1. Then copy bin\merged\EventStore.ClientAPI.* from EventStore over to lib\GetEventStore in this project. As soon as the nuget package is ready these instructions will go away.
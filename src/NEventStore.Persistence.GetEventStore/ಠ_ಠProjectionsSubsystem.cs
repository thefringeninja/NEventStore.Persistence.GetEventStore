﻿using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Common.Options;
using EventStore.Core;
using EventStore.Core.Bus;
using EventStore.Core.Messages;
using EventStore.Core.Services.AwakeReaderService;
using EventStore.Projections.Core;

namespace NEventStore.Persistence.GetEventStore
{
    internal sealed class ಠ_ಠProjectionsSubsystem : ISubsystem
    {
        public const int VERSION = 3;
        private readonly int _projectionWorkerThreadCount;
        private readonly ProjectionType _runProjections;
        private IDictionary<Guid, QueuedHandler> _coreQueues;

        private QueuedHandler _masterInputQueue;
        private InMemoryBus _masterMainBus;
        private InMemoryBus _masterOutputBus;
        private Dictionary<Guid, IPublisher> _queueMap;

        public ಠ_ಠProjectionsSubsystem(int projectionWorkerThreadCount, ProjectionType runProjections)
        {
            ForceProjectionAppDomainLoad();

            if (runProjections <= ProjectionType.System)
                _projectionWorkerThreadCount = 1;
            else
                _projectionWorkerThreadCount = projectionWorkerThreadCount;
            _runProjections = runProjections;
        }

        public IPublisher MainQueue
        {
            get { return _masterInputQueue; }
        }

        public void Register(StandardComponents standardComponents)
        {
            _masterMainBus = new InMemoryBus("manager input bus");
            _masterInputQueue = new QueuedHandler(_masterMainBus, "Projections Master");
            _masterOutputBus = new InMemoryBus("ProjectionManagerAndCoreCoordinatorOutput");

            var projectionsStandardComponents = new ProjectionsStandardComponents(
                _projectionWorkerThreadCount,
                _runProjections,
                _masterOutputBus,
                _masterInputQueue,
                _masterMainBus);

            CreateAwakerService(standardComponents);
            _coreQueues = ProjectionCoreWorkersNode.CreateCoreWorkers(standardComponents, projectionsStandardComponents);
            _queueMap = _coreQueues.ToDictionary(v => v.Key, v => (IPublisher) v.Value);

            ProjectionManagerNode.CreateManagerService(standardComponents, projectionsStandardComponents, _queueMap);
        }


        public void Start()
        {
            if (_masterInputQueue != null)
                _masterInputQueue.Start();
            foreach (var queue in _coreQueues)
                queue.Value.Start();
        }

        public void Stop()
        {
            if (_masterInputQueue != null)
                _masterInputQueue.Stop();
            foreach (var queue in _coreQueues)
                queue.Value.Stop();
        }

        /// <summary>
        ///     Really lame way to ensure projection messages get into the MessageHierarchy/>
        /// </summary>
        private static void ForceProjectionAppDomainLoad()
        {
            typeof (ProjectionsSubsystem).ToString();
        }

        private static void CreateAwakerService(StandardComponents standardComponents)
        {
            var awakeReaderService = new AwakeService();
            standardComponents.MainBus.Subscribe<StorageMessage.EventCommitted>(awakeReaderService);
            standardComponents.MainBus.Subscribe<StorageMessage.TfEofAtNonCommitRecord>(awakeReaderService);
            standardComponents.MainBus.Subscribe<AwakeServiceMessage.SubscribeAwake>(awakeReaderService);
            standardComponents.MainBus.Subscribe<AwakeServiceMessage.UnsubscribeAwake>(awakeReaderService);
        }
    }
}
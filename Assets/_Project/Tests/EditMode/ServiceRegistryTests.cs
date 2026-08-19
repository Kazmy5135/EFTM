using System;
using System.Collections.Generic;
using EFTM.Core;
using NUnit.Framework;

namespace EFTM.Tests.EditMode
{
    public sealed class ServiceRegistryTests
    {
        [Test]
        public void InitializeAndShutdown_UseRegistrationOrderAndReverseOrder()
        {
            var events = new List<string>();
            var registry = new ServiceRegistry();

            registry.Register(new FirstService(events));
            registry.Register(new SecondService(events));

            registry.InitializeAll();
            registry.ShutdownAll();

            CollectionAssert.AreEqual(
                new[] { "first:init", "second:init", "second:shutdown", "first:shutdown" },
                events);
        }

        [Test]
        public void Register_DuplicateServiceType_Throws()
        {
            var registry = new ServiceRegistry();
            registry.Register(new FirstService(new List<string>()));

            Assert.Throws<InvalidOperationException>(
                () => registry.Register(new FirstService(new List<string>())));
        }

        [Test]
        public void InitializeAll_WhenAServiceFails_RollsBackInitializedServices()
        {
            var events = new List<string>();
            var registry = new ServiceRegistry();

            registry.Register(new FirstService(events));
            registry.Register(new FailingService(events));

            Assert.Throws<InvalidOperationException>(() => registry.InitializeAll());
            CollectionAssert.AreEqual(
                new[] { "first:init", "failing:init", "first:shutdown" },
                events);
            Assert.That(registry.IsInitialized, Is.False);
        }

        private sealed class FirstService : IGameService
        {
            private readonly IList<string> events;

            public FirstService(IList<string> events)
            {
                this.events = events;
            }

            public void Initialize() => events.Add("first:init");

            public void Shutdown() => events.Add("first:shutdown");
        }

        private sealed class SecondService : IGameService
        {
            private readonly IList<string> events;

            public SecondService(IList<string> events)
            {
                this.events = events;
            }

            public void Initialize() => events.Add("second:init");

            public void Shutdown() => events.Add("second:shutdown");
        }

        private sealed class FailingService : IGameService
        {
            private readonly IList<string> events;

            public FailingService(IList<string> events)
            {
                this.events = events;
            }

            public void Initialize()
            {
                events.Add("failing:init");
                throw new InvalidOperationException("Expected test failure.");
            }

            public void Shutdown() => events.Add("failing:shutdown");
        }
    }
}

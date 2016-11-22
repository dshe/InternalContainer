﻿using System;
using StandardContainer.Tests.Tests.Other;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.TypeFactory
{
    public class TypeFactoryGetInstanceTest : TestBase
    {
        public TypeFactoryGetInstanceTest(ITestOutputHelper output) : base(output) {}

        public class SomeClass {}

        [Fact]
        public void T00_not_registered()
        {
            var container = new Container(log: Write);
            Assert.Throws<TypeAccessException>(() => container.RegisterTransient<Func<SomeClass>>()).Output(Write);
        }

        [Fact]
        public void T01_factory_from_transient()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<SomeClass>();

            var factory = container.Resolve<Func<SomeClass>>();
            Assert.IsType(typeof(SomeClass), factory());
            Assert.NotEqual(factory(), factory());
        }

        [Fact]
        public void T02_factory_from_factory()
        {
            var container = new Container(log: Write);
            container.RegisterFactory(() => new SomeClass());
            var factory = container.Resolve<Func<SomeClass>>();
            Assert.NotEqual(factory(), factory());
        }

        [Fact]
        public void T03_factory_from_singleton()
        {
            var container = new Container(log:Write);
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<Func<SomeClass>>()).Output(Write);
        }

        [Fact]
        public void T04_factory_from_instance()
        {
            var container = new Container(log: Write);
            container.RegisterInstance(new SomeClass());
            Assert.Throws<TypeAccessException>(() => container.Resolve<Func<SomeClass>>()).Output(Write);
        }

        [Fact]
        public void T05_auto_transient()
        {
            var container = new Container(DefaultLifestyle.Transient, log: Write);
            container.Resolve<Func<SomeClass>>();
        }

        [Fact]
        public void T06_auto_singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton, log:Write);
            container.Resolve<Func<SomeClass>>();
            container.Log();
        }

        [Fact]
        public void T07_auto_singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: Write);
            container.Resolve<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<Func<SomeClass>>()).Output(Write);
            container.Log();
        }

    }
}

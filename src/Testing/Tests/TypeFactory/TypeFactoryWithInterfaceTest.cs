﻿using System;
using StandardContainer;
using Testing.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Tests.TypeFactory
{
    public class TypeFactoryWithInterfaceTest : TestBase
    {
        public TypeFactoryWithInterfaceTest(ITestOutputHelper output) : base(output) {}

        public interface IFoo { }
        public interface IBar { }

        public class Foo : IFoo { }
        public class Bar : IBar
        {
            public Bar(Func<IFoo> factory) { }
        }

        [Fact]
        public void T01_transient_factory()
        {
            var container = new Container();
            container.RegisterTransient<IFoo, Foo>();
            var factory = container.Resolve<Func<IFoo>>();
            Assert.IsType<Foo>(factory());
            Assert.NotEqual(factory(), factory());
        }

        [Fact]
        public void T02_singleton_factory()
        {
            var container = new Container();
            container.RegisterTransient<IFoo>();
            container.Resolve<Func<IFoo>>();
        }

        [Fact]
        public void T03_auto_singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton);
            container.Resolve<Func<IFoo>>();
        }

        [Fact]
        public void T04_auto_singleton_injection()
        {
            var container = new Container(DefaultLifestyle.Singleton);
            container.Resolve<IBar>();
        }

    }
}

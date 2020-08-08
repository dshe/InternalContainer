﻿using System;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;

namespace MinimalContainer.Tests.Lifestyle
{
    public class TransientTest : BaseUnitTest
    {
        public interface IFoo { }
        public class Foo : IFoo { }

        public TransientTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void T00_Not_Registered()
        {
            var container = new Container(log: Log);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IFoo>()).WriteMessageTo(Log);
        }

        [Fact]
        public void T01_Already_Registered()
        {
            var container = new Container(log: Log);
            container.RegisterTransient<Foo>();
            Assert.Throws<TypeAccessException>(() => container.RegisterTransient<Foo>()).WriteMessageTo(Log);
        }

        [Fact]
        public void T02_Concrete()
        {
            var container = new Container(log: Log);
            container.RegisterTransient<Foo>();
            var instance1 = container.Resolve<Foo>();
            var instance2 = container.Resolve<Foo>();
            Assert.NotEqual(instance1, instance2);
        }

        [Fact]
        public void T03_Interface()
        {
            var container = new Container(log: Log);
            container.RegisterTransient<IFoo>();
            Assert.Throws<TypeAccessException>(() => container.RegisterTransient<IFoo>()).WriteMessageTo(Log);
            var instance3 = container.Resolve<IFoo>();
            var instance4 = container.Resolve<IFoo>();
            Assert.NotEqual(instance3, instance4);
            Assert.Throws<TypeAccessException>(() => container.Resolve<Foo>()).WriteMessageTo(Log);
        }

        [Fact]
        public void T04_Concrete_Interface()
        {
            var container = new Container(log: Log);
            container.RegisterTransient<IFoo>();
            container.RegisterTransient<Foo>();
            var instance5 = container.Resolve<Foo>();
            var instance6 = container.Resolve<IFoo>();
            Assert.NotEqual(instance6, instance5);
        }

    }
}

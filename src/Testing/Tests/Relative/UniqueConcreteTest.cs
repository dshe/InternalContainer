﻿using System;
using StandardContainer;
using Testing.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Tests.Relative
{
    public class UniqueConcreteTest : TestBase
    {
        public UniqueConcreteTest(ITestOutputHelper output) : base(output) {}

        public interface IMarker1 {}
        public interface IMarker2 {}

        public class ClassA : IMarker1, IMarker2 {}
        public class ClassB : IMarker1, IMarker2 {}
        public class ClassC : IMarker1, IMarker2 {}

        [Fact]
        public void T01_Duplicate_Registration()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);

            container.RegisterSingleton<ClassA>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ClassA>()).WriteMessageTo(Write);

            container.RegisterSingleton<IMarker1, ClassB>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<IMarker1, ClassB>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T02_Registration_Duplicate_Marker()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IMarker1>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T03_Registration_Concrete_Multiple()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<IMarker1, ClassA>();
            container.Resolve<IMarker1>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassA>()).WriteMessageTo(Write);
        }

    }

}
﻿using System;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests.Relative
{
    public interface IMarker1 {}
    public interface IMarker2 {}

    public class ClassA1 : IMarker1, IMarker2 {}
    public class ClassA2 : IMarker1, IMarker2 { }
    public class ClassA3 : IMarker1, IMarker2 { }

    public class UniqueConcreteTest
    {
        private readonly Container container;

        public UniqueConcreteTest(ITestOutputHelper output)
        {
            container = new Container(Lifestyle.Singleton, log: output.WriteLine);
        }

        [Fact]
        public void Test_DuplicateRegistration()
        {
            container.RegisterSingleton<ClassA1>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ClassA1>());

            container.RegisterSingleton<IMarker1, ClassA2>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<IMarker1, ClassA2>());
        }
        [Fact]
        public void Test_RegistrationConcrete()
        {
            container.RegisterSingleton<ClassA1>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<IMarker1, ClassA1>());

            container.RegisterSingleton<IMarker1, ClassA2>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ClassA2>());
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<IMarker1, ClassA3>());
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<IMarker2, ClassA2>());
        }

        [Fact]
        public void Test_RegistrationConcreteMultiple()
        {
            container.RegisterSingleton<IMarker1,ClassA1>();
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassA1>());
            container.GetInstance<IMarker1>();
        }

    }

}

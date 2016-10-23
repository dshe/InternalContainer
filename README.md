
## StandardContainer&nbsp;&nbsp; [![release](https://img.shields.io/github/release/dshe/StandardContainer.svg)](https://github.com/dshe/StandardContainer/releases) [![status](https://ci.appveyor.com/api/projects/status/uuft89jhlm0xw22q/branch/master?svg=true)](https://ci.appveyor.com/project/dshe/standardcontainer/branch/master) [![License](https://img.shields.io/badge/license-Apache%202.0-7755BB.svg)](https://opensource.org/licenses/Apache-2.0)

***A simple and portable IoC (Inversion of Control) container.***
- one C# 6.0 source file with no dependencies
- supports .NET Platform Standard 1.0
- supports automatic and/or explicit type registration
- supports public and internal constructor injection
- supports injection of instances and factories
- supports transient and singleton lifestyles
- detects captive and recursive dependencies
- fluent interface
- fast

#### example
```csharp
public interface IFoo {}
public class Foo : IFoo {}

public void Main()
{
    var container = new Container();
    container.RegisterSingleton<IFoo, Foo>();
    IFoo instance = container.GetInstance<IFoo>();
...
```
#### registration
```csharp
container.RegisterSingleton<Foo>();
container.RegisterSingleton<IFoo>();
container.RegisterSingleton<IFoo, Foo>();

container.RegisterTransient<Foo>();
container.RegisterTransient<IFoo>();
container.RegisterTransient<IFoo, Foo>();

container.RegisterInstance(foo);
container.RegisterInstance<IFoo>(foo);

container.RegisterFactory(() => new Foo());
container.RegisterFactory<IFoo>(() => new Foo());
```
#### resolution
```csharp
IFoo instance = container.GetInstance<IFoo>();
Func<IFoo> factory = container.GetInstance<Func<IFoo>>();
```
#### constructors
The container can create instances of types using public and internal constructors. In case a type has more than one constructor, indicate the constructor to be used with the 'ContainerConstructor' attribute. Otherwise, the constructor with the smallest number of arguments is selected.
```csharp
public class Foo
{
    public Foo() {}

    [ContainerConstructor]    
    public Foo(IFoo2 foo2) {}
}
```
#### enumerables
```csharp
public class IFoo {}
public class Foo1 : IFoo {}
public class Foo2 : IFoo {}

var container = new Container();
container.RegisterSingleton<Foo1>();
container.RegisterSingleton<Foo2>();

IList<IFoo> list = container.GetInstance<IList<Ifoo>>();
```
A list of instances of registered types which are assignable to `IFoo` is returned.
#### fluency
```csharp
var root = new Container(Lifestyle.Transient)
    .RegisterSingleton<T1>()
    .RegisterInstance(new T2())
    .RegisterFactory(() => new T3())
    .GetInstance<TRoot>();
```
#### automatic registration
```csharp
public class Foo {}

var container = new Container(Lifestyle.Singleton, assemblies:someAssembly);

Foo instance = container.GetInstance<Foo>();
```
To enable automatic registration, set the default lifestyle to singleton or transient when constructing the container. Note that the container will always register the dependencies of singleton instances as singletons. If automatic type resolution requires scanning assemblies other than the assembly where the container is created, include references to those assemblies in the container's constructor.

#### example
```csharp
public interface IClassB {}
public class ClassB : IClassB {}

public class ClassA
{
    public ClassA(IClassB b) {}
}

public class Root
{
    public Root(ClassA a) {}
    public void StartApplication() {}
}

new Container(Lifestyle.Transient).GetInstance<TRoot>().StartApplication();
```
The complete object graph is created by simply resolving the compositional root. 

#### resolution strategy
The following graphic illustrates the automatic type resolution strategy:

![Image of Resolution Strategy](https://github.com/dshe/InternalContainer/blob/master/TypeResolutionFlowChart.png)


#### disposal
```csharp
container.Dispose();
```
Disposing the container disposes any registered disposable singletons.
#### logging
```csharp
var container = new Container(log:Console.WriteLine);
```
#### diagnostic
```csharp
Console.WriteLine(container.ToString());
```

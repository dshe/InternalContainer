/*
StandardContainer.cs 0.1.*
Copyright 2016 dshe
Licensed under the Apache License 2.0: http://www.apache.org/licenses/LICENSE-2.0
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace StandardContainer
{
    public enum Lifestyle { Transient, Singleton, Instance, Factory };
    public enum DefaultLifestyle { Transient, Singleton, None };

    public sealed class Registration
    {
        public TypeInfo Type;
        public Lifestyle Lifestyle;
        public readonly List<Concrete> Concretes = new List<Concrete>();
        //public Concrete Concrete = new Concrete();
        //public override string ToString() =>
        //    $"{(Concrete.Type == null || Equals(Concrete.Type, Type) ? "" : Concrete.Type.AsString() + "->")}{Type.AsString()}, {Lifestyle}({Concrete.Count})";
    }
    public sealed class Concrete
    {
        public Lifestyle Lifestyle;
        public TypeInfo Type;
        public Func<object> Factory;
        internal Expression Expression;
        public int Count;
    }


    public sealed class Container : IDisposable
    {
        private readonly DefaultLifestyle defaultLifestyle;
        private readonly List<TypeInfo> allTypesConcrete;
        private readonly Dictionary<Type, Registration> registrations = new Dictionary<Type, Registration>();
        private readonly Dictionary<Type, Concrete> concretes = new Dictionary<Type, Concrete>();
        public IEnumerable<Registration> Registrations => registrations.Values.OrderBy(r => r.Type.Name);
        private readonly Stack<TypeInfo> typeStack = new Stack<TypeInfo>();
        private readonly Action<string> log;

        public Container(DefaultLifestyle defaultLifestyle = DefaultLifestyle.None, Action<string> log = null, params Assembly[] assemblies)
        {
            this.defaultLifestyle = defaultLifestyle;
            this.log = log;
            Log("Creating Container.");
            var assemblyList = assemblies.ToList();
            if (!assemblyList.Any())
            {
                var method = typeof(Assembly).GetTypeInfo().GetDeclaredMethod("GetCallingAssembly");
                if (method == null)
                    throw new ArgumentException("Since calling assembly cannot be determined, one or more assemblies must be indicated when constructing the container.");
                assemblyList.Add((Assembly)method.Invoke(null, new object[0]));
            }
            allTypesConcrete = assemblyList
                .Select(a => a.DefinedTypes.Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface).ToList())
                .SelectMany(x => x)
                .ToList();
            RegisterInstance(this); // container self-registration
        }

        public Container RegisterTransient<T>() => RegisterTransient(typeof(T));
        public Container RegisterTransient<T, TConcrete>() where TConcrete : T => RegisterTransient(typeof(T), typeof(TConcrete));
        public Container RegisterTransient(Type type, Type typeConcrete = null) => Register(Lifestyle.Transient, type, typeConcrete);

        public Container RegisterSingleton<T>() => RegisterSingleton(typeof(T));
        public Container RegisterSingleton<T, TConcrete>() where TConcrete : T => RegisterSingleton(typeof(T), typeof(TConcrete));
        public Container RegisterSingleton(Type type, Type typeConcrete = null) => Register(Lifestyle.Singleton, type, typeConcrete);

        public Container RegisterInstance<T>(T instance) => RegisterInstance(typeof(T), instance);
        public Container RegisterInstance(Type type, object instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            return Register(Lifestyle.Instance, type, instance.GetType(), () => instance);
        }

        public Container RegisterFactory<T>(Func<T> factory) where T : class => RegisterFactory(typeof(T), factory);
        public Container RegisterFactory(Type type, Func<object> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            return Register(Lifestyle.Factory, type, null, factory);
        }

        private Container Register(Lifestyle lifestyle, Type type, Type typeConcrete,
            Func<object> factory = null, [CallerMemberName] string caller = null)
        {
            AddRegistration(lifestyle, type?.GetTypeInfo(), typeConcrete?.GetTypeInfo(), factory, caller);
            return this;
        }

        //////////////////////////////////////////////////////////////////////////////

        private Registration AddRegistration(Lifestyle lifestyle, TypeInfo type, TypeInfo typeConcrete, Func<object> factory, string caller)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (typeConcrete == null && lifestyle != Lifestyle.Instance && lifestyle != Lifestyle.Factory)
                typeConcrete = allTypesConcrete.FindTypeConcrete(type);
            if (typeConcrete != null)
            {
                if (!type.IsAssignableFrom(typeConcrete))
                    throw new TypeAccessException($"Type {typeConcrete.AsString()} is not assignable to type {type.AsString()}.");
                if (typeConcrete.IsValueType)
                    throw new TypeAccessException("Cannot register value type.");
                if (typeof(string).GetTypeInfo().IsAssignableFrom(typeConcrete))
                    throw new TypeAccessException("Cannot register type string.");
            }
            lock (registrations)
                return AddRegistrationCore(lifestyle, type, typeConcrete, factory, caller);
        }

        private Registration AddRegistrationCore(Lifestyle lifestyle, TypeInfo type, TypeInfo typeConcrete,
            Func<object> factory, string caller)
        {
            Registration reg;
            if (!registrations.TryGetValue(type.AsType(), out reg))
            {
                reg = new Registration
                {
                    Lifestyle = lifestyle,
                    Type = type
                };
                registrations.Add(type.AsType(), reg);
            }
            else if (reg.Lifestyle != lifestyle)
                throw new TypeAccessException("bad lifestyle");

            var concreteNew = new Concrete
            {
                Lifestyle = lifestyle,
                Type = typeConcrete,
                Factory = factory
            };
            Log(() => $"{caller}: {reg}");

            if (lifestyle == Lifestyle.Instance || lifestyle == Lifestyle.Factory)
            {
                reg.Concretes.Add(concreteNew);
                return reg;
            }

            Concrete concreteFound;
            if (!concretes.TryGetValue(typeConcrete.AsType(), out concreteFound))
            {
                reg.Concretes.Add(concreteNew);
                concretes.Add(typeConcrete.AsType(), concreteNew);
            }
            else
            {
                if (reg.Lifestyle != concreteFound.Lifestyle)
                    throw new TypeAccessException($"Bad olifestyle for type {type.Name} is already registered.");
                if (reg.Concretes.Any(c => Equals(c.Type, typeConcrete)))
                    throw new TypeAccessException($"Concrete type {type.Name} is already registered.");
                reg.Concretes.Add(concreteFound);
            }
            return reg;
        }

        //////////////////////////////////////////////////////////////////////////////

        public T GetInstance<T>() => (T)GetInstance(typeof(T));
        public object GetInstance(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            lock (registrations)
            {
                try
                {
                    if (!typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                        return GetRegistration(type, null).Concretes.Single().Factory();

                    var genericType = type.GenericTypeArguments.Single();
                    var expressions = GetRegistration(genericType, null).Concretes.Select(x => x.Expression).ToList();
                    if (!expressions.Any())
                        throw new TypeAccessException($"No types found assignable to generic type {genericType.AsString()}.");
                    Log(() => $"Creating list of {expressions.Count} types assignable to {genericType.AsString()}.");
                    var xxx = Expression.NewArrayInit(genericType, expressions);
                    var factory = Expression.Lambda<Func<object>>(xxx).Compile();
                    return factory();
                }
                catch (TypeAccessException ex)
                {
                    if (!typeStack.Any())
                        throw new TypeAccessException($"Could not get instance of type {type}. {ex.Message}", ex);
                    var typePath = typeStack.Select(t => t.AsString()).JoinStrings("->");
                    throw new TypeAccessException($"Could not get instance of type {typePath}. {ex.Message}", ex);
                }
            }
        }

        private IEnumerable<T> ToListOfType<T>(T data, IEnumerable<object> listIn) where T: class
        {
            //object o;
            return listIn.Select(x => x as T);
            

            var list = new List<T>();
            foreach (var x in listIn)
                list.Add((T)x);
            return list;
        }

        private Registration GetRegistration(Type type, Concrete dependent)
        {
            Registration reg;
            if (!registrations.TryGetValue(type, out reg))
            {
                if (defaultLifestyle == DefaultLifestyle.None)
                    throw new TypeAccessException($"Cannot resolve unregistered type {type.AsString()}.");
                var style = (dependent?.Lifestyle == Lifestyle.Singleton || dependent?.Lifestyle == Lifestyle.Instance || defaultLifestyle == DefaultLifestyle.Singleton)
                    ? Lifestyle.Singleton : Lifestyle.Transient;
                reg = AddRegistration(style, type.GetTypeInfo(), null, null, "Auto-registration");
            }

            foreach (var c in reg.Concretes.Where(c => c.Expression == null))
                Initialize(c, dependent);

            //reg.Concrete.Count = reg.Lifestyle == Lifestyle.Transient || reg.Lifestyle == Lifestyle.Factory ?  reg.Concrete.Count + 1 : 1;
            return reg;
        }

        private void Initialize(Concrete reg, Concrete dependent)
        {
            if (reg.Lifestyle == Lifestyle.Instance)
            {
                reg.Expression = Expression.Constant(reg.Factory());
                return;
            }
            if (reg.Lifestyle == Lifestyle.Factory)
            {
                Expression<Func<object>> expression = () => reg.Factory();
                reg.Expression = expression;
                return;
            }
            if (dependent == null)
            {
                typeStack.Clear();
                Log(() => $"Getting instance of type: {reg.Type.AsString()}.");
            }
            typeStack.Push(reg.Type);
            if (typeStack.Count(t => t.Equals(reg.Type)) > 1)
                throw new TypeAccessException("Recursive dependency.");
            if (dependent?.Lifestyle == Lifestyle.Singleton && reg.Lifestyle == Lifestyle.Transient)
                throw new TypeAccessException($"Captive dependency: the singleton {dependent.Type.AsString()} depends on transient {reg.Type.AsString()}.");
            reg.Expression = GetExpression(reg);
            reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();
            if (reg.Lifestyle == Lifestyle.Singleton)
            {
                var instance = reg.Factory();
                reg.Expression = Expression.Constant(instance);
                reg.Factory = () => instance;
            }
            typeStack.Pop();
        }

        private Expression GetExpression(Concrete reg)
        {
            // For singleton registrations, use a previously registered singleton instance, if any.
            /*
            if (reg.Lifestyle == Lifestyle.Singleton)
            {
                var expression = registrations.Values.Where(r =>
                        Equals(r.Concrete.Type, reg.Concrete.Type) &&
                        r.Lifestyle == Lifestyle.Singleton &&
                        r.Concrete.Expression != null)
                    .Select(r => r.Concrete.Expression)
                    .SingleOrDefault();
                if (expression != null)
                    return expression;
            }
            */
           // if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(reg.Type))
               //return GetExpressionArray(reg);
            return GetExpressionNew(reg);
        }

        private Expression GetExpressionNew(Concrete reg)
        {
            var type = reg.Type;
            var ctor = type.GetConstructor();
            var parameters = ctor.GetParameters()
                .Select(p => p.HasDefaultValue ? Expression.Constant(p.DefaultValue, p.ParameterType) : GetExpression(p.ParameterType, reg))
                .ToList();
            Log(() => $"Constructing {reg.Lifestyle} instance: {type.AsString()}({parameters.Select(p => p?.Type.AsString()).JoinStrings(", ")}).");
            return Expression.New(ctor, parameters);
        }

        /*
        private Expression GetExpressionArray(Concrete reg)
        {
            //var genericType = reg.TypeConcrete.GenericTypeArguments.Single().GetTypeInfo();
            var genericType = reg.Type;
            var expressions = allTypesConcrete
                .Where(t => genericType.IsAssignableFrom(t))
                .Select(x => GetRegistration(x.AsType(), reg).Expression)
                .ToList();
            if (!expressions.Any())
                throw new TypeAccessException($"No types found assignable to generic type {genericType.AsString()}.");
            Log(() => $"Creating list of {expressions.Count} types assignable to {genericType.AsString()}.");
            return Expression.NewArrayInit(genericType.AsType(), expressions);
        }
        */


        private Expression GetExpression(Type type, Concrete dependent)
        {
            if (!typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                return GetRegistration(type, null).Concretes.Single().Expression;
            var genericType = type.GenericTypeArguments.Single().GetTypeInfo();
            var regs = GetRegistration(genericType.AsType(), null).Concretes.Select(x => x.Expression).ToList();
            return Expression.NewArrayInit(genericType.AsType(), regs);
        }


        //////////////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            var reg = Registrations.ToList();
            return new StringBuilder()
                .AppendLine($"Container: {defaultLifestyle}, {reg.Count} registered types:")
                .AppendLine(reg.Select(x => x.ToString()).JoinStrings(Environment.NewLine))
                .ToString();
        }

        public void Log() => Log(ToString());
        private void Log(string message) => Log(() => message);
        private void Log(Func<string> message)
        {
            if (log == null)
                return;
            var msg = message?.Invoke();
            if (!string.IsNullOrEmpty(msg))
                log(msg);
        }

        /// <summary>
        /// Disposing the container disposes any registered disposable instances.
        /// </summary>
        public void Dispose()
        {
            lock (registrations)
            {
                foreach (var instance in Registrations
                    .ToList()
                    .Where(r => r.Lifestyle == Lifestyle.Singleton || r.Lifestyle == Lifestyle.Instance)
                    .SelectMany(r => r.Concretes)
                    .Select(c => c.Factory())
                    .Where(i => i != null && i != this)
                    .OfType<IDisposable>())
                {
                    Log($"Disposing type {instance.GetType().AsString()}.");
                    instance.Dispose();
                }
                registrations.Clear();
                concretes.Clear();
            }
            Log("Container disposed.");
        }
    }

    /// <summary>
    /// The container can create instances of types using public and internal constructors. 
    /// In case a type has more than one constructor, indicate the constructor to be used with the ContainerConstructor attribute.
    /// Otherwise, the constructor with the smallest number of arguments is selected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class ContainerConstructorAttribute : Attribute { }

    internal static class StandardContainerEx
    {
        /// When a non-concrete type is indicated (register or get instance), the concrete type is determined automatically.
        /// In this case, the non-concrete type must be assignable to exactly one concrete type.
        internal static TypeInfo FindTypeConcrete(this List<TypeInfo> allTypesConcrete, TypeInfo type)
        {
            if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type) || (!type.IsAbstract && !type.IsInterface))
                return type;
            var assignableTypes = allTypesConcrete.Where(type.IsAssignableFrom).ToList(); // slow
            if (assignableTypes.Count != 1)
                throw new TypeAccessException($"{assignableTypes.Count} types found assignable to {type.AsString()}.");
            return assignableTypes.Single();
        }

        internal static ConstructorInfo GetConstructor(this TypeInfo type)
        {
            var ctors = type.DeclaredConstructors.Where(c => !c.IsPrivate).ToList();
            if (ctors.Count == 1)
                return ctors.Single();
            if (!ctors.Any())
                throw new TypeAccessException($"Type {type.AsString()} has no public or internal constructor.");
            var ctorsWithAttribute = ctors.Where(c => c.GetCustomAttribute<ContainerConstructorAttribute>() != null).ToList();
            if (ctorsWithAttribute.Count == 1)
                return ctorsWithAttribute.Single();
            if (ctorsWithAttribute.Count > 1)
                throw new TypeAccessException($"Type {type.AsString()} has more than one constructor decorated with {nameof(ContainerConstructorAttribute)}.");
            return ctors.OrderBy(c => c.GetParameters().Length).First();
        }

        internal static string JoinStrings(this IEnumerable<string> strings, string separator) => string.Join(separator, strings);
        internal static string AsString(this Type type) => type.GetTypeInfo().AsString();
        internal static string AsString(this TypeInfo type)
        {
            var name = type.Name;
            if (type.IsGenericParameter || !type.IsGenericType)
                return name;
            var index = name.IndexOf("`", StringComparison.Ordinal);
            if (index >= 0)
                name = name.Substring(0, index);
            var args = type.GenericTypeArguments
                .Select(a => a.GetTypeInfo().AsString())
                .JoinStrings(",");
            return $"{name}<{args}>";
        }
    }
}
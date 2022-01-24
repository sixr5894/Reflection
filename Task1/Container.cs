using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Task1.DoNotChange;

namespace Task1
{
    public class Container
    {

        List<Type> _attributes = new List<Type>() { typeof(ExportAttribute), typeof(ImportAttribute), typeof(ImportConstructorAttribute) };
        private Assembly _asm;
        private List<Type> _types = new List<Type>();
        private void SetDefVals()
        {
            _asm = null;
            _types = new List<Type>();
        }
        public void AddAssembly(Assembly assembly)
        {
            if (this._asm != null)
                throw new Exception();
            this._asm = assembly;
            this._types = _asm.GetTypes().ToList();
        }

        public void AddType(Type type)
        {
            if (this._types.Contains(type))
                throw new Exception();
            this._types.Add(type);
        }

        public void AddType(Type type, Type baseType)
        {
            if (this._types.Any(c => c == type || c == baseType))
                throw new Exception();
            this._types.Add(type);
            this._types.Add(baseType);
        }

        public T Get<T>()
        {
            if (RequireConstructorDI<T>())
                return DIToConstructor<T>();
            if (RequirePropertyDI<T>())
                return DIToProperty<T>();
            if (typeof(T).IsInterface)
                return DirectDI<T>();
            return Build<T>();
        }
        private T Build<T>()
        {
            T result = (T)Activator.CreateInstance(typeof(T));
            SetDefVals();
            return result;
        }
        private T DirectDI<T>()
        {
            T result = (T)Activator.CreateInstance(this._types.FirstOrDefault(x => x.GetInterfaces().FirstOrDefault(c => c == typeof(T)) != null));
            SetDefVals();
            return result;
        }
        private T DIToProperty<T>()
        {
            T result = (T)Activator.CreateInstance(typeof(T));
            foreach (var temp in typeof(T).GetProperties())
            {
                Type t = temp.PropertyType;
                if (t.IsInterface)
                    t = RemoveInjection(t);
                temp.SetValue(result, Activator.CreateInstance(t));
            }
            SetDefVals();
            return result;
        }
        private T DIToConstructor<T>()
        {
            List<Type> tempArgs = new List<Type>();
            foreach (ParameterInfo temp in typeof(T).GetConstructors()[0].GetParameters())
            {
                Type t = temp.ParameterType;
                if (temp.ParameterType.IsInterface)
                    t = RemoveInjection(t);
                tempArgs.Add(t);
            }
            object[] args = new object[tempArgs.Count];
            for (int i = 0; i < tempArgs.Count; i++)
            {
                args[i] = Activator.CreateInstance(tempArgs[i]);
            }
            T result = (T)Activator.CreateInstance(typeof(T), args);
            SetDefVals();
            return result;
        }
        private Type RemoveInjection(Type t) => this._types.FirstOrDefault(x => x.GetInterfaces().FirstOrDefault(v => v == t) != null);
        private bool RequirePropertyDI<T>() => typeof(T).GetProperties().FirstOrDefault(x => x.GetCustomAttribute(typeof(ImportAttribute)) != null) != null;
        private bool RequireConstructorDI<T>() => typeof(T).GetCustomAttribute(typeof(ImportConstructorAttribute)) != null;

    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace GeoDecisions.Esb.Common.Utility.Plugins
{
    internal class PluginInfo
    {
        public string Name { get; set; }
        public Type[] Interfaces { get; set; }
        public Type Plugin { get; set; }
        //public ConstructorInfo Ctor { get; set; }
    }

    //internal class ParamCtorMap
    //{
    //    public string ParamHash { get; set; }
    //    public ConstructorInfo Ctor { get; set; }
    //}

    /// <summary>
    ///     This is a nice wrapper around and plugins we are using for Server or Client.  Performs assembly sniffing, caching, and instantation
    /// </summary>
    internal class PluginManager
    {
        private const string ASM_PREFIX = "GeoDecisions.Esb";

        private static readonly object _lockObj = new object();
        private static PluginManager _pluginManager;

        static PluginManager()
        {
            lock (_lockObj)
            {
                if (_pluginManager == null)
                    _pluginManager = new PluginManager();
            }
        }

        private PluginManager()
        {
            PluginInfos = new SynchronizedCollection<PluginInfo>();

            Type pluginType = typeof (IPlugin);

            // read all plugins defined internally for the Esb (this will not load external dlls)
            
            Console.WriteLine("Getting Asms");

            Assembly[] allAsms = AppDomain.CurrentDomain.GetAssemblies();
            List<Assembly> ourAsms = allAsms.Where(asm => asm.FullName.StartsWith(ASM_PREFIX)).ToList();

            try
            {
                ourAsms.ForEach(asm =>
                    {
                        //see if there are any IPlugins defined
                        List<TypeInfo> pluginTypes = asm.DefinedTypes.Where(type => pluginType.IsAssignableFrom(type) && type.IsClass && type.IsAbstract == false).ToList();

                        pluginTypes.ForEach(ti =>
                            {
                                Type[] interfaces = ti.GetInterfaces();

                                PluginInfos.Add(new PluginInfo {Interfaces = interfaces, Name = ti.FullName, Plugin = ti});
                            });
                    });

            }
            catch (ReflectionTypeLoadException loadEx)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(loadEx.Message);

                if (loadEx.LoaderExceptions != null)
                {
                    Console.WriteLine("Loader Exceptions:");
                    loadEx.LoaderExceptions.ToList().ForEach(ex =>
                        {
                            Console.WriteLine(ex.Message);
                        });
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(ex.Message);
                throw;
            }

            //AppDomain.CurrentDomain.RelativeSearchPath
        }

        private static PluginManager Instance
        {
            get
            {
                if (_pluginManager == null)
                    _pluginManager = new PluginManager();

                return _pluginManager;
            }
        }

        private SynchronizedCollection<PluginInfo> PluginInfos { get; set; }

        public static IEnumerable<T> GetAll<T>(params object[] parameters) where T : IPlugin
        {
            if (typeof (T).IsInterface)
            {
                IEnumerable<PluginInfo> pluginInfos = Instance.PluginInfos.Where(pi => pi.Interfaces.Any(type => type == typeof (T)));

                Type[] ctorParamTypes = parameters.Select(o => o.GetType()).ToArray();

                IEnumerable<T> instances = pluginInfos.ToList().Select(pi =>
                    {
                        ConstructorInfo ctor = pi.Plugin.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, ctorParamTypes, null);

                        if (ctor != null)
                        {
                            // maybe we can cache the ctor for this type once we have it???
                            object plugin = ctor.Invoke(parameters);
                            return (T) plugin;
                        }

                        // try to get default empty ctor
                        ctor = pi.Plugin.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

                        if (ctor != null)
                        {
                            object plugin = ctor.Invoke(new object[] {});
                            return (T) plugin;
                        }

                        return default(T);
                    }).Where(obj => obj != null); // only take non-nulls

                return instances;
            }
            return null;
        }


        public static T GetOneOf<T>(params object[] parameters) where T : IPlugin
        {
            // need to find implementation of Interface T, if there are more than 1, find by name??
            if (typeof (T).IsInterface)
            {
                PluginInfo pluginInfo = Instance.PluginInfos.FirstOrDefault(pi => pi.Interfaces.Any(type => type == typeof (T)));

                if (pluginInfo != null)
                {
                    Type[] ctorParamTypes = parameters.Select(o => o.GetType()).ToArray();

                    ConstructorInfo ctor = pluginInfo.Plugin.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, ctorParamTypes, null);

                    if (ctor != null)
                    {
                        // maybe we can cache the ctor for this type once we have it???
                        object plugin = ctor.Invoke(parameters);
                        return (T) plugin;
                    }
                }
            }

            return default(T);
        }
    }
}
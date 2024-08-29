using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.ComponentModel;
using UnityEditor;

public class ServiceLocator : MonoBehaviour
{
    private static ServiceLocator global;
    private static Dictionary<Scene, ServiceLocator> sceneContainers;
    private static List<GameObject> TmpSceneGameObjects;

    private readonly ServiceManager services = new ServiceManager();
    private const string const_globalServiceLocatorName = "ServiceLocator [Global]";
    private const string const_sceneServiceLocatorName = "ServiceLocator [Scene]";

    internal void ConfigureAsGlobal(bool dontDestroyOnLoad)
    {
        if (global == this)
            Debug.LogWarning($"ServiceLocator.ConfigureAsGlobal : Already configured as global",this);
        else if (global != null)
            Debug.LogError($"ServiceLocator.ConfigureAsGlobal : Already configured as global",this);
        else
        {
            global = this;
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
        }
    }

    internal void ConfigureForScene()
    {
        Scene scene = gameObject.scene;
        if (sceneContainers.ContainsKey(scene))
        {
            /*This is like this at the moment because I don't make more logic for the scenes.*/
            Debug.LogError($"ServiceLocator.ConfigureForScene : Another ServiceLocator is already configured for this scene",this);
            return;
        }
    }


    public static ServiceLocator Global
    {
        get
        {
            if (global != null) return global;

            // bootstrap or initialize the new instance of global as necessary
            if (FindFirstObjectByType<ServiceLocatorGlobalBootstrapper>() is { } found)
            {
                found.BootstrapOnDemand();
                return global;
            }

            var container = new GameObject(const_globalServiceLocatorName, typeof(ServiceLocator));
            container.AddComponent<ServiceLocatorGlobalBootstrapper>().BootstrapOnDemand();
            return global;
        }
    }

    public static ServiceLocator For(MonoBehaviour mb)
    {
        return mb.GetComponentInParent<ServiceLocator>() ?? ForSceneOf(mb) ?? global;
    }

    public static ServiceLocator ForSceneOf(MonoBehaviour mb)
    {
        Scene scene = mb.gameObject.scene;
        if (sceneContainers.TryGetValue(scene, out ServiceLocator container) && container != mb)
            return container;

        TmpSceneGameObjects.Clear();
        scene.GetRootGameObjects(TmpSceneGameObjects);

        foreach (GameObject gmeObj in TmpSceneGameObjects.Where(gmeObj => gmeObj.GetComponent<ServiceLocatorSceneBootstrapper>() != null))
        {
            if (gmeObj.TryGetComponent(out ServiceLocatorSceneBootstrapper bootstrapper) && bootstrapper.Container != mb)
            {
                bootstrapper.BootstrapOnDemand();
                return bootstrapper.Container;
            }
        }

        return global;
    }

    public ServiceLocator Register<T>(T service)
    {
        services.Register(service);
        Debug.Log($"ServiceLocator.Register : Registered Service of type : " + typeof(T));
        return this;
    }
    
    public ServiceLocator Register<T>(Type type,object service)
    {
        services.Register(type,service);
        Debug.Log($"ServiceLocator.Register : Registered Service of type : " + type  + " in " + service);
        return this;
    }
    
    public void Remove<T>(T service)
    {
        services.Remove(typeof(T));
        Debug.Log($"ServiceLocator.Remove : Registered Service of type : " + typeof(T));
    }
    
    public ServiceLocator Get<T>(out T service) where T : class
    {
        if (TryGetService(out service)) 
            return this;

        if (TryGetNextInHerarchy(out ServiceLocator container))
        {
            container.Get(out service);
            return this;
        }
        throw new ArgumentException($"Service Locator.Get : Service of Type {typeof(T).FullName} not registered");
    }

    private bool TryGetService<T>(out T service) where T : class
    {
        return services.TryGet(out service);
    }

    private bool TryGetNextInHerarchy(out ServiceLocator container)
    {
        if (this == global)
        {
            container = null;
            return false;
        }

        container = transform.parent ? .GetComponent<ServiceLocator>() ?? ForSceneOf(this);
        return container != null;
    }

    private void OnDestroy()
    {
        if (this == global)
            global = null;
        else if (sceneContainers.ContainsValue(this))
            sceneContainers.Remove(gameObject.scene);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        /*This is to clean the global variables when
         we exit play mode because Unity does not guarantee that these variables are cleaned*/
        
        global = null;
        sceneContainers = new Dictionary<Scene, ServiceLocator>();
        TmpSceneGameObjects = new List<GameObject>();
    }

#if UNITY_EDITOR
    [MenuItem("GameObject/ServiceLocator/Add Global")]
    static void AddGlobal()
    {
        GameObject globalGo = new GameObject(const_globalServiceLocatorName,typeof(ServiceLocatorGlobalBootstrapper));
    }
    
    [MenuItem("GameObject/ServiceLocator/Add Scene")]
    static void AddScene()
    {
        GameObject globalGo = new GameObject(const_sceneServiceLocatorName,typeof(ServiceLocatorSceneBootstrapper));
    }
#endif
}
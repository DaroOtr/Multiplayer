using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ServiceLocator))]
public abstract class Bootstrapper : MonoBehaviour
{
   private ServiceLocator container;
   internal ServiceLocator Container => container  ?? (container = GetComponent<ServiceLocator>());

   private bool hasBeenBootstrapped;

   private void Awake() => BootstrapOnDemand();

   public void BootstrapOnDemand()
   {
      if (hasBeenBootstrapped) return;

      hasBeenBootstrapped = true;
      Bootstrap();
   }

   protected abstract void Bootstrap();
}

[AddComponentMenu("ServiceLocator/ServiceLocator Global")]
public class ServiceLocatorGlobalBootstrapper : Bootstrapper
{
   [SerializeField] private bool dontDestroyOnLoad = true;
   protected override void Bootstrap()
   {
      Container.ConfigureAsGlobal(dontDestroyOnLoad);
   }
}

[AddComponentMenu("ServiceLocator/ServiceLocator Scene")]
public class ServiceLocatorSceneBootstrapper : Bootstrapper
{
   protected override void Bootstrap()
   {
      Container.ConfigureForScene();
   }
}

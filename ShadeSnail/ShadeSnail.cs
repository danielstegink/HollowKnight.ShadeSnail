using Modding;
using System.Collections.Generic;
using UnityEngine;

namespace ShadeSnail
{
    public class ShadeSnail : Mod, IGlobalSettings<GlobalSettings>, ICustomMenuMod
    {
        public static ShadeSnail Instance;

        #region Global Settings
        public static GlobalSettings globalSettings = new GlobalSettings();

        public void OnLoadGlobal(GlobalSettings s)
        {
            globalSettings = s;
        }

        public GlobalSettings OnSaveGlobal()
        {
            return globalSettings;
        }
        #endregion

        public override string GetVersion() => "1.1.0.0";

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            Instance = this;
            ShadeHelper.ApplyHooks();

            Log("Initialized");
        }

        #region Menu
        public bool ToggleButtonInsideMenu => false;

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            return ModMenu.CreateMenuScreen(modListMenu);
        }
        #endregion
    }
}
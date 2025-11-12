using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShadeSnail
{
    public class ShadeSnail : Mod
    {
        public static ShadeSnail Instance;

        public override string GetVersion() => "1.0.0.0";

        /// <summary>
        /// Stores prefab for the shade
        /// </summary>
        private GameObject shadePrefab;

        /// <summary>
        /// Name for the shade
        /// </summary>
        private string shadeName = "ShadeSnail.Shade";

        private bool spawningShade = false;

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            Instance = this;

            On.HeroController.Update += SpawnShade;
            On.HealthManager.TakeDamage += ImmortalShade;
            On.HutongGames.PlayMaker.Actions.BoolTest.OnEnter += NotFriendly;
            On.HutongGames.PlayMaker.Actions.BoolAllTrue.OnEnter += PursuePlayer;

            Log("Initialized");
        }

        /// <summary>
        /// Spawn Shade whenever it doesn't exist
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void SpawnShade(On.HeroController.orig_Update orig, HeroController self)
        {
            orig(self);

            if (shadePrefab == null)
            {
                Log("Building prefab");
                shadePrefab = UnityEngine.Object.Instantiate(GameManager.instance.sm.hollowShadeObject);
                shadePrefab.name = shadeName;
                shadePrefab.SetActive(false);
                UnityEngine.GameObject.DontDestroyOnLoad(shadePrefab);
            }

            GameObject shadeSnail = UnityEngine.Object.FindObjectsOfType<GameObject>()
                                                        .Where(x => x.name.StartsWith(shadeName))
                                                        .FirstOrDefault();
            if (shadeSnail == default &&
                !spawningShade)
            {
                spawningShade = true;
                GameManager.instance.StartCoroutine(SpawnShade());
            }
            else if (shadeSnail != default) // If Shade Snail lags behind or get stuck, destroy it and we will make a new one
            {
                if (GetDistance(shadeSnail, HeroController.instance.gameObject) > 25)
                {
                    UnityEngine.GameObject.Destroy(shadeSnail);
                }
            }
        }

        /// <summary>
        /// Spawns Shade after 1 second so player has time to get away
        /// </summary>
        /// <returns></returns>
        private IEnumerator SpawnShade()
        {
            // Wait 1 second, then spawn it at the player's last location
            Vector3 heroPosition = HeroController.instance.gameObject.transform.position;
            yield return new WaitForSeconds(1);

            GameObject shadeSnail = UnityEngine.Object.Instantiate(shadePrefab, heroPosition, Quaternion.identity);
            shadeSnail.SetActive(true);
            //Log($"{shadeSnail.name} spawned at {heroPosition}");
            spawningShade = false;
        }

        private float GetDistance(GameObject shade, GameObject player)
        {
            Transform shadePosition = shade.transform;
            Transform playerPosition = player.transform;

            float xDiff = Math.Abs(shadePosition.GetPositionX() - playerPosition.GetPositionX());
            float yDiff = Math.Abs(shadePosition.GetPositionY() - playerPosition.GetPositionY());
            return (float)Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
        }

        /// <summary>
        /// Shade should not be killable
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="hitInstance"></param>
        private void ImmortalShade(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.gameObject.name.StartsWith(shadeName))
            {
                self.hp += hitInstance.DamageDealt;
                //Log($"Shade hp increased to {self.hp}");
            }

            orig(self, hitInstance);
        }

        /// <summary>
        /// Shade is not friendly
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void NotFriendly(On.HutongGames.PlayMaker.Actions.BoolTest.orig_OnEnter orig, HutongGames.PlayMaker.Actions.BoolTest self)
        {
            bool defaultValue = self.boolVariable.Value;

            // The key tests of the Friendly variable is the BoolTest action of the Startle and Idle states
            bool isFriendlyTest = self.Fsm.GameObjectName.StartsWith(shadeName) &&
                                    (self.State.Name.Equals("Startle") ||
                                        self.State.Name.Equals("Idle"));
            if (isFriendlyTest)
            {
                self.boolVariable.Value = false;
            }

            orig(self);

            // Remember to reset afterwards so this doesn't break anything
            if (isFriendlyTest)
            {
                self.boolVariable.Value = defaultValue;
                //Log($"Shade friendliness reset to {self.boolVariable.Value}");
            }
        }

        /// <summary>
        /// Shade should pursue player at all times
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void PursuePlayer(On.HutongGames.PlayMaker.Actions.BoolAllTrue.orig_OnEnter orig, HutongGames.PlayMaker.Actions.BoolAllTrue self)
        {
            orig(self);

            // The first check to make sure the Shade follows the player is in the final action of the Idle state
            bool isAlertTest = self.Fsm.GameObjectName.StartsWith(shadeName) &&
                                self.State.Name.Equals("Idle");
            if (isAlertTest)
            {
                self.Fsm.Event(self.sendEvent);
            }
        }
    }
}
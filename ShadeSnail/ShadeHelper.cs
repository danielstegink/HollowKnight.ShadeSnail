using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace ShadeSnail
{
    public static class ShadeHelper
    {
        /// <summary>
        /// Name for the shade
        /// </summary>
        internal static string shadeName = "ShadeSnail.Shade";

        /// <summary>
        /// Whether or not we've already triggered the shade spawn
        /// </summary>
        internal static bool spawningShade = false;

        public static void ApplyHooks()
        {
            On.HeroController.Update += SpawnShade;
            On.HeroController.Start += Reset;

            On.HealthManager.TakeDamage += ImmortalShade;
            On.HutongGames.PlayMaker.Actions.BoolTest.OnEnter += NotFriendly;
            On.HutongGames.PlayMaker.Actions.BoolAllTrue.OnEnter += PursuePlayer;
        }

        #region Spawn Shade
        /// <summary>
        /// Spawn Shade whenever it doesn't exist
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        internal static void SpawnShade(On.HeroController.orig_Update orig, HeroController self)
        {
            orig(self);

            // Find the shade (if it exists)
            GameObject shadeSnail = GetShade();

            // Spawn the shade if the necessary conditions are met:
            // the global setting is enabled
            // the shade doesn't already exist
            // the spawning sequence isn't in progress
            // the player has control of their body and is able to start running
            if (ShadeSnail.globalSettings.spawnShade && 
                shadeSnail == null &&
                !spawningShade &&
                self.acceptingInput)
            {
                //ShadeSnail.Instance.Log("Spawning shade");
                spawningShade = true;
                GameManager.instance.StartCoroutine(SpawnShadeAfterDelay());
            }

            if (shadeSnail != null) 
            {
                // If Shade Snail lags behind or get stuck, destroy it so we can make a new one
                // Also destroy if the global setting is disabled
                if (GetDistance(shadeSnail, HeroController.instance.gameObject) > 25 ||
                    !ShadeSnail.globalSettings.spawnShade)
                {
                    DestroyShade();
                }
            }

            GameObject shadeMusic = UnityEngine.GameObject.Find("Shade");
            if (shadeMusic != null)
            {
                AudioSource audioSource = shadeMusic.GetComponent<AudioSource>();
                audioSource.mute = ShadeSnail.globalSettings.spawnShade;
            }
        }

        /// <summary>
        /// Spawns Shade after a short delay so player has time to get away
        /// </summary>
        /// <returns></returns>
        internal static IEnumerator SpawnShadeAfterDelay()
        {
            // Track the player's position, so we can spawn the Shade nearby
            Vector3 heroPosition = HeroController.instance.gameObject.transform.position;
            heroPosition.y += 2f;

            // Wait an appropriate length of time before spawning the Shade so the player doesn't get spawn-camped
            float waitSeconds = 1.5f;
            //ShadeSnail.Instance.Log($"Starting wait: {waitSeconds}");
            yield return new WaitForSeconds(waitSeconds);

            GameObject shadeSnail = UnityEngine.Object.Instantiate(GameManager.instance.sm.hollowShadeObject,
                                                                    heroPosition, Quaternion.identity);
            shadeSnail.name = shadeName;
            //ShadeSnail.Instance.Log($"{shadeSnail.name} spawned at {heroPosition}");
            spawningShade = false;
        }

        /// <summary>
        /// When we start a new save, make sure to reset everything
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void Reset(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);

            DestroyShade();
            spawningShade = false;
        }
        #endregion

        #region Spawn Shade Helpers
        /// <summary>
        /// Gets the current shade, if they exist
        /// </summary>
        /// <returns></returns>
        private static GameObject GetShade()
        {
            return UnityEngine.Object.FindObjectsOfType<GameObject>()
                                        .Where(x => x.name.Equals(shadeName))
                                        .FirstOrDefault();
        }

        /// <summary>
        /// Determines the distance between the shade and the player
        /// </summary>
        /// <param name="shade"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        internal static float GetDistance(GameObject shade, GameObject player)
        {
            Transform shadePosition = shade.transform;
            Transform playerPosition = player.transform;

            float xDiff = Math.Abs(shadePosition.GetPositionX() - playerPosition.GetPositionX());
            float yDiff = Math.Abs(shadePosition.GetPositionY() - playerPosition.GetPositionY());
            return (float)Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
        }

        /// <summary>
        /// Destroys the shade (if it exists)
        /// </summary>
        private static void DestroyShade()
        {
            GameObject shadeSnail = GetShade();
            if (shadeSnail != null)
            {
                UnityEngine.GameObject.Destroy(shadeSnail);
            }
        }
        #endregion

        #region Shade Behavior
        /// <summary>
        /// Shade should not be killable
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="hitInstance"></param>
        internal static void ImmortalShade(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.gameObject.name.StartsWith(shadeName))
            {
                self.hp += hitInstance.DamageDealt + 1;
                //ShadeSnail.Instance.Log($"Shade hp increased to {self.hp}");
            }

            orig(self, hitInstance);
        }

        /// <summary>
        /// Shade is not friendly
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal static void NotFriendly(On.HutongGames.PlayMaker.Actions.BoolTest.orig_OnEnter orig, HutongGames.PlayMaker.Actions.BoolTest self)
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
        internal static void PursuePlayer(On.HutongGames.PlayMaker.Actions.BoolAllTrue.orig_OnEnter orig, HutongGames.PlayMaker.Actions.BoolAllTrue self)
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
        #endregion
    }
}

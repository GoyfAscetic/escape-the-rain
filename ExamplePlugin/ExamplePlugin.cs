using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ExamplePlugin
{
    // This is an example plugin that can be put in
    // BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    // It's a small plugin that adds a relatively simple item to the game,
    // and gives you that item whenever you press F2.

    // This attribute specifies that we have a dependency on a given BepInEx Plugin,
    // We need the R2API ItemAPI dependency because we are using for adding our item to the game.
    // You don't need this if you're not using R2API in your plugin,
    // it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(ItemAPI.PluginGUID)]

    // This one is because we use a .language file for language tokens
    // More info in https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]

    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    // This is the main declaration of our plugin class.
    // BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    // BaseUnityPlugin itself inherits from MonoBehaviour,
    // so you can use this as a reference for what you can declare and use in your plugin class
    // More information in the Unity Docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class ExamplePlugin : BaseUnityPlugin
    {
        // The Plugin GUID should be a unique ID for this plugin,
        // which is human readable (as it is used in places like the config).
        // If we see this PluginGUID as it is on thunderstore,
        // we will deprecate this mod.
        // Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "AuthorName";
        public const string PluginName = "ExamplePlugin";
        public const string PluginVersion = "1.0.0";

        // We need our item definition to persist through our functions, and therefore make it a class field.
        private static ItemDef myItemDef;

        public static readonly string DefaultHighlightColor = "#b3b3b3";
        public TickDebuffHandler tickDebuffHandler;

        public static ConfigEntry<bool> ConfigLogPlayers;
        public static ConfigEntry<bool> ConfigLogAllies;
        public static ConfigEntry<bool> ConfigLogUtility;
        public static ConfigEntry<bool> ConfigLogDebuff;
        public static ConfigEntry<bool> ConfigLogFallDamage;
        public static ConfigEntry<bool> ConfigLogShrinesOfBlood;
        public static ConfigEntry<int> ConfigHpPercentageFilter;

        public static ConcurrentDictionary<CharacterBody, int> dmgPerSource;

        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            // Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            // First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            myItemDef.name = "EXAMPLE_CLOAKONKILL_NAME";
            myItemDef.nameToken = "EXAMPLE_CLOAKONKILL_NAME";
            myItemDef.pickupToken = "EXAMPLE_CLOAKONKILL_PICKUP";
            myItemDef.descriptionToken = "EXAMPLE_CLOAKONKILL_DESC";
            myItemDef.loreToken = "EXAMPLE_CLOAKONKILL_LORE";

            // The tier determines what rarity the item is:
            // Tier1=white, Tier2=green, Tier3=red, Lunar=Lunar, Boss=yellow,
            // and finally NoTier is generally used for helper items, like the tonic affliction
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            // Instead of loading the itemtierdef directly, you can also do this like below as a workaround
            // myItemDef.deprecatedTier = ItemTier.Tier2;

            // You can create your own icons and prefabs through assetbundles, but to keep this boilerplate brief, we'll be using question marks.
            myItemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            myItemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();

            // Can remove determines
            // if a shrine of order,
            // or a printer can take this item,
            // generally true, except for NoTier items.
            myItemDef.canRemove = true;

            // Hidden means that there will be no pickup notification,
            // and it won't appear in the inventory at the top of the screen.
            // This is useful for certain noTier helper items, such as the DrizzlePlayerHelper.
            myItemDef.hidden = false;

            // You can add your own display rules here,
            // where the first argument passed are the default display rules:
            // the ones used when no specific display rules for a character are found.
            // For this example, we are omitting them,
            // as they are quite a pain to set up without tools like https://thunderstore.io/package/KingEnderBrine/ItemDisplayPlacementHelper/
            var displayRules = new ItemDisplayRuleDict(null);

            // Then finally add it to R2API
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));

            // But now we have defined an item, but it doesn't do anything yet. So we'll need to define that ourselves.
            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;

            ConfigLogPlayers = Config.Bind(
               "EnemyHitLog.Toggles",
               "Player",
               true,
               "Whether or not to log Players\n"
           );
            ConfigLogAllies = Config.Bind(
                "EnemyHitLog.Toggles",
                "Ally",
                false, "Whether or not to log Allies, like Engineer Turrets, Beetle Guards or Aurelionite.\n"
            );
            ConfigLogUtility = Config.Bind(
                "EnemyHitLog.Toggles",
                "Utility",
                false,
                "Whether or not to log Drones and Turrets that were bought during a run.\n"
            );
            ConfigLogDebuff = Config.Bind(
                "EnemyHitLog.Toggles",
                "Debuffs",
                true,
                "Whether or not to log Debuffs.\n"
            );
            ConfigLogFallDamage = Config.Bind(
                "EnemyHitLog.Toggles",
                "FallDamage",
                true,
                "Whether or not to log FallDamage."
            );
            ConfigLogShrinesOfBlood = Config.Bind(
                "EnemyHitLog.Toggles",
                "ShrinesOfBlood",
                false,
                "Whether or not to log sacrificed HP on a Shrine of Blood."
            );
            ConfigHpPercentageFilter = Config.Bind(
                "EnemyHitLog.Filter",
                "DamageToMaxHealthThreshold",
                0,
                "Do not log any damage which has a lower value than the given percentage of the Player's HP.\n\nFor example, if this variable is 10, only damage as high as at least 10% of the Player's max. HP (not counting barrier and shield) will be logged to the chat.\n\nNote: Splash damage results in being ignored as well, since each splash is a separate Hit á e.g. 5 Damage. Hopefully I will find the time to try to fix this in the future...\n"
            );


            dmgPerSource = [];


            //if (!AtLeastOnePlayerTeamLogEnabled())
            //    return;

            if (ConfigHpPercentageFilter.Value < 0)
                ConfigHpPercentageFilter.Value = 0;

            tickDebuffHandler = new TickDebuffHandler();

            // Subscribe to the global damage event
            On.RoR2.GlobalEventManager.ServerDamageDealt += Event_ServerDamageDealt;
            On.RoR2.GlobalEventManager.OnCharacterDeath += Event_OnCharacterDeath;
        }

        private void Event_OnCharacterDeath(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            Log.Info("DEATH");
            IterateDmgPerSource();
        }

        private void IterateDmgPerSource()
        {
            if (dmgPerSource == null || dmgPerSource.IsEmpty)
            {
                Log.Info("dmgPerSource is empty or null.");
                return;
            }

            foreach (var kvp in dmgPerSource.ToArray())
            {
                var body = kvp.Key;
                var totalDamage = kvp.Value;

                string sourceName = body != null
                    ? (body.isPlayerControlled ? body.GetUserName() : body.GetDisplayName())
                    : "null";

                string netId = body?.master?.netId.ToString() ?? "null";

                Log.Info($"Source: {sourceName} (netId={netId}) - Damage: {totalDamage}");
            }
        }


        private void Event_ServerDamageDealt(On.RoR2.GlobalEventManager.orig_ServerDamageDealt orig, DamageReport damageReport)
        {
            Log.Info("121 - orig(damageReport)");

            orig(damageReport);

            Log.Info("125 - DamageReportHandler damageReportHandler = new DamageReportHandler(damageReport)");

            DamageReportHandler damageReportHandler = new DamageReportHandler(damageReport);
            if (damageReportHandler == null)
                Log.Info("damageReportHandler is null");
            if (ConfigHpPercentageFilter == null)
                Log.Info("ConfigHpPercentageFilter is null");
            Chat.SimpleChatMessage chatMessage = null;
            int hitPointPercentage = ConfigHpPercentageFilter.Value;

            string teamEntityLabel;
            string enemyEntityLabel;

            Log.Info("138 - if (!damageReportHandler.VictimIsInPlayerTeam() || damageReport.damageInfo.rejected)");

            if (!damageReportHandler.VictimIsInPlayerTeam() || damageReport.damageInfo.rejected)
                return;

            Log.Info($"142 - damageReportHandler.victim {damageReportHandler}");
            teamEntityLabel = GetTeamEntityLabel(damageReportHandler.Victim);
            if (teamEntityLabel == null)
                return;
            // Fall Damage
            Log.Info("147 - Damage source");
            if (damageReportHandler.CheckIfFallDamageBroadcast(hitPointPercentage)
                && AtLeastOnePlayerTeamLogEnabled()
                && ConfigLogFallDamage.Value)
            {
                Log.Info("Fall Damage");
                string msgText = $"{teamEntityLabel}: Crashed into the ground, Lost <color=#ff4000>{damageReportHandler.Damage}</color> HP";
                chatMessage = ComposeMessage(msgText);
            }
            // Shrines of Blood
            else if (damageReportHandler.CheckIfShrineBloodDamageBroadcast(hitPointPercentage)
                && AtLeastOnePlayerTeamLogEnabled()
                && ConfigLogFallDamage.Value)
            {
                Log.Info("Shrine of Blood");
                string msgText = $"{teamEntityLabel} paid the Shrine of blood, Sacrificed <color=#ff4000>{damageReportHandler.Damage}</color> HP";
                chatMessage = ComposeMessage(msgText);
            }
            // DoT Debuffs
            else if (tickDebuffHandler.IsTickDamageEvent(damageReportHandler)
                && AtLeastOnePlayerTeamLogEnabled()
                && ConfigLogDebuff.Value)
            {
                Log.Info("Dot Debuff");
                enemyEntityLabel = tickDebuffHandler.ComposeLabel(damageReportHandler.TickingDebuffIndex);
                if (enemyEntityLabel == null)
                    return;
                Log.Info($"DEBUFF Adding {damageReportHandler.Damage} from {damageReportHandler.Attacker} dmgPerSource");
                dmgPerSource.AddOrUpdate(damageReportHandler.Attacker, damageReportHandler.Damage, (_, old) => old + damageReportHandler.Damage);
                chatMessage = ComposeMessage($"{teamEntityLabel}: {enemyEntityLabel}");
            }
            // Normal Damage
            else if (damageReportHandler.CheckIfDamageBroadcast(hitPointPercentage)
                && damageReportHandler.Attacker != null
                && AtLeastOnePlayerTeamLogEnabled())
            {
                Log.Info("Normal Dmg");
                enemyEntityLabel = GetAttackerMsg(damageReportHandler);
                if (enemyEntityLabel == null)
                    return;
                Log.Info($"DAMAGE Adding {damageReportHandler.Damage} from {damageReportHandler.Attacker} dmgPerSource");
                dmgPerSource.AddOrUpdate(damageReportHandler.Attacker, damageReportHandler.Damage, (_, old) => old + damageReportHandler.Damage);
                chatMessage = ComposeMessage($"{teamEntityLabel}: Hit By {enemyEntityLabel}, Lost <color=#ff4000>{damageReportHandler.Damage}</color> HP)");
            } else
            {
                Log.Info("No conditions met for logging damage.");
            }
            IterateDmgPerSource();

            if (chatMessage != null)
                Chat.SendBroadcastChat(chatMessage);
        }

        private string GetAttackerMsg(DamageReportHandler dmgHandler)
        {
            BuffIndex attackerBuffIndex = BuffIndex.None;
            BuffDef attackerBuff = null;


            foreach (BuffIndex aBuff in BuffCatalog.eliteBuffIndices)
            {
                if (dmgHandler.Attacker.HasBuff(aBuff))
                {
                    attackerBuffIndex = aBuff;
                    attackerBuff = BuffCatalog.GetBuffDef(attackerBuffIndex);
                    break;
                }
            }

            // Replace the switch statement with a series of if-else statements for buff checks
            if (dmgHandler.Attacker.HasBuff(RoR2Content.Buffs.AffixRed))
                return __ComposeEnemyEntityLabel(dmgHandler, "#b30000", "Blazing");
            else if (dmgHandler.Attacker.HasBuff(RoR2Content.Buffs.AffixHaunted))
                return __ComposeEnemyEntityLabel(dmgHandler, "#99ffbb", "Celestine");
            else if (dmgHandler.Attacker.HasBuff(RoR2Content.Buffs.AffixWhite))
                return __ComposeEnemyEntityLabel(dmgHandler, "#98e4ed", "Glacial");
            else if (dmgHandler.Attacker.HasBuff(RoR2Content.Buffs.AffixPoison))
                return __ComposeEnemyEntityLabel(dmgHandler, "#008000", "Malachite");
            else if (dmgHandler.Attacker.HasBuff(RoR2Content.Buffs.AffixBlue))
                return __ComposeEnemyEntityLabel(dmgHandler, "#0066cc", "Overloading");
            else if (dmgHandler.Attacker.HasBuff(RoR2Content.Buffs.AffixLunar))
                return __ComposeEnemyEntityLabel(dmgHandler, "#364d63", "Perfected");
            // if (dmgHandler.Attacker.HasBuff(RoR2Content.Buffs.AffixAurelionite))
            //     return __ComposeEnemyEntityLabel(dmgHandler, "#ffcd2c", "Gilded");
            //else if (dmgHandler.Attacker.HasBuff(RoR2Content.Buffs.AffixEarth))
            //    return __ComposeEnemyEntityLabel(dmgHandler, "#a1e74f", "Mending");
            //else if (dmgHandler.Attacker.HasBuff(RoR2Content.Buffs.AffixBead))
            //    return __ComposeEnemyEntityLabel(dmgHandler, "#004eef", "Twisted");
            // else if (dmgHandler.Attacker.HasBuff(RoR2Content.Buffs.<VoidtouchedBuff>))
            //     return __ComposeEnemyEntityLabel(dmgHandler, "#db71ed", "Voidtouched");
            else
                return __ComposeEnemyEntityLabel(dmgHandler, DefaultHighlightColor);
        }

        private string __ComposeEnemyEntityLabel(DamageReportHandler dmgHandler, string hexColor, string affixLabel = "")
        {
            string name = dmgHandler.Attacker.GetDisplayName();

            if (dmgHandler.AttackIsFriendlyFire() && IsRealPlayer(dmgHandler.Attacker))
                name = dmgHandler.Attacker.GetUserName();
            
            string affix = affixLabel;
            if (affixLabel != "") 
                affix += " ";

            return $"<color={hexColor}>{affix}{name}</color>";
        }

        private Chat.SimpleChatMessage ComposeMessage(string baseToken)
        {
            return new Chat.SimpleChatMessage
            {
                baseToken = baseToken
            };
        }

        private bool AtLeastOnePlayerTeamLogEnabled()
        {
            return ConfigLogPlayers.Value || ConfigLogAllies.Value || ConfigLogUtility.Value;
        }

        private string GetTeamEntityLabel(CharacterBody teamMember)
        { 
            CharacterBody sourceBody = null;
            if (!IsRealPlayer(teamMember))
            {
                if (MinionTogglesAreDisabled()) 
                    return null;

                CharacterMaster minionOwner = TryResolveMinionOwnerMaster(teamMember);
                if (minionOwner == null)
                    return null;

                sourceBody = minionOwner.GetBody();

            }
            if (sourceBody == null)
                Log.Info("sourceBody is Null");
            if (teamMember == null)
                Log.Info("teamMember is Null");
            return ComposeVictimLabel(teamMember, sourceBody);
        }
        
        private bool IsRealPlayer(CharacterBody somebody)
        {
            return somebody && somebody.isPlayerControlled;
        }

        private bool MinionTogglesAreDisabled()
        {
            return !ConfigLogAllies.Value && !ConfigLogUtility.Value;
        }

        private CharacterMaster TryResolveMinionOwnerMaster(CharacterBody somebody)
        {
            CharacterMaster minionOwner = null;
            foreach(PlayerCharacterMasterController pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc.master && pcmc.master.netId == somebody.master.minionOwnership.NetworkownerMasterId)
                {
                    minionOwner = pcmc.master;
                    break;
                }
            }
            return minionOwner;
        }

        private string ComposeVictimLabel(CharacterBody somebody, CharacterBody source = null)
        {
            DataContainer characterData = DataCatalog.GetCharacterDataFor(somebody);

            //TODO The original code does not check the second body should we add one?
            if (!characterData.DataContainerTypeIsEnabled())
                return null;

            string prefixColor = characterData.ColorHex;
            string prefixName = somebody.GetUserName();
            string suffixColor = characterData.ColorHex;
            string suffixName = characterData.TruncatedDisplayName;
            if (source != null)
            {
                DataContainer sourceData = DataCatalog.GetCharacterDataFor(source);
                prefixColor = sourceData.ColorHex;
                prefixName = source.GetUserName();
            }

            return $"<color={prefixColor}>{prefixName}</color> [<color={suffixColor}>{suffixName}</color>]";
        }

        private void OnTakeDamageGlobal(DamageReport report, DamageInfo damageInfo, CharacterBody victimBody)
        {
            // Check if the damaged body is a player
            if (victimBody && victimBody.isPlayerControlled)
            {
                Log.Info($"Player {victimBody.GetUserName()} took {damageInfo.damage} damage from {damageInfo.attacker?.name ?? "Unknown"}.");
            }
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        {
            // If a character was killed by the world, we shouldn't do anything.
            if (!report.attacker || !report.attackerBody)
            {
                return;
            }

            var attackerCharacterBody = report.attackerBody;

            // We need an inventory to do check for our item
            if (attackerCharacterBody.inventory)
            {
                // Store the amount of our item we have
                var garbCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                if (garbCount > 0 &&
                    // Roll for our 50% chance.
                    Util.CheckRoll(50, attackerCharacterBody.master))
                {
                    // Since we passed all checks, we now give our attacker the cloaked buff.
                    // Note how we are scaling the buff duration depending on the number of the custom item in our inventory.
                    attackerCharacterBody.AddTimedBuff(RoR2Content.Buffs.Cloak, 3 + garbCount);
                }
            }
        }

        // The Update() method is run on every frame of the game.
        private void Update()
        {
            // This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop our defined item in front of the player.

                Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(myItemDef.itemIndex), transform.position, transform.forward * 20f);
            }
        }
    }
}

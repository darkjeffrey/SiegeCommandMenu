using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace SiegeCommandMenu
{
    /// <summary>
    /// The panel beside the left game menu (game menus can only show text, not pictures). It is a
    /// Gauntlet layer on the map screen that closes itself when the slot menu is no longer showing.
    /// Cards can be clickable; the layer then takes mouse input (and only mouse input).
    /// </summary>
    internal static class SiegeInfoOverlay
    {
        private const string SlotMenuId = "scm_slot";
        private const int GraceTicks = 10;

        private static GauntletLayer _layer;
        private static ScreenBase _screen;
        private static Action _release;
        private static int _mismatchTicks;
        private static bool _restricted;
        private static Action _queued;

        /// <summary>A card for one engine type. A non-null action makes the card clickable.</summary>
        public static SiegeEngineCardVM Card(SiegeEvent siege, SiegeEngineType type, string actionText, Action action,
            SiegeEvent.SiegeEngineConstructionProgress engine = null)
        {
            // For an engine that exists, show its current health instead of the base value.
            string health = SiegeActions.HealthText(engine);
            string line1 = health.Length > 0 ? health : "Hit points " + type.BaseHitPoints.ToString("0");
            string line2 = "";

            if (type.IsRanged)
            {
                line1 += "    Damage " + type.Damage.ToString("0");
                line2 = "Hit chance " + (type.HitChance * 100).ToString("0") + "%    Shots per day "
                        + type.CampaignRateOfFirePerDay.ToString("0");
                if (type.IsAntiPersonnel)
                    line2 += "    Anti-personnel";
            }

            string line3 = "Build time " + SiegeActions.DaysText(SiegeActions.DaysToBuild(siege, type, 0f))
                           + " (" + type.ManDayCost.ToString("0") + " man-days)";
            if (engine != null)
            {
                line3 = engine.Progress >= 1f
                    ? "Finished"
                    : "Ready in " + SiegeActions.DaysText(SiegeActions.DaysToBuild(siege, type, engine.Progress));
            }

            return new SiegeEngineCardVM(EngineSprites.For(type.StringId), type.Name.ToString(), line1, line2, line3,
                type.Description.ToString(), actionText, action);
        }

        /// <summary>Compact one-line stats for the menu text.</summary>
        public static string OneLine(SiegeEvent siege, SiegeEngineType type)
        {
            string text = "HP " + type.BaseHitPoints.ToString("0");
            if (type.IsRanged)
                text += ", damage " + type.Damage.ToString("0");
            return text + ", " + SiegeActions.DaysText(SiegeActions.DaysToBuild(siege, type, 0f));
        }

        public static void Show(string title, List<SiegeEngineCardVM> cards)
        {
            Hide();

            if (cards.Count == 0 || !Options.ShowEnginePanel || !(ScreenManager.TopScreen is MapScreen screen))
                return;

            var vm = new SiegeInfoVM(title, cards);

            _screen = screen;
            _layer = new GauntletLayer("GauntletLayer", 1001);
            var movie = _layer.LoadMovie("SiegeInfoPanel", vm);

            // A panel with clickable cards or scroll buttons has to see the mouse; a plain one takes no input.
            _restricted = cards.Any(card => card.HasAction) || cards.Count > SiegeInfoVM.PageSize;
            if (_restricted)
                _layer.InputRestrictions.SetInputRestrictions(true, ClickMask);

            screen.AddLayer(_layer);
            _mismatchTicks = 0;

            var layer = _layer;
            _release = () =>
            {
                layer.ReleaseMovie(movie);
                vm.OnFinalize();
            };
        }

        public static void Hide()
        {
            if (_layer == null)
                return;

            try
            {
                _release?.Invoke();
                if (_restricted)
                    _layer.InputRestrictions.ResetInputRestrictions();
                _screen.RemoveLayer(_layer);
            }
            catch (Exception)
            {
                // The screen may already be gone (e.g. back to the main menu); nothing to clean up.
            }

            Clear();
        }

        private static void Clear()
        {
            _layer = null;
            _screen = null;
            _release = null;
            _restricted = false;
        }

        // Only the mouse buttons are needed; falls back to "all input" if the name is not found.
        private static InputUsageMask ClickMask =>
            Enum.TryParse("MouseButtons", out InputUsageMask mask) ? mask : InputUsageMask.All;

        /// <summary>Asks for an action to run on the next frame (used by clicks on the panel).</summary>
        public static void Queue(Action action)
        {
            _queued = action;
        }

        /// <summary>Called every frame: runs a queued click action, and closes the panel once the slot menu is gone.</summary>
        public static void Tick()
        {
            if (_queued != null)
            {
                Action action = _queued;
                _queued = null;

                try
                {
                    action();
                }
                catch (Exception e)
                {
                    ModLog.Error("Panel click", e);
                    InformationManager.DisplayMessage(new InformationMessage("Siege Command Menu: " + e.Message));
                }
            }

            if (_layer == null)
                return;

            if (ScreenManager.TopScreen != _screen)
            {
                // The map screen was replaced (battle, main menu); the layer went with it.
                Clear();
                return;
            }

            if (Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId == SlotMenuId)
            {
                _mismatchTicks = 0;
                return;
            }

            // A short grace period covers the moment the menu is still switching.
            if (++_mismatchTicks > GraceTicks)
                Hide();
        }
    }
}

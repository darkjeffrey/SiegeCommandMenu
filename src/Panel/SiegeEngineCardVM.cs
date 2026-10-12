using System;
using TaleWorlds.Library;

namespace SiegeCommandMenu
{
    /// <summary>One engine in the panel: picture plus stat lines, optionally clickable.</summary>
    public class SiegeEngineCardVM : ViewModel
    {
        private readonly Action _action;

        public SiegeEngineCardVM(string spritePath, string name, string line1, string line2, string line3,
            string description, string actionText, Action action)
        {
            SpritePath = spritePath;
            Name = name;
            Line1 = line1;
            Line2 = line2;
            Line3 = line3;
            Description = description;
            ActionText = actionText ?? "";
            _action = action;
        }

        /// <summary>Name of the game sprite drawn as this engine's picture (empty = no picture).</summary>
        [DataSourceProperty]
        public string SpritePath { get; }

        [DataSourceProperty]
        public string Name { get; }

        [DataSourceProperty]
        public string Line1 { get; }

        [DataSourceProperty]
        public string Line2 { get; }

        [DataSourceProperty]
        public string Line3 { get; }

        [DataSourceProperty]
        public string Description { get; }

        /// <summary>What clicking the card does ("Click to build"); empty when the card is display-only.</summary>
        [DataSourceProperty]
        public string ActionText { get; }

        [DataSourceProperty]
        public bool HasAction => _action != null;

        /// <summary>Runs on the next frame, not inside the click, so the panel can safely close itself.</summary>
        public void ExecuteAction()
        {
            if (_action != null)
                SiegeInfoOverlay.Queue(_action);
        }
    }
}

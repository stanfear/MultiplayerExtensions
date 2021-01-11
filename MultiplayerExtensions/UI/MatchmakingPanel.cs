using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace MultiplayerExtensions.UI
{
    class MatchmakingPanel : BSMLResourceViewController
    {
        JoinQuickPlayViewController? joinQuickPlayView;
        BeatmapDifficultyDropdown? difficultyDropdown;
        SimpleTextDropdown? diffTextDropdown;


        public override string ResourceName => "MultiplayerExtensions.UI.MatchmakingPanel.bsml";
        internal void Activate()
        {
            base.DidActivate(true, false, true);

            joinQuickPlayView = transform.GetComponent<JoinQuickPlayViewController>();
            difficultyDropdown = joinQuickPlayView.GetField<BeatmapDifficultyDropdown, JoinQuickPlayViewController>("_beatmapDifficultyDropdown");
            diffTextDropdown = difficultyDropdown.GetField<SimpleTextDropdown, BeatmapDifficultyDropdown>("_simpleTextDropdown");
            difficultyDropdown.didSelectCellWithIdxEvent += idx =>
            {
                if (difficultyDropdown.includeAllDifficulties && difficultyDropdown.GetSelectedBeatmapDifficultyMask() != BeatmapDifficultyMask.All)
                    SetAllDifficulties(false);
            };

            Transform wrapper = transform.Find("Wrapper");
            wrapper.GetComponent<VerticalLayoutGroup>().enabled = true;
            Transform background = transform.Find("BSMLBackground");

            for (int i = 0; 0 < background.childCount; i++)
            {
                Transform child = background.GetChild(0);
                child.SetParent(wrapper, false);
                child.SetSiblingIndex(i + 1);
            }
        }

        [UIComponent("CustomMatchmakeToggle")]
        public ToggleSetting customMatchmakeToggle = null!;

        [UIComponent("AllDifficultiesToggle")]
        public ToggleSetting allDifficultiesToggle = null!;

        [UIValue("CustomMatchmake")]
        public bool CustomMatchmake
        {
            get => Plugin.Config.CustomMatchmake;
            set
            {
                Plugin.Config.CustomMatchmake = value;
            }
        }

        [UIValue("AllDifficulties")]
        public bool AllDifficulties
        {
            get => Plugin.Config.AllDifficulties;
            set
            {
                Plugin.Config.AllDifficulties = value;
            }
        }

        [UIAction("SetCustomMatchmake")]
        public void SetCustomMatchmake(bool value)
        {
            CustomMatchmake = value;
            customMatchmakeToggle.Value = value;


        }

        [UIAction("SetAllDifficulties")]
        public void SetAllDifficulties(bool value)
        {
            AllDifficulties = value;
            allDifficultiesToggle.Value = value;

            if (difficultyDropdown != null)
            {
                difficultyDropdown.SetField<BeatmapDifficultyDropdown, IReadOnlyList<Tuple<BeatmapDifficultyMask, string>>>("_beatmapDifficultyData", null!);
                difficultyDropdown.includeAllDifficulties = value;
                difficultyDropdown.GetProperty<IReadOnlyList<Tuple<BeatmapDifficultyMask, string>>, BeatmapDifficultyDropdown>("beatmapDifficultyData");
                difficultyDropdown.Start();
                if (value)
                    diffTextDropdown?.SelectCellWithIdx(0);
            }
        }
    }
}

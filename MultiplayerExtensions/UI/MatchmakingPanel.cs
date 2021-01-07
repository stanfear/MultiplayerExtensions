using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace MultiplayerExtensions.UI
{
    class MatchmakingPanel : BSMLResourceViewController
    {
        public override string ResourceName => "MultiplayerExtensions.UI.MatchmakingPanel.bsml";
        internal void Activate()
        {
            base.DidActivate(true, false, true);

            Transform wrapper = transform.Find("Wrapper");
            Transform toggle = transform.Find("BSMLToggle");
            toggle.SetParent(wrapper, false);
            toggle.localPosition -= new Vector3(0f, 7.2f, 0f);
        }

        [UIComponent("CustomMatchmakeToggle")]
        public ToggleSetting customMatchmakeToggle = null!;

        [UIValue("CustomMatchmake")]
        public bool CustomMatchmake
        {
            get => Plugin.Config.CustomMatchmake;
            set
            {
                Plugin.Config.CustomMatchmake = value;
            }
        }

        [UIAction("SetCustomMatchmake")]
        public void SetCustomMatchmake(bool value)
        {
            CustomMatchmake = value;
            customMatchmakeToggle.Value = value;
        }
    }
}

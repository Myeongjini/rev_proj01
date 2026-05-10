using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WizardGrower.Core
{
    public interface IGameStartupPopup
    {
        bool ShouldShow();
        Task ShowAsync();
    }

    public class GameStartupPopupQueue : MonoBehaviour
    {
        private readonly List<IGameStartupPopup> popups = new List<IGameStartupPopup>();

        public void Register(IGameStartupPopup popup)
        {
            if (popup != null && !popups.Contains(popup))
                popups.Add(popup);
        }

        public async Task RunAsync()
        {
            for (int i = 0; i < popups.Count; i++)
            {
                IGameStartupPopup popup = popups[i];
                if (popup != null && popup.ShouldShow())
                    await popup.ShowAsync();
            }
        }
    }
}

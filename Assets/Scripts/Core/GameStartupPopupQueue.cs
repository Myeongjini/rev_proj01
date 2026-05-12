using System.Collections.Generic;
using System;
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
        [SerializeField] private int popupTimeoutMilliseconds = 15000;

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
                {
                    try
                    {
                        Task popupTask = popup.ShowAsync();
                        Task timeoutTask = Task.Delay(Mathf.Max(1000, popupTimeoutMilliseconds));
                        Task winner = await Task.WhenAny(popupTask, timeoutTask);
                        if (winner == timeoutTask)
                        {
                            Debug.LogWarning($"Startup popup timed out: {popup.GetType().Name}");
                            continue;
                        }

                        await popupTask;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Startup popup failed: {popup.GetType().Name} / {ex.GetBaseException().Message}");
                    }
                }
            }
        }
    }
}

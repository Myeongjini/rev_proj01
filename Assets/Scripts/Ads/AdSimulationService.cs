using System.Threading.Tasks;
using UnityEngine;

namespace WizardGrower.Ads
{
    public interface IRewardedAdProvider
    {
        Task<bool> WatchRewardedAdAsync();
    }

    public class AdSimulationService : MonoBehaviour, IRewardedAdProvider
    {
        public async Task<bool> WatchRewardedAdAsync()
        {
            await Task.Delay(1000);
            Debug.Log("[AdSim] Rewarded ad watched (simulated)");
            return true;
        }
    }
}

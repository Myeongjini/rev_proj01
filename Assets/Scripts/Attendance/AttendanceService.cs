using System;
using UnityEngine;
using WizardGrower.Economy;
using WizardGrower.Missions;

namespace WizardGrower.Attendance
{
    public enum AttendanceCellState
    {
        Claimed,
        Claimable,
        Locked
    }

    public class AttendanceService
    {
        private AttendanceConfig config;
        private CurrencyWallet wallet;
        private MissionResetService resetService;
        private AttendanceState state = new AttendanceState();

        public event Action StateChanged;
        public event Action<int, AttendanceDayReward> Claimed;

        public AttendanceState State => state;
        public AttendanceConfig Config => config;

        public void Initialize(AttendanceConfig config, CurrencyWallet wallet, MissionResetService resetService)
        {
            this.config = config != null ? config : AttendanceConfig.CreateDefault();
            this.config.EnsureDefaultRewards();
            this.wallet = wallet;
            this.resetService = resetService;
            NormalizeState();
        }

        public void Load(AttendanceState saved)
        {
            state = saved != null ? saved.Clone() : new AttendanceState();
            NormalizeState();
            StateChanged?.Invoke();
        }

        public AttendanceState Capture()
        {
            NormalizeState();
            return state.Clone();
        }

        public bool CanClaimToday()
        {
            NormalizeState();
            return !HasClaimedToday();
        }

        public AttendanceDayReward GetTodayReward()
        {
            NormalizeState();
            return config.GetReward(state.currentDayIndex);
        }

        public AttendanceDayReward GetRewardForDay(int dayIndex)
        {
            return config.GetReward(dayIndex);
        }

        public AttendanceCellState GetCellState(int dayIndex)
        {
            NormalizeState();
            int day = Mathf.Clamp(dayIndex, 1, 10);
            if (day < state.currentDayIndex)
                return AttendanceCellState.Claimed;
            if (day > state.currentDayIndex)
                return AttendanceCellState.Locked;
            if (state.currentDayIndex == 10 && HasCompletedCycle() && HasClaimedToday())
                return AttendanceCellState.Claimed;
            return CanClaimToday() ? AttendanceCellState.Claimable : AttendanceCellState.Locked;
        }

        public bool TryClaimToday()
        {
            NormalizeState();
            if (!CanClaimToday())
                return false;

            int claimedDay = state.currentDayIndex;
            AttendanceDayReward reward = config.GetReward(claimedDay);
            Grant(reward);
            state.lastClaimedUtcMs = NowUtcMs();
            state.totalCheckIns += 1;
            if (claimedDay < 10)
                state.currentDayIndex = claimedDay + 1;
            else
                state.currentDayIndex = 10;

            Claimed?.Invoke(claimedDay, reward);
            StateChanged?.Invoke();
            return true;
        }

        public void ForceLastClaimedUtcMs(long utcMs)
        {
            state.lastClaimedUtcMs = utcMs;
            NormalizeState();
            StateChanged?.Invoke();
        }

        private void NormalizeState()
        {
            if (state == null)
                state = new AttendanceState();
            state.currentDayIndex = Mathf.Clamp(state.currentDayIndex <= 0 ? 1 : state.currentDayIndex, 1, 10);
            state.totalCheckIns = Mathf.Max(0, state.totalCheckIns);
            if (config == null)
                config = AttendanceConfig.CreateDefault();
            config.EnsureDefaultRewards();

            if (state.currentDayIndex == 10 && HasCompletedCycle() && state.lastClaimedUtcMs > 0 && IsLaterServerDay(state.lastClaimedUtcMs, NowUtcMs()))
            {
                state.currentDayIndex = 1;
                state.lastClaimedUtcMs = 0;
            }
        }

        private bool HasCompletedCycle()
        {
            return state.totalCheckIns > 0 && state.totalCheckIns % 10 == 0;
        }

        private bool HasClaimedToday()
        {
            return state.lastClaimedUtcMs > 0 && !IsLaterServerDay(state.lastClaimedUtcMs, NowUtcMs());
        }

        private bool IsLaterServerDay(long previousUtcMs, long currentUtcMs)
        {
            return resetService != null
                ? resetService.IsLaterKstDay(previousUtcMs, currentUtcMs)
                : DateTimeOffset.FromUnixTimeMilliseconds(currentUtcMs).UtcDateTime.Date > DateTimeOffset.FromUnixTimeMilliseconds(previousUtcMs).UtcDateTime.Date;
        }

        private long NowUtcMs()
        {
            return resetService != null ? resetService.CurrentServerUtcMs : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private void Grant(AttendanceDayReward reward)
        {
            if (reward.kind == RewardKind.Gem && wallet != null)
                wallet.AddGems(reward.amount);
        }
    }
}

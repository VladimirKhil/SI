using SIPackages.Core;

namespace SICore
{
    public sealed class StakeInfo : BagCatInfo
    {
        private int _stake = 0;

        public int Stake
        {
            get => _stake;
            set { _stake = value; OnPropertyChanged(); }
        }
    }
}

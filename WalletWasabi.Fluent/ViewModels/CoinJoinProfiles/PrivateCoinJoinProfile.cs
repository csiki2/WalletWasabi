using Avalonia.Media;
using WalletWasabi.Fluent.Helpers;

namespace WalletWasabi.Fluent.ViewModels.CoinJoinProfiles;

internal class PrivateCoinJoinProfile : CoinJoinProfileViewModelBase
{
	public override string Title => "Private";

	public override string Description => "very Private";

	public override IImage Icon => AssetHelpers.GetBitmapAsset($"avares://WalletWasabi.Fluent/Assets/WalletIcons/{ThemeHelper.CurrentTheme}/coldcard.png");
	public override int MinAnonScoreTarget => 50;
	public override int MaxAnonScoreTarget => 100;

	public override int FeeRateAverageTimeFrameHours => 0;
}

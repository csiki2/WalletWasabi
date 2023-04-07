using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.ViewModels.AddWallet;
using WalletWasabi.Fluent.ViewModels.Login.PasswordFinder;
using WalletWasabi.Fluent.ViewModels.Navigation;
using WalletWasabi.Fluent.ViewModels.Wallets;
using WalletWasabi.Userfacing;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.Login;

[NavigationMetaData(Title = "")]
public partial class LoginViewModel : RoutableViewModel
{
	[AutoNotify] private string _password;
	[AutoNotify] private bool _isPasswordNeeded;
	[AutoNotify] private string _errorMessage;
	[AutoNotify] private bool _isForgotPasswordVisible;

	public LoginViewModel(NavBarWalletStateViewModel nbwsvm)
	{
		var wallet = nbwsvm.Wallet;
		IsPasswordNeeded = !wallet.KeyManager.IsWatchOnly;
		WalletName = wallet.WalletName;
		_password = "";
		_errorMessage = "";
		WalletType = WalletHelpers.GetType(nbwsvm.Wallet.KeyManager);

		NextCommand = ReactiveCommand.CreateFromTask(async () => await OnNextAsync(nbwsvm, wallet));

		OkCommand = ReactiveCommand.Create(OnOk);

		ForgotPasswordCommand = ReactiveCommand.Create(() => OnForgotPassword(wallet));

		EnableAutoBusyOn(NextCommand);
	}

	public WalletType WalletType { get; }

	public string WalletName { get; }

	public ICommand OkCommand { get; }

	public ICommand ForgotPasswordCommand { get; }

	private async Task OnNextAsync(NavBarWalletStateViewModel nbwsvm, Wallet wallet)
	{
		string? compatibilityPasswordUsed = null;

		var isPasswordCorrect = await Task.Run(() => wallet.TryLogin(Password, out compatibilityPasswordUsed));

		if (!isPasswordCorrect)
		{
			IsForgotPasswordVisible = true;
			ErrorMessage = "The password is incorrect! Please try again.";
			return;
		}

		if (compatibilityPasswordUsed is { })
		{
			await ShowErrorAsync(Title, PasswordHelper.CompatibilityPasswordWarnMessage, "Compatibility password was used");
		}

		var legalResult = await ShowLegalAsync();

		if (legalResult)
		{
			LoginWallet(nbwsvm);
		}
		else
		{
			wallet.Logout();
			ErrorMessage = "You must accept the Terms and Conditions!";
		}
	}

	private void OnOk()
	{
		Password = "";
		ErrorMessage = "";
	}

	private void OnForgotPassword(Wallet wallet)
	{
		Navigate(NavigationTarget.DialogScreen).To(new PasswordFinderIntroduceViewModel(wallet));
	}

	private void LoginWallet(NavBarWalletStateViewModel nbwsvm)
	{
		//closedWalletViewModel.RaisePropertyChanged(nameof(WalletViewModelBase.IsLoggedIn));
		//closedWalletViewModel.StartLoading();

		nbwsvm.IsLoggedIn = true;

		nbwsvm.CurrentPage = new LoadingViewModel(nbwsvm);

		// if (closedWalletViewModel.IsSelected && closedWalletViewModel.OpenCommand.CanExecute(default))
		// {
		// 	closedWalletViewModel.OpenCommand.Execute(true);
		// }
	}

	private async Task<bool> ShowLegalAsync()
	{
		if (!Services.LegalChecker.TryGetNewLegalDocs(out _))
		{
			return true;
		}

		var legalDocs = new TermsAndConditionsViewModel();

		var dialogResult = await NavigateDialogAsync(legalDocs, NavigationTarget.DialogScreen);

		if (dialogResult.Result)
		{
			await Services.LegalChecker.AgreeAsync();
		}

		return dialogResult.Result;
	}
}

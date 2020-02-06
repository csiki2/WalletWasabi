using NBitcoin;
using System;
using System.IO;
using System.Linq;
using WalletWasabi.Helpers;
using WalletWasabi.Models;

namespace WalletWasabi.Blockchain.Keys
{
	public class WalletGenerator
	{
		private static readonly string[] ReservedFileNames = new string[]
		{
			"CON", "PRN", "AUX", "NUL",
			"COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
			"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
		};

		public WalletGenerator(string walletsDir, Network network)
		{
			WalletsDir = walletsDir;
			Network = network;
		}

		public string WalletsDir { get; private set; }
		public Network Network { get; private set; }
		public bool TermsAccepted { get; set; }
		public uint TipHeight { get; set; }

		public (KeyManager, Mnemonic) GenerateWallet(string walletName, string password)
		{
			if (!ValidateWalletName(walletName))
			{
				throw new ArgumentException("Invalid wallet name.");
			}

			if (!TermsAccepted)
			{
				throw new InvalidOperationException("Terms are not accepted.");
			}

			string walletFilePath = Path.Combine(WalletsDir, $"{walletName}.json");
			if (File.Exists(walletFilePath))
			{
				throw new ArgumentException("Wallet name is already taken.");
			}

			PasswordHelper.Guard(password); // Here we are not letting anything that will be autocorrected later. We need to generate the wallet exactly with the entered password bacause of compatibility.

			var km = KeyManager.CreateNew(out Mnemonic mnemonic, password);
			km.SetNetwork(Network);
			km.SetBestHeight(new Height(TipHeight));
			km.SetFilePath(walletFilePath);
			return (km, mnemonic);
		}

		private bool ValidateWalletName(string walletName)
		{
			if (string.IsNullOrWhiteSpace(walletName))
			{
				return false;
			}
			var invalidChars = Path.GetInvalidFileNameChars();
			var isValid = !walletName.Any(c => invalidChars.Contains(c)) && !walletName.EndsWith(".");
			var isReserved = ReservedFileNames.Any(w => walletName.ToUpper() == w || walletName.ToUpper().StartsWith(w + "."));
			return isValid && !isReserved;
		}
	}
}

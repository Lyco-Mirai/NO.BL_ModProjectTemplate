using System;
using System.Security.Cryptography;
using System.Text;

namespace NuclearOption.Networking.Lobbies
{
	public class LobbyPassword
	{
		public class PasswordChallenge
		{
			private readonly byte[] combinedBytes;

			private readonly byte[] nonce;

			private bool hasChallenged;

			public PasswordChallenge(byte[] combinedBytes)
			{
				this.combinedBytes = combinedBytes;
				nonce = GenerateNonceBytes();
			}

			public ArraySegment<byte> GetNonceBytes()
			{
				return nonce;
			}

			public bool VerifyResponse(ArraySegment<byte> clientResponse)
			{
				if (hasChallenged)
				{
					return false;
				}
				hasChallenged = true;
				byte[] array = HashPasswordNonce(combinedBytes, nonce);
				int offset = clientResponse.Offset;
				int count = clientResponse.Count;
				byte[] array2 = clientResponse.Array;
				if (count != array.Length)
				{
					return false;
				}
				for (int i = 0; i < count; i++)
				{
					if (array2[offset + i] != array[i])
					{
						return false;
					}
				}
				return true;
			}

			private static byte[] GenerateNonceBytes()
			{
				byte[] array = new byte[32];
				using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
				randomNumberGenerator.GetBytes(array);
				return array;
			}
		}

		private const int NONCE_LENGTH = 32;

		private byte[] combinedBytes;

		public LobbyPassword(string lobbyPassword)
		{
			combinedBytes = GenerateCombineByteArray(lobbyPassword);
		}

		public PasswordChallenge GenerateChallenge()
		{
			return new PasswordChallenge(combinedBytes);
		}

		public static bool TestShortPassword(LobbyInstance lobby, string password)
		{
			if (!lobby.IsPasswordProtected(out var shortPassword))
			{
				return true;
			}
			return shortPassword == GetShortPassword(password);
		}

		public static string GetShortPassword(string password)
		{
			using SHA256 sHA = SHA256.Create();
			byte[] bytes = Encoding.UTF8.GetBytes(password);
			int num = sHA.ComputeHash(bytes)[0] & 0x3F;
			return $"{num:x1}";
		}

		private static byte[] GenerateCombineByteArray(string password)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(password);
			byte[] array = new byte[bytes.Length + 32];
			Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
			return array;
		}

		public static byte[] GenerateResponse(string password, ArraySegment<byte> nonceBytes)
		{
			return HashPasswordNonce(GenerateCombineByteArray(password), nonceBytes);
		}

		private static byte[] HashPasswordNonce(byte[] combinedBytes, ArraySegment<byte> nonceBytes)
		{
			using SHA256 sHA = SHA256.Create();
			Buffer.BlockCopy(nonceBytes.Array, nonceBytes.Offset, combinedBytes, combinedBytes.Length - 32, 32);
			return sHA.ComputeHash(combinedBytes);
		}
	}
}

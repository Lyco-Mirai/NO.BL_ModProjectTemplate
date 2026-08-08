using System;
using NuclearOption.Networking.Lobbies;
using UnityEngine;

namespace NuclearOption.BuildScripts.DebugTests
{
	public class LobbyPasswordTest
	{
		public static void Run()
		{
			LobbyPassword lobbyPassword = new LobbyPassword("nuclear option");
			LobbyPassword.PasswordChallenge passwordChallenge = lobbyPassword.GenerateChallenge();
			LobbyPassword.PasswordChallenge passwordChallenge2 = lobbyPassword.GenerateChallenge();
			byte[] array = LobbyPassword.GenerateResponse("nuclear option", passwordChallenge.GetNonceBytes());
			byte[] array2 = LobbyPassword.GenerateResponse("nuclear option", passwordChallenge2.GetNonceBytes());
			bool flag = passwordChallenge2.VerifyResponse(array2);
			bool flag2 = passwordChallenge.VerifyResponse(array);
			Debug.Log($"[Case 1] Interleaved - Player B: {flag}, Player A: {flag2}");
			if (!flag2)
			{
				Debug.LogError("BUG DETECTED: Player A failed because Player B overwritten the shared buffer!");
			}
			LobbyPassword.PasswordChallenge passwordChallenge3 = lobbyPassword.GenerateChallenge();
			byte[] array3 = LobbyPassword.GenerateResponse("nuclear option", passwordChallenge3.GetNonceBytes());
			passwordChallenge3.VerifyResponse(array3);
			bool flag3 = passwordChallenge3.VerifyResponse(array3);
			Debug.Log($"[Case 2] Re-entry - Second attempt success: {flag3} (Expected: False)");
			LobbyPassword.PasswordChallenge passwordChallenge4 = new LobbyPassword("very_long_password_here_12345").GenerateChallenge();
			byte[] array4 = LobbyPassword.GenerateResponse("very_long_password_here_12345", passwordChallenge4.GetNonceBytes());
			Debug.Log($"[Case 3] Length check - Success: {passwordChallenge4.VerifyResponse(array4)}");
			LobbyPassword.PasswordChallenge passwordChallenge5 = lobbyPassword.GenerateChallenge();
			byte[] array5 = LobbyPassword.GenerateResponse("nuclear option", passwordChallenge5.GetNonceBytes());
			byte[] array6 = new byte[1024];
			int num = 50;
			Buffer.BlockCopy(array5, 0, array6, num, array5.Length);
			ArraySegment<byte> clientResponse = new ArraySegment<byte>(array6, num, array5.Length);
			bool flag4 = passwordChallenge5.VerifyResponse(clientResponse);
			Debug.Log($"[Case 4] ArraySegment respect - Success: {flag4}");
		}
	}
}

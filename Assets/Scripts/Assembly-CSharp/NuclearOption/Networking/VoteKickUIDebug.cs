using Steamworks;
using UnityEngine;

namespace NuclearOption.Networking
{
	public class VoteKickUIDebug : MonoBehaviour
	{
		private bool _active = true;

		private bool _passed;

		private string _targetName = "PlayerToKick";

		private int _yesVotes = 1;

		private int _noVotes;

		private int _votesRequired = 3;

		private int _eligibleVoters = 5;

		private float _remainingTime = 30f;

		[SerializeField]
		private VoteKickUI voteKickUI;

		private string _manualSteamID = "76561197960287930";

		private bool _showDebugGUI = true;

		private Vector2 _scrollPosition;

		private void OnValidate()
		{
		}

		private void Update()
		{
			if (Application.isPlaying && Input.GetKeyDown(KeyCode.F9))
			{
				_showDebugGUI = !_showDebugGUI;
			}
		}

		private void OnGUI()
		{
			if (!_showDebugGUI)
			{
				return;
			}
			float num = 300f;
			float height = 520f;
			GUILayout.BeginArea(new Rect((float)Screen.width - num - 10f, 10f, num, height), "Vote Kick UI Debugger (F9 to toggle)", GUI.skin.window);
			_scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
			GUILayout.Label("Simulated VoteKickState Fields:");
			_active = GUILayout.Toggle(_active, " Active");
			_passed = GUILayout.Toggle(_passed, " Passed");
			GUILayout.BeginHorizontal();
			GUILayout.Label("Target:", GUILayout.Width(80f));
			_targetName = GUILayout.TextField(_targetName);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("SteamID:", GUILayout.Width(80f));
			_manualSteamID = GUILayout.TextField(_manualSteamID);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label($"Yes Votes: {_yesVotes}", GUILayout.Width(110f));
			if (GUILayout.Button("-", GUILayout.Width(30f)))
			{
				_yesVotes = Mathf.Max(0, _yesVotes - 1);
			}
			if (GUILayout.Button("+", GUILayout.Width(30f)))
			{
				_yesVotes++;
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label($"No Votes: {_noVotes}", GUILayout.Width(110f));
			if (GUILayout.Button("-", GUILayout.Width(30f)))
			{
				_noVotes = Mathf.Max(0, _noVotes - 1);
			}
			if (GUILayout.Button("+", GUILayout.Width(30f)))
			{
				_noVotes++;
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label($"Votes Required: {_votesRequired}", GUILayout.Width(130f));
			if (GUILayout.Button("-", GUILayout.Width(30f)))
			{
				_votesRequired = Mathf.Max(1, _votesRequired - 1);
			}
			if (GUILayout.Button("+", GUILayout.Width(30f)))
			{
				_votesRequired++;
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label($"Eligible Voters: {_eligibleVoters}", GUILayout.Width(130f));
			if (GUILayout.Button("-", GUILayout.Width(30f)))
			{
				_eligibleVoters = Mathf.Max(1, _eligibleVoters - 1);
			}
			if (GUILayout.Button("+", GUILayout.Width(30f)))
			{
				_eligibleVoters++;
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label($"Remaining Time: {_remainingTime:F0}s");
			_remainingTime = GUILayout.HorizontalSlider(_remainingTime, 0f, 60f);
			GUILayout.EndHorizontal();
			GUILayout.Space(10f);
			if (GUILayout.Button("Apply State to UI"))
			{
				ApplyState();
			}
			GUILayout.Space(10f);
			GUILayout.Label("Networked Kick Trigger Options:");
			if (Application.isPlaying && NetworkSceneSingleton<VoteKickManager>.i != null)
			{
				if (UnitRegistry.playerLookup != null)
				{
					foreach (Player value in UnitRegistry.playerLookup.Values)
					{
						if (!(value == null))
						{
							GUILayout.BeginHorizontal();
							GUILayout.Label(value.GetDisplayName(PlayerNameContext.ChatOrLeaderboard), GUILayout.Width(120f));
							if (GUILayout.Button("Request Kick", GUILayout.Width(100f)))
							{
								SceneSingleton<VoteKickUI>.i.RequestVoteKickLocal(value);
							}
							GUILayout.EndHorizontal();
						}
					}
				}
				else
				{
					GUILayout.Label("UnitRegistry.playerLookup is null");
				}
			}
			else
			{
				GUILayout.Label("VoteKickManager.i is null (Offline / Editor)");
			}
			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		private void ApplyState()
		{
			if (voteKickUI == null)
			{
				Debug.LogWarning("voteKickUI is null. Ensure it is referenced in the inspector.");
				return;
			}
			double voteEndTimeNetwork = Time.time + _remainingTime;
			if (Application.isPlaying && NetworkSceneSingleton<VoteKickManager>.i != null && NetworkSceneSingleton<VoteKickManager>.i.NetworkTime != null)
			{
				voteEndTimeNetwork = NetworkSceneSingleton<VoteKickManager>.i.NetworkTime.Time + (double)_remainingTime;
			}
			if (!ulong.TryParse(_manualSteamID, out var result))
			{
				result = 123456789012345uL;
			}
			VoteKickState state = new VoteKickState
			{
				Active = _active,
				Passed = _passed,
				TargetID = new CSteamID(result),
				YesVotes = _yesVotes,
				NoVotes = _noVotes,
				VotesRequired = _votesRequired,
				EligibleVoters = _eligibleVoters,
				VoteEndTimeNetwork = voteEndTimeNetwork,
				LockoutEndTimeNetwork = 0.0
			};
			voteKickUI.SetState(state);
		}
	}
}

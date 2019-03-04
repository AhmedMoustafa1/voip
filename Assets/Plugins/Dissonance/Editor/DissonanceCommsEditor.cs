#if !NCRUNCH

using Dissonance.Audio.Playback;
using Dissonance.Networking;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof (DissonanceComms))]
    public class DissonanceCommsEditor : UnityEditor.Editor
    {
        private Texture2D _logo;

        private readonly TokenControl _tokenEditor = new TokenControl("These access tokens are used by broadcast/receipt triggers to determine if they should function");

        private readonly HashSet<string> _showRoomMembership = new HashSet<string>();
        private readonly HashSet<string> _showSpeakingChannels = new HashSet<string>();
        private bool _showLocalRooms;

        private string _lastPrefabError;

        #region initialisation
        private void CreateSkin()
        {
        }

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }
        #endregion

        public override void OnInspectorGUI()
        {
            CreateSkin();

            var comm = (DissonanceComms) target;

            GUILayout.Label(_logo);

            CommsNetworkGui();
            DissonanceCommsGui();

            PlaybackPrefabGui(comm);

            comm.ChangeWithUndo(
                "Changed Dissonance Mute",
                EditorGUILayout.Toggle("Mute", comm.IsMuted),
                comm.IsMuted,
                a => comm.IsMuted = a
            );

            comm.ChangeWithUndo(
                "Changed Dissonance Deafen",
                EditorGUILayout.Toggle("Deafen", comm.IsDeafened),
                comm.IsDeafened,
                a => comm.IsDeafened = a
            );

            EditorGUILayout.Space();

            if (Application.isPlaying)
            {
                StatusGui(comm);

                EditorGUILayout.Space();
            }

            _tokenEditor.DrawInspectorGui(comm, comm);

            if (GUILayout.Button("Voice Settings"))
                VoiceSettingsEditor.GoToSettings();

            if (GUILayout.Button("Configure Rooms"))
                ChatRoomSettingsEditor.GoToSettings();
            
            if (GUILayout.Button("Diagnostic Settings"))
                DebugSettingsEditor.GoToSettings();

            Undo.FlushUndoRecordObjects();
            EditorUtility.SetDirty(comm);
        }

        private void PlaybackPrefabGui([NotNull] DissonanceComms comm)
        {
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                var prefab = EditorGUILayout.ObjectField("Playback Prefab", comm.PlaybackPrefab, typeof(GameObject), false);
                if (!Application.isPlaying)
                {
                    // Check if we're in the special case of setting to nothing, when it's already nothing
                    if (prefab == null && comm.PlaybackPrefab == null)
                    {
                        //Display the last error
                        if (!string.IsNullOrEmpty(_lastPrefabError))
                            EditorGUILayout.HelpBox(_lastPrefabError, MessageType.Error);

                        return;
                    }

                    GameObject newPrefab = null;
                    if (prefab == null)
                    {
                        //Setting to null, no error involved with this
                        _lastPrefabError = null;
                    }
                    else if (PrefabUtility.GetPrefabType(prefab) == PrefabType.Prefab)
                    {
                        //Check that the prefab is valid
                        newPrefab = (GameObject)prefab;
                        if (newPrefab.GetComponent<VoicePlayback>() == null)
                        {
                            newPrefab = null;
                            _lastPrefabError = "Playback Prefab must contain a VoicePlayback component";
                        }
                        else
                            _lastPrefabError = null;
                    }
                    else
                    {
                        _lastPrefabError = "Playback Prefab type must be user created prefab asset";
                    }

                    if (!string.IsNullOrEmpty(_lastPrefabError))
                        EditorGUILayout.HelpBox(_lastPrefabError, MessageType.Error);

                    comm.ChangeWithUndo(
                        "Changed Dissonance Playback Prefab",
                        ReferenceEquals(newPrefab, null) ? null : newPrefab.gameObject,
                        comm.PlaybackPrefab,
                        a => comm.PlaybackPrefab = a
                    );
                }
            }
        }

        private void CommsNetworkGui()
        {
            var nets = ((DissonanceComms)target).gameObject.GetComponents<ICommsNetwork>();
            if (nets == null || nets.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "Please attach a Comms Network component appropriate to your networking system to the entity.",
                    MessageType.Error
                );
            }
            else if (nets.Length > 1)
            {
                EditorGUILayout.HelpBox(
                    "Please remove all but one of the ICommsNetwork components attached to this entity.",
                    MessageType.Error
                );
            }
        }

        private void DissonanceCommsGui()
        {
            var nets = ((DissonanceComms)target).gameObject.GetComponents<DissonanceComms>();
            if (nets.Length > 1)
            {
                EditorGUILayout.HelpBox(
                    "Please remove all but one of the DissonanceComms components attached to this entity.",
                    MessageType.Error
                );
            }
            else
            {
                var comms = FindObjectsOfType<DissonanceComms>();
                if (comms.Length > 1)
                {
                    EditorGUILayout.HelpBox(
                        string.Format("Found {0} DissonanceComms components in scene, please remove all but one", comms.Length),
                        MessageType.Error
                    );
                }
            }
        }

        private void StatusGui([NotNull] DissonanceComms comm)
        {
            EditorGUILayout.LabelField("Local Player ID", comm.LocalPlayerName);
            _showLocalRooms = GUILayout.Toggle(_showLocalRooms, new GUIContent("Show Rooms (Listening To)"));

            if (_showLocalRooms)
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    foreach (var room in comm.Rooms.Memberships)
                        EditorGUILayout.LabelField(" - " + room);

            EditorGUILayout.LabelField("Estimated Packet Loss", (comm.PacketLoss).ToString("0%"));

            var count = comm.Players.Count - 1;
            EditorGUILayout.LabelField("Peers: (" + (count == 0 ? "none" : count.ToString()) + ")");

            for (var i = 0; i < comm.Players.Count; i++)
            {
                var p = comm.Players[i];

                //Skip the local player
                if (p.Name == comm.LocalPlayerName)
                    continue;

                PlayerGui(p);

                //If there is a player we'll set the comms object to dirty which causes the editor to be redrawn.
                //This makes the (speaking) indicator update live for players.
                EditorUtility.SetDirty(comm);
            }
        }

        private void PlayerGui([NotNull] VoicePlayerState p)
        {
            var message = string.Format("{0} {1} {2} {3}",
                p.Name,
                p.IsSpeaking ? "(speaking)" : "",
                !p.IsConnected ? "(disconnected)" : "",
                p.Tracker != null && p.Tracker.IsTracking ? "(positional)" : ""
            );

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                bool showListeningRooms;
                bool showSpeakingChannels;

                EditorGUILayout.LabelField(message);
                using (new EditorGUILayout.HorizontalScope())
                {
                    p.IsLocallyMuted = GUILayout.Toggle(p.IsLocallyMuted, new GUIContent("Mute", "Prevent this player from being heard locally"));

                    showListeningRooms = GUILayout.Toggle(_showRoomMembership.Contains(p.Name), new GUIContent("Show Rooms", "Show the set of rooms this player is listening to"));
                    if (showListeningRooms)
                        _showRoomMembership.Add(p.Name);
                    else
                        _showRoomMembership.Remove(p.Name);

                    showSpeakingChannels = GUILayout.Toggle(_showSpeakingChannels.Contains(p.Name), new GUIContent("Show Channels", "Show the set of channels this player is speaking to the local player through"));
                    if (showSpeakingChannels)
                        _showSpeakingChannels.Add(p.Name);
                    else
                        _showSpeakingChannels.Remove(p.Name);
                }

                if (showListeningRooms)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("Listening To:");
                        foreach (var room in p.Rooms)
                            EditorGUILayout.LabelField(" - " + room);
                    }
                }

                if (showSpeakingChannels)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        var l = new List<RemoteChannel>();
                        p.GetSpeakingChannels(l);

                        EditorGUILayout.LabelField("Speaking Through:");
                        foreach (var channel in l.OrderByDescending(a => a.Type))
                            EditorGUILayout.LabelField(string.Format(" - {0}: {1}", channel.Type, channel.TargetName));
                    }
                }
            }
        }
    }
}

#endif
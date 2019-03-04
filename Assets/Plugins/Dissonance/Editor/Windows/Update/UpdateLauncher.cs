using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Dissonance.Editor.Windows.Update
{
    [InitializeOnLoad]
    public class UpdateLauncher
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(UpdateLauncher).Name);

        private const string CheckForNewVersionKey = "placeholder_dissonance_update_checkforlatestversion";

        private static readonly string StateKey = "placeholder_dissonance_update_checkstate";

        private static uint _frameCount;
        private static DateTime? _cachedNextCheckTime;
        private static IEnumerator<bool> _updateCheckInProgress;
        #endregion

        public static void ForceUpdateCheck()
        {
            var state = GetUpdateState();

            //Set the next-check time stamp to now
            SetUpdateState(new UpdateState(state.ShownForVersion, DateTime.UtcNow));
            _frameCount = 0;
            _cachedNextCheckTime = DateTime.UtcNow;

            //Since an update has been forced then we can assume the updater is enabled
            SetUpdaterEnabled(true);
        }

        private static IEnumerable<bool> CheckUpdateVersion()
        {
            //Exit if the update check is not enabled
            if (!GetUpdaterEnabled())
            {
                Log.Trace("Updater not enabled");
                yield return false;
            }

            //Fill the cache if it's empty
            if (_cachedNextCheckTime == null)
            {
                Log.Trace("Caching next update time");
                _cachedNextCheckTime = GetUpdateState().NextCheck;
            }

            //Exit if the next update check isn't due yet
            if (_cachedNextCheckTime.Value >= DateTime.UtcNow)
            {
                Log.Trace("Updater not yet time to check");
                yield return false;
            }

            //Take a single frame break before starting the update check process
            yield return true;
            var state = GetUpdateState();
            Log.Trace("Beginning version check. Current state:'{0}' Current version:'{1}'", state, DissonanceComms.Version);

            //setup some helpers for later
            //On failure, check again after a short time
            //On success, check again after a long time
            var random = new System.Random();
            Action failed = () => SetUpdateState(new UpdateState(state.ShownForVersion, DateTime.UtcNow + TimeSpan.FromMinutes(random.Next(10, 70))));
            Action<SemanticVersion> success = v => SetUpdateState(new UpdateState(v, DateTime.UtcNow + TimeSpan.FromHours((random.Next(24, 144)))));

            //Begin downloading the manifest of all Dissonance updates
            using (var request = UnityWebRequest.Get(string.Format("https://placeholder-software.co.uk/dissonance/releases/latest-published.html{0}", EditorMetadata.GetQueryString("update_checker"))))
            {
                //Wait until request is complete
                var wait = request.SendWebRequest();
                while (!wait.isDone)
                    yield return true;

                //If it's an error give up and schedule the next check fairly soon
                if (request.isNetworkError)
                {
                    request.Dispose();
                    failed();
                    Log.Trace("Update request failed");
                    yield return false;
                }

                //Get the response bytes and discard the request
                var bytes = request.downloadHandler.data;
                request.Dispose();

                //Parse the response data. If we fail give up and schedule the next check fairly soon
                SemanticVersion latest;
                if (!TryParse(bytes, out latest) || latest == null)
                {
                    failed();
                    Log.Trace("Update response parsing failed");
                    yield return false;
                }
                else
                {
                    Log.Trace("Received updater response, remote latest version is: '{0}'", latest);

                    //Check if we've already shown the window for a greater version
                    if (latest.CompareTo(state.ShownForVersion) <= 0)
                    {
                        success(state.ShownForVersion);
                        Log.Trace("Update success, window already shown for higher version '{0}'", state.ShownForVersion);
                        yield return false;
                    }

                    //Check if the new version is greater than the currently installed version
                    if (latest.CompareTo(DissonanceComms.Version) <= 0)
                    {
                        success(state.ShownForVersion);
                        Log.Trace("Update success, newer version '{0}' already installed", DissonanceComms.Version);
                        yield return false;
                    }

                    //Update the state so that the window does not show up again for this version
                    success(latest);
                    UpdateWindow.Show(latest, DissonanceComms.Version);
                    Log.Trace("Update success, showing update notification window for version '{0}'", latest);
                }
            }
        }

        internal static void Startup()
        {
            Windows.Startup.SafeUpdate += Update;
        }

        private static void Update()
        {
            //If a coroutine is running, pump that.
            if (_updateCheckInProgress != null)
            {
                Log.Trace("Pumping update coroutine");

                try
                {
                    if (!_updateCheckInProgress.MoveNext() || !_updateCheckInProgress.Current)
                        _updateCheckInProgress = null;
                }
                catch (Exception e)
                {
                    Log.Error("Disabling update checker! Encountered unexpected error running update check:  '{0}'", e.Message);
                    SetUpdaterEnabled(false);
                }

                return;
            }

            //Exit right away if the update is not enabled
            if (!GetUpdaterEnabled())
                return;

            //Only check once every 10000 frames (_very_ roughly once every 100 seconds)
            //Offset first check by 250 so that it doesn't happen _immediately_
            unchecked { _frameCount++; }
            if (_frameCount % 10000 != 250)
                return;

            //Start a new update check if there isn't one in progress
            if (_updateCheckInProgress == null)
            {
                Log.Trace("Starting new update coroutine");
                _updateCheckInProgress = CheckUpdateVersion().GetEnumerator();
            }
        }

        /// <summary>
        /// Try to parse the response from the server
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="parsed"></param>
        /// <returns></returns>
        private static bool TryParse(byte[] bytes, [CanBeNull] out SemanticVersion parsed)
        {
            try
            {
                // The received data is a root level array. Wrap it in an object which gives the root array a name
                var str = Encoding.UTF8.GetString(bytes);
                parsed = JsonUtility.FromJson<SemanticVersion>(str);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                parsed = null;
                return false;
            }
        }

        #region state
        /// <summary>
        /// Get the state of the updater as stored on disk
        /// </summary>
        /// <returns></returns>
        private static UpdateState GetUpdateState()
        {
            if (!PlayerPrefs.HasKey(StateKey))
            {
                // State path does not exist at all so create the default
                Log.Trace("Attempted to read state but there is no state - Created default.");
                var state = new UpdateState(new SemanticVersion(), DateTime.UtcNow);
                SetUpdateState(state);
                return state;
            }
            else
            {
                //Read the state from the string
                var json = PlayerPrefs.GetString(StateKey);
                var state = JsonUtility.FromJson<UpdateState>(json);
                Log.Trace("Read state from disk: '{0}'", json);
                return state;
            }
        }

        /// <summary>
        /// Set the state of the updater as stored on disk
        /// </summary>
        /// <param name="state"></param>
        private static void SetUpdateState([CanBeNull] UpdateState state)
        {
            if (state == null)
            {
                //Clear installer state
                Log.Trace("Setting update state to 'null'");
                PlayerPrefs.DeleteKey(StateKey);
                _cachedNextCheckTime = null;
            }
            else
            {
                _cachedNextCheckTime = state.NextCheck;
                var json = JsonUtility.ToJson(state);
                Log.Trace("Setting update state to '{0}'", json);
                PlayerPrefs.SetString(StateKey, json);
            }
        }
        #endregion

        #region prefs
        internal static void SetUpdaterEnabled(bool enabled)
        {
            EditorPrefs.SetBool(CheckForNewVersionKey, enabled);
        }

        internal static bool GetUpdaterEnabled()
        {
            //If pref not set yet then assume the updater is enabled
            if (!EditorPrefs.HasKey(CheckForNewVersionKey))
                return true;

            return EditorPrefs.GetBool(CheckForNewVersionKey);
        }
        #endregion

        [Serializable] private class UpdateState
        {
            [SerializeField, UsedImplicitly] private SemanticVersion _shownForVersion;
            [SerializeField, UsedImplicitly] private long _nextCheckFileTime;

            public SemanticVersion ShownForVersion
            {
                get { return _shownForVersion; }
            }

            public DateTime NextCheck
            {
                get { return DateTime.FromFileTimeUtc(_nextCheckFileTime); }
            }

            public UpdateState(SemanticVersion version, DateTime nextCheck)
            {
                _shownForVersion = version;
                _nextCheckFileTime = nextCheck.ToFileTimeUtc();
            }

            public override string ToString()
            {
                return _shownForVersion.ToString();
            }
        }
    }
}
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine; 
using UnityEngine.SceneManagement;

namespace Oxide.Plugins
{
    [Info("Console Filter", "Wulf", "0.0.2")]
    [Description("Filters debug, test, and other undesired output in the server console")]
    class ConsoleFilter : CovalencePlugin
    {
        #region Configuration

        private Configuration config;

        class Configuration
        {
            // TODO: Add support for regex matching

            [JsonProperty("List of partial strings to filter", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Filter = new List<string>
            {
                "AngryAnt Behave version",
            };

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            LogWarning($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion Configuration

        #region Filtering

        private void Init()
        {
            UnityEngine.Application.logMessageReceived += HandleLog;
#if HURTWORLD
            UnityEngine.Application.logMessageReceived -= ConsoleManager.Instance.CaptureLog;
#elif RUST
            UnityEngine.Application.logMessageReceived -= Facepunch.Output.LogHandler;
#elif SEVENDAYSTODIE
            UnityEngine.Application.logMessageReceivedThreaded -= Logger.Main.UnityLogCallback;
#endif
        }

        private void Unload()
        {
#if HURTWORLD
            UnityEngine.Application.logMessageReceived += ConsoleManager.Instance.CaptureLog;
#elif RUST
            UnityEngine.Application.logMessageReceived += Facepunch.Output.LogHandler;
#elif SEVENDAYSTODIE
            UnityEngine.Application.logMessageReceivedThreaded += Logger.Main.UnityLogCallback;
#endif
            UnityEngine.Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string message, string stackTrace, UnityEngine.LogType type)
        {
            if (!string.IsNullOrEmpty(message))
            {
#if HURTWORLD
                ConsoleManager.Instance.CaptureLog(message, stackTrace, type);
#elif RUST
				if (message.Contains("failed to sample navmesh at position")){		
					List<Transform> list2 = new List<Transform>(Resources.FindObjectsOfTypeAll<Transform>());			
					string prefabName = message.Split(' ')[0];
					foreach (Transform transform in list2)
					{
						
						if(transform.name != prefabName){break;}
						BaseEntity baseEntity = transform.gameObject.ToBaseEntity();
						if (baseEntity.IsValid())
						{
							if (baseEntity.isServer) baseEntity.Kill();
						}
						else GameManager.Destroy(transform.gameObject);
					}
					
				}
                Facepunch.Output.LogHandler(message, stackTrace, type);
#elif SEVENDAYTODIE
                Logger.Main.SendToLogListeners(message, stackTrace, type);
#endif
            }
        }

        #endregion Filtering
    }
}

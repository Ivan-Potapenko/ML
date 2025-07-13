using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;

namespace HideAndSeek {

    public class Environment : MonoBehaviour {

        private enum RewardDisplayMode {
            Center,
            AboveAgents
        }

        [SerializeField]
        private RewardDisplayMode _displayMode = RewardDisplayMode.AboveAgents;

        [Serializable]
        private struct SettingsByTeam {
            public HideAndSeekAgent.TeamId teamId;
            public HideAndSeekAgent agentPrefab;
            public int agentsCount;
            public Transform[] spawnPoints;
            public float damageReward;
            public float killReward;
            public float deathReward;
            public float winReward;
            public float timeReward;
            public Material winMaterial;
        }

        [SerializeField]
        private MeshRenderer[] _objectsToIndicate;

        [SerializeField]
        private GameObject[] _objectsToRandomDisable;

        [SerializeField]
        private int _objectsToRandomDisableCount;

        [SerializeField]
        private Transform _agentsRoot;

        [SerializeField]
        private SettingsByTeam[] _settingsByTeams;

        private Dictionary<HideAndSeekAgent.TeamId, SettingsByTeam> _cachedSettingsByTeamId;
        private Dictionary<HideAndSeekAgent.TeamId, SettingsByTeam> CachedSettingsByTeamId {
            get {
                if (_cachedSettingsByTeamId == null) {
                    _cachedSettingsByTeamId = _settingsByTeams.ToDictionary(settings => settings.teamId);
                }
                return _cachedSettingsByTeamId;
            }
        }

        [SerializeField]
        private Transform _centerPoint;
        public Transform CenterPoint => _centerPoint;

        [SerializeField]
        private int _maxSteps;

        [Header("Player")]
        [SerializeField]
        private bool _spawnPlayer;

        [SerializeField]
        private HideAndSeekAgent.TeamId _playerTeam;

        [SerializeField]
        private GameCameraController _playerCamera;

        public float StepProgress => (float)_currentSteps / _maxSteps;
        private int _currentSteps;
        private HideAndSeekAgent.TeamId _winnerTeamId;

        private Dictionary<HideAndSeekAgent.TeamId, List<float>> _cumulativeRewardsByTeam = new Dictionary<HideAndSeekAgent.TeamId, List<float>>();

        public Dictionary<HideAndSeekAgent.TeamId, List<HideAndSeekAgent>> AgentInstancesByTeamId { get; private set; } = new Dictionary<HideAndSeekAgent.TeamId, List<HideAndSeekAgent>>();
        public Dictionary<HideAndSeekAgent.TeamId, SimpleMultiAgentGroup> AgentGroupByTeamId { get; private set; } = new Dictionary<HideAndSeekAgent.TeamId, SimpleMultiAgentGroup>();

        private void Start() {
            foreach (var settings in _settingsByTeams) {
                AgentGroupByTeamId.Add(settings.teamId, new SimpleMultiAgentGroup());
                AgentInstancesByTeamId.Add(settings.teamId, new List<HideAndSeekAgent>());
                _cumulativeRewardsByTeam[settings.teamId] = new List<float>();
                for (var i = 0; i < settings.agentsCount; i++) {
                    _cumulativeRewardsByTeam[settings.teamId].Add(0);
                    var agent = Instantiate(settings.agentPrefab, _agentsRoot);
                    agent.Health.onDamaged += (damageInfo) => OnAgentDamaged(damageInfo.damageOwner, agent);
                    AgentInstancesByTeamId[settings.teamId].Add(agent);
                    agent.Init(this);
                }
            }

            ResetEnvironment();
            if (_spawnPlayer) {
                var playerAgent = AgentInstancesByTeamId[_playerTeam][UnityEngine.Random.Range(0, AgentInstancesByTeamId[_playerTeam].Count)];
                Instantiate(_playerCamera, playerAgent.transform);
                playerAgent.BehaviorParameters.BehaviorType = Unity.MLAgents.Policies.BehaviorType.HeuristicOnly;
                if(playerAgent.DemonstrationRecorder != null) {
                    playerAgent.DemonstrationRecorder.Record = true;
                }
            }
        }

        private void FixedUpdate() {
            foreach (var group in AgentGroupByTeamId) {
                group.Value.AddGroupReward(CachedSettingsByTeamId[group.Key].timeReward / _maxSteps);
            }
            foreach (var agents in AgentInstancesByTeamId) {
                for (var i = 0; i < agents.Value.Count; i++) {
                    _cumulativeRewardsByTeam[agents.Key][i] = agents.Value[i].GetCumulativeReward();
                }
            }
            bool endEpisode = CheckEndConditions();
            if (endEpisode) {
                EndEpisode();
            }
        }

        private void RandomizeObjects() {
            foreach(var objectToRandomize in _objectsToRandomDisable) {
                objectToRandomize.gameObject.SetActive(true);
            }
            var listToRandomDisable = new List<GameObject>(_objectsToRandomDisable);
            for(var i = 0; i < _objectsToRandomDisableCount; i++) {
                var itemToDisable = listToRandomDisable[UnityEngine.Random.Range(0, listToRandomDisable.Count)];
                itemToDisable.gameObject.SetActive(false);
            }
        }

        private bool CheckEndConditions() {
            _currentSteps++;
            if (_currentSteps >= _maxSteps) {
                _winnerTeamId = HideAndSeekAgent.TeamId.Hider;
                return true;
            }

            if (AgentInstancesByTeamId[HideAndSeekAgent.TeamId.Seeker].All(a => a.Health.IsDead)) {
                _winnerTeamId = HideAndSeekAgent.TeamId.Hider;
                return true;
            }
            if (AgentInstancesByTeamId[HideAndSeekAgent.TeamId.Hider].All(a => a.Health.IsDead)) {
                _winnerTeamId = HideAndSeekAgent.TeamId.Seeker;
                return true;
            }
            return false;
        }

        private void EndEpisode() {
            foreach (var group in AgentGroupByTeamId) {
                var teamId = group.Key;
                var settings = CachedSettingsByTeamId[teamId];
                float reward = (teamId == _winnerTeamId) ? settings.winReward : -settings.winReward;
                group.Value.AddGroupReward(reward);
            }
            foreach (var group in AgentGroupByTeamId.Values) group.EndGroupEpisode();
            ResetEnvironment();
        }

        public void ResetEnvironment() {
            _currentSteps = 0;
            foreach (var settings in _settingsByTeams) {
                ResetAgents(AgentGroupByTeamId[settings.teamId],
                    settings.spawnPoints, AgentInstancesByTeamId[settings.teamId]);
            }
            SetIndicatorMaterial();
            RandomizeObjects();
        }

        private void ResetAgents(SimpleMultiAgentGroup agentGroup, Transform[] spawnPoints, List<HideAndSeekAgent> agents) {
            var list = new List<Transform>(spawnPoints);
            foreach (var agent in agents) {
                var spawn = list[UnityEngine.Random.Range(0, list.Count)]; list.Remove(spawn);
                agentGroup.RegisterAgent(agent);
                agent.ResetAgent(); 
                agent.transform.position = spawn.position;
            }
        }

        private void OnAgentDamaged(HideAndSeekAgent owner, HideAndSeekAgent victim) {
            var group = AgentGroupByTeamId[owner.AgentTeamId];
            var settings = CachedSettingsByTeamId[owner.AgentTeamId];
            group.AddGroupReward(settings.damageReward);
            if (victim.Health.IsDead) {
                group.AddGroupReward(settings.killReward);
                victim.AddReward(CachedSettingsByTeamId[victim.AgentTeamId].deathReward);
            }
        }

        private void SetIndicatorMaterial() {
            Material material = null;
            material = CachedSettingsByTeamId[_winnerTeamId].winMaterial;
            if (material == null) {
                return;
            }
            foreach (var r in _objectsToIndicate) {
                r.material = material;
            }
        }

        private void OnGUI() {
            GUIStyle style = new GUIStyle(GUI.skin.label) 
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = Color.white } 
            };
            switch (_displayMode) {
                case RewardDisplayMode.Center:
                    DrawGUICenter(style);
                    break;
                case RewardDisplayMode.AboveAgents:
                    DrawGUIAboveAgents(style);
                    break;
            }
        }

        private void DrawGUICenter(GUIStyle style) {
            float startX = Screen.width / 2f;
            float y = 10f;
            int idx = 0;
            foreach (var kvp in _cumulativeRewardsByTeam) {
                string text = $"{kvp.Key}: {kvp.Value:F2}";
                Vector2 size = style.CalcSize(new GUIContent(text));
                Rect rect = new Rect(startX - size.x / 2 + idx * (size.x + 10), y, size.x, size.y);
                GUI.Label(rect, text, style);
                idx++;
            }
        }

        private void DrawGUIAboveAgents(GUIStyle style) {
            Camera cam = Camera.main;
            if (cam == null) return;
            foreach (var agents in AgentInstancesByTeamId) {
                for (var i = 0; i < agents.Value.Count; i++) {
                    Vector3 worldPos = agents.Value[i].transform.position + Vector3.up * 2f;
                    Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                    if (screenPos.z > 0) {
                        string text = $"{agents.Key}: {_cumulativeRewardsByTeam[agents.Key][i]:F2}" +
                            $" Health:{agents.Value[i].Health.CurrentHealthValue}";
                        Vector2 size = style.CalcSize(new GUIContent(text));
                        Rect rect = new Rect(screenPos.x - size.x / 2,
                            Screen.height - screenPos.y - size.y / 2,
                            size.x * 2, size.y * 2);
                        GUI.Label(rect, text, style);
                    }
                }

            }
        }
    }
}


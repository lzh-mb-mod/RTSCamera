* `MissionState.FinishMissionLoading`
  * `MissionState.Handler?.OnMissionAfterStarting(MissionState.CurrentMission)`
  * `MissionState.CurrentMission.AfterStart()`
    * `MBSubModuleBase.OnBeforeMissionBehaviorInitialize(Mission)`
    * `MissionBehaviour.OnBehaviorInitialize()`
    * `MBSubModuleBase.OnMissionBehaviorInitialize(Mission)`
    * `MissionBehaviour.EarlyStart()`
    * `BattleSpawnPathSelector.Initialize()`
    * `IMissionDeploymentPlan.Initialize()`
    * `MissionBehaviour.AfterStart()`
    * `MissionBehaviour.AfterMissionStart()`
    * `MissionGameModels.ApplyWeatherEffectsModel.ApplyWeatherEffects()`
    * `MissionState.CurrentState = Mission.State.Continuing`
  * `MissionState.Handler?.OnMissionLoadingFinished(MissionState.CurrentMission)`
  * `MisisonState.CurrentMission.Scene.ResumeLoadingRenderings()`

Determine whether player deployment is enabled:
`BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle`

Team initialization:
* `DeploymentMissionController.SetupTeams()` is called in `OnMissionTick(dt)` after Mission.Scene is set to not null.
  * `Mission.DisableDying = true`
  * `Mission.SetFallAvoidSystemActive(true)`
  * Call `DeploymentMissionController.OnSetupTeamsOfSide(DeploymentMissionController.EnemySide)`:
    - For implementation `BattleDeploymentMissionController.OnSetupTeamsOfSide(battleSide)`:
      * `MissionAgentSpawnLogic.SetSpawnTroops(battleSide, true, true)`
      * `DeploymentMissionController.SetupAgentAIStatesForSide(battleSide)`
        * For each team of battleSide:
          * For each formation in the team:
            * For each agent in the formation:
              * `Agent.SetAlarmState(Agent.AIStateFlag.None)`
              * `Agent.SetIsAIPaused(true)`
      * `MissionAgentSpawnLogic.OnSideDeploymentOver(battleSide)`
        * For each team of battleSide, call `Mission.OnTeamDeployed(team)`
          * **`MissionBehaviour.OnTeamDeployed(team)`**
            * `AssignPlayerRoleInTeamMissionController.OnTeamDeployed(team)`
              * If team is player team:
                * Set `Team.PlayerOrderController.Owner` to main agent
                * If player is player team general:
                  * Set `Formation.PlayerOwner` to main agent.
            * `GeneralsAndCaptainsAssignmentLogic.OnTeamDeployed(team)`
              * `GeneralsAndCaptainsAssignmentLogic.SetGeneralAgentOfTeam(team)`
                * If is player team and player is team general: set main agent as `Team.GeneralAgent`
                * Else, get general agent by name and set to `Team.GeneralAgent`
              * If is player team:
                * If `BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle()` is false:
                  * If `GeneralsAndCaptainsAssignmentLogic.CanTeamHaveGeneralsFormation(team)` is true:
                    * `GeneralsAndCaptainsAssignmentLogic.CreateGeneralFormationForTeam(team)` which creates general formation and add `Team.GeneralAgent` to it. Optionally create bodyguard formation.
                    * Set `GeneralsAndCaptainsAssignmentLogic._isPlayerTeamGeneralFormationSet` to true
                  * `GeneralsAndCaptainsAssignmentLogic.AssignBestCaptainsForTeam(team)`
              * else (is non-player team):
                * If `GeneralsAndCaptainsAssignmentLogic.CanTeamHaveGeneralsFormation(team)` is true:
                  * `GeneralsAndCaptainsAssignmentLogic.CreateGeneralFormationForTeam(team)` which creates general formation and add `Team.GeneralAgent` to it. Optionally create bodyguard formation.
                * `GeneralsAndCaptainsAssignmentLogic.AssignBestCaptainsForTeam(team)`
        * `Mission.OnBattleSideDeployed(battleSide)`
          * **`MissionBehaviour.OnBattleSideDeployed(battleSide)`**
            * `DeploymentHandler.OnBattleSideDeployed(battleSide)`
              * If battleSide is player team side:
                * Trigger event `DeploymentHandler.OnPlayerSideDeploymentReady`
                  * Trigger handler `MissionOrderDeploymentControllerVM.ExecuteDeployPlayerSide`
                    * ...
                    * `MissionOrderDeploymentControllerVM.DeployFormationsOfPlayer()`
                      * ...
                      * `AssignPlayerRoleInTeamMissionController.OnPlayerTeamDeployed()`
                        * If `BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle()` returns true:
                          * If player is not general:
                            * Determine agents list by priority and choose formation... (calls `ChooseFormationToLead`). main agent should not be added to general at this moment
                            * Trigger event `AssignPlayerRoleInTeamMissionController.OnPlayerTurnToChooseFormationToLead`
                              * Trigger handler `MissionGauntletOrderOfBattleUIHandler.OnPlayerTurnToChooseFormationToLead`
                                * `OrderOfBattleVM.Initialize(...)`
        * For each team of battleSide:
          * For each formation from index 0 to 7 in the team:
            * Call `Formation.QuerySystem.EvaluateAllPreliminaryQueryData()` if `Formation.CountOfUnits > 0`.
          * Register `MissionAgentSpawnLogic.OrderController_OnOrderIssued` as handler of `Team.MasterOrderContorller.OnOrderInssued`, which in turn calls `DeploymentHandler.OrderController_OnOrderIssued_Aux`, which would teleport formations according to orders.
          * For each formation from index 8 to 9 in the team (General formation and Bodyguard formation):
            * Execute the following if `Formation.CountOfUnits > 0`:
              * Team.MasterOrderController.SelectFormation(formation)
              * Team.MasterOrderController.SetOrderWithAgent(OrderType.FollowMe, team.GeneralAgent)
              * Team.MasterOrderController.ClearSelectedFormations()
              * formation.SetControlledByAI(true)
          * Unregister `MissionAgentSpawnLogic.OrderController_OnOrderIssued` as handler of `Team.MasterOrderContorller.OnOrderInssued`.
    - For implementation `SiegeDeploymentMissionController.OnSetupTeamsOfSide(battleSide)`:
      * For the team that's leading the `battleSide` (`Mission.AttackerTeam` for `BattleSideEnum.Attacker`, `Mission.DefenderTeam` for `BattleSideEnum.Defender`, that is, ignoring ally team):
        * If the team is player team:
          * `SiegeDeploymentHandler.RemoveUnavailableDeploymentPoints(battleSide)`
          * `SiegeDeploymentHandler.UnHideDeploymentPoints(battleSide)`
          * `SiegeDeploymentHandler.DeployAllSiegeWeaponsOfPlayer()`
        * Else
          * `SiegeDeploymentHandler.DeployAllSiegeWeaponsOfAi()`
      * `MissionAgentSpawnLogic.SetSpawnTroops(battleSide, true, true)`
      * For each siege weapon of battleSide:
        * `SiegeWeapon.TickAuxForInit`
      * `DeploymentMissionController.SetupAgentAIStatesForSide(battleSide)`
      * If the team is player team:
        * For each formation, call `Formation.SetControlledByAI(true)`
      * `MissionAgentSpawnLogic.OnSideDeploymentOver(battleSide)`
    - For implementation `NavalDeploymentMissionController.OnSetupTeamOfSide(battleSide)`
      * `DefaultNavalMissionLogic.DeployBattleSide(battleSide)`
      * `ShipAgentSpawnLogic.AllocateAndDeployInitialTroops(battleSide)`
      * `DeploymentMissionController.SetupAgentAIStatesForSide(battleSide)`
      * `ShipAgentSpawnLogic.OnSideDeploymentOver(battleSide)`
  * `DeploymentMissionController.SetupAIOfEnemySide(DeploymentMissionController.EnemySide)`:
    - For default implementation `DeploymentMissionController.SetupAIOfEnemySide(battleSide)`:
      * For the team that's leading the `battleSide`, call `DeploymentMissionController.SetupAIOfEnemyTeam(team)`:
        * Call `DeploymentMissionController.SetupAIOfEnemyTeam(team)`
          * For default implementation `DeploymentMissionController.SetupAIOfEnemyTeam(team)`:
            * For each formation of the team, if `Formation.CountOfUnits > 0`, call `Formation.SetControlledByAI(true)
            * Tick team to teleport agents.
      * For ally team of `battleSide` if not null, `DeploymentMissionController.SetupAIOfEnemyTeam(team)`
    - For implementation `NavalDeploymentMissionController.SetupAIOfEnemySide(battleSide)`:
      * The same as `DeploymentMissionController.SetupAIOfEnemySide(battleSide)`
  * If player is attacker:
    * Call `DeploymentMissionController.HideAgentsOfSide(BattleSideEnum.Defender)`
      * Hide all agents of `battleSide`
  * Call `DeploymentMissionController.OnSetupTeamsOfSide(DeploymentMissionController.PlayerSide)`
  * Call `DeploymentMissionController.OnSetupTeamsFinished()`:
    - For implementation `BattleDeploymentMissionController.OnSetupTeamsFinished()`:
      * `Mission.IsTeleportingAgents = true`
    - For implementation `SiegeDeploymentMissionController.OnSetupTeamsFinished()`:
      * `Mission.IsTeleportingAgents = true`
    -  For implementation `NavalDeploymentMissionController.OnsetupTeamsFinished()`:
      * `NavalShipsLogic.SetTeleportShips(true)`
  * Unregister `DeploymentMissionController.AreOrderGesturesEnabled_AdditionalCondition` to `Mission.AreOrderGesturesEnabled_AdditionalCondition`
  * If call of `BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle()` return `false`
    * Call `DeploymentMissionController.FinishDeployment()`
* `DeploymentMissionController.OnAfterSetupTeams` is called after `SetupTeams` is called.

### Player clicked Ready
* `MissionOrderDeploymentControllerVM.ExecuteBeginMission()`
  * `MissionOrderDeploymentControllerVM.IsSiegeDeploymentListActive = false`
  * `MissionOrder.TryCloseToggleOrder()`
  * `DeploymentHandler.FinishDeployment()`
    * `DeploymentMissionController.FinishDeployment()`
    * `Mission.IsTeleportingAgents = false`



### `DeploymentMissionController.FinishDeployment()`
* `DeploymentMissionController.BeforeDeploymentFinished()`:
  - For implementation `BattleDeploymentMissionController.BeforeDeploymentFinished()`:
    * `Mission.IsTeleportingAgents = false`
  - For implementation `SiegeDeploymentMissionController.BeforeDeploymentFinished()`:
    * `SiegeDeploymentHandler.RemoveDeploymentPoints(this.Mission.PlayerTeam.Side)`;`
    * Disable invisible siege ladders.
    * `Mission.IsTeleportingAgents = false`
  -  For implementation `NavalDeploymentMissionController.BeforeDeploymentFinished()`:
    * `NavalShipsLogic.SetTeleportShips(false)`
* If player is attacker:
  * Call `DeploymentMissionController.UnhideAgentsOfSide(BattleSideEnum.Defender)`
  * Call `Mission.OnDeploymentFinished()`
    * `Mission.IsDeploymentFinished = true`
    * For each team:
      * If `Team.TeamAI` is not null:
        * `Team.TeamAI.OnDeploymentFinished()`
    * **`MissionBehaviour.OnDeploymentFinished()`**
      * `MissionGauntletOrderOfBattleUIHandler.OnDeploymentFinished()`
        * `OrderOfBattleVM.OnDeploymentFinalized(playerDeployed)`
          
          (`playerDeployed` is `BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle()`)
          * If `playerDeployed`:
            * If player is captain of a formation
              * `AssignPlayerRoleInTeamMissionController.OnPlayerChoiceMade(formationIndex)`
                * ...
                * For remaining agent in agents list by priority call `AssignPlayerRoleInTeamMissionController.ChooseFormationToLead(...)` main agent should not be added to general at this moment
              * `AssignPlayerRoleInTeamMissionController.OnPlayerChoiceFinalized()`
              * Trigger `AssignPlayerRoleInTeamMissionController.OnAllFormationsAssignedSergeants`
      * `GeneralsAndCaptainsAssignmentLogic.OnDeploymentFinished ()`:
        * If `_isPlayerTeamGeneralFormationSet` is false and `CanTeamHaveGeneralsFormation(playerTeam)` is true:
          * `CreateGeneralFormationForTeam(playerTeam)`
          * Set `_isPlayerTeamGeneralFormationSet` to true.
        * If `_isPlayerTeamGeneralFormationSet` is true, and main agent is not null, and player team general is not main agent, and is not naval battle:
          * Add main agent to general formation.
          * `mainAgent.Team.TriggerOnFormationsChanged(formation)`
            * Trigger `Team.OnFormationsChanged`
              * Trigger `MissionAgentLabelView.PlayerTeam_OnFormationsChanged`
  * For each team:
    * For each formation in the team:
      * For each agent in the formation:
        * If agent is AI Controlled:
          * `Agent.SetAlarmState(Agent.AIStateFlag.Alarmed)`
          * `Agent.SetIsAIPaused(false)`
          * if agent has agent flag `AgentFlag.CanWieldWeapon`:
            * `Agent.ResetEnemyCaches()`
          * `Agent.HumanAIComponent?.SyncBehaviorParamsIfNecessary()`
  * If `Mission.MainAgent` is not null:
    * `Mission.MainAgent.SetDetachableFromFormation(true)`
    * `Mission.MainAgent.Controller = AgentControllerType.Player`
  * `Mission.AllowAiTicking = true`
  * `Mission.DisableDying = false`
  * `Mission.SetFallAvoidSystemActive(false)`
  * `Mission.OnAfterDeploymentFinished()`
    * **`MissionBehaviour.OnAfterDeploymentFinished()`**
  * `DeploymentMissionController.AfterDeploymentFinished()`
    * For implementation `BattleDeploymentMissionController.AfterDeploymentFinished()`:
      * `MissionAgentSpawnLogic.SetReinforcementsSpawnEnabled(true)`
      * `Mission.RemoveMissionBehavior(BattleDeploymentHandler)`
        * `Mission.SetMissionMode(PreviousMissionMode, false)`
          * **`Missionbehaviour.OnMissionModeChange(PreviousMissionMode, false)`**
    * For implementation `SiegeDeploymentMissionController.AfterDeploymentFinished()`:
      * `MissionAgentSpawnLogic.SetReinforcementsSpawnEnabled(true)`
      * `Mission.RemoveMissionBehavior(SiegeDeploymentHandler)`
        * `Mission.SetMissionMode(PreviousMissionMode, false)`
          * **`Missionbehaviour.OnMissionModeChange(PreviousMissionMode, false)`**
    * For implementation `NavalDeploymentMissionController.AfterDeploymentFinished()`
      * `Mission.RemoveMissionbehavior(NavalDeploymentHandler)`
        * `Mission.SetMissionMode(PreviousMissionMode, false)`
          * **`Missionbehaviour.OnMissionModeChange(PreviousMissionMode, false)`**
  * `Mission.RemoveMissionBehavior(DeploymentMissionController)`








Current call chain during battle start up is:
* `OnMissionModeChange(StartUp)` with Mission.Mode == Battle triggered by `MissionCombatantsLogic.AfterStart()`
* OnMissionModeChange(Battle) with Mission.Mode == Deployment triggered by `DeploymentHandler.AfterStart()`
* OnMainAgentChanged of DeploymentMissionController triggered by `MainAgent.Controller = Controller.Player`
* followed by OnAgentControllerChanged
* OnAgentControllerChanged triggered by `MainAgent.Controller = Controller.None` in `DeploymentMissionController.OnAgentControllerSetToPlayer`

If has deployment stage:
* OnTeamDeployed is called
* Player deploy troops and click ready ...
* Player is assigned to general formation in GeneralsAndCaptainsAssignmentLogic.OnDeploymentFinished
* OnDeploymentFinished is called
else:
* Player is assigned to general formation if player is general in GeneralsAndCaptainsAssignmentLogic.OnTeamDeployed
* OnTeamDeployed is called
* In the same tick, Player is added to general formation if player is not general in GeneralsAndCaptainsAssignmentLogic.OnDeploymentFinished
* In the same tick, OnDeploymentFinished is called.

* OnMainAgentChanged triggered by `MainAgent.Controller = Controller.Player` in DeploymentMissionController
* followed by OnAgentControllerChanged
* OnMissionModeChange(Deployment) with Mission.Mode == Deployment


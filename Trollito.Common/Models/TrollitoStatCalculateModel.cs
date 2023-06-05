using MBHelpers;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace Trollito.Common.Models
{
    /// <summary>
    /// GameModel calculating agents stats.
    /// Apply different multiplier on stats depending on agents AI difficulty or player formation.    
    /// </summary>
    public class TrollitoStatCalculateModel : AgentStatCalculateModel
    {
        private int GetSkillValueForItem(Agent agent, ItemObject primaryItem)
        {
            return agent.Character.GetSkillValue(primaryItem != null ? primaryItem.RelevantSkill : DefaultSkills.Athletics);
        }

        public override float GetKnockDownResistance(Agent agent, StrikeType strikeType = StrikeType.Invalid)
        {
            float num = 0.5f;
            if (agent.Name.ToLower().Contains("troll"))
            {
                num += 10f;
            }
            else if (agent.HasMount)
            {
                num += 0.1f;
            }
            else if (strikeType == StrikeType.Thrust)
            {
                num += 0.25f;
            }
            return num;
        }

        public override void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            if (agent.IsHuman)
            {
                UpdateHumanStats(agent, agentDrivenProperties);
                return;
            }
            UpdateHorseStats(agent, agentDrivenProperties);
        }

        private void UpdateHumanStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            BasicCharacterObject character = agent.Character;
            MissionEquipment equipment = agent.Equipment;
            float num = equipment.GetTotalWeightOfWeapons();
            int weight = agent.Monster.Weight;
            float num2 = agentDrivenProperties.ArmorEncumbrance + num;
            EquipmentIndex wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            EquipmentIndex wieldedItemIndex2 = agent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
            if (wieldedItemIndex != EquipmentIndex.None)
            {
                ItemObject item = equipment[wieldedItemIndex].Item;
                float realWeaponLength = item.WeaponComponent.PrimaryWeapon.GetRealWeaponLength();
                num += 1.5f * item.Weight * MathF.Sqrt(realWeaponLength);
            }
            if (wieldedItemIndex2 != EquipmentIndex.None)
            {
                ItemObject item2 = equipment[wieldedItemIndex2].Item;
                num += 1.5f * item2.Weight;
            }
            agentDrivenProperties.WeaponsEncumbrance = num;
            EquipmentIndex wieldedItemIndex3 = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            WeaponComponentData weaponComponentData = wieldedItemIndex3 != EquipmentIndex.None ? equipment[wieldedItemIndex3].CurrentUsageItem : null;
            ItemObject itemObject = wieldedItemIndex3 != EquipmentIndex.None ? equipment[wieldedItemIndex3].Item : null;
            EquipmentIndex wieldedItemIndex4 = agent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
            WeaponComponentData weaponComponentData2 = wieldedItemIndex4 != EquipmentIndex.None ? equipment[wieldedItemIndex4].CurrentUsageItem : null;
            agentDrivenProperties.SwingSpeedMultiplier = 0.93f + 0.0007f * GetSkillValueForItem(agent, itemObject);
            agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier = agentDrivenProperties.SwingSpeedMultiplier;
            agentDrivenProperties.HandlingMultiplier = 1f;
            agentDrivenProperties.ShieldBashStunDurationMultiplier = 1f;
            agentDrivenProperties.KickStunDurationMultiplier = 1f;
            agentDrivenProperties.ReloadSpeed = 0.93f + 0.0007f * GetSkillValueForItem(agent, itemObject);
            agentDrivenProperties.MissileSpeedMultiplier = 1f;
            agentDrivenProperties.ReloadMovementPenaltyFactor = 1f;
            SetAllWeaponInaccuracy(agent, agentDrivenProperties, (int)wieldedItemIndex3, weaponComponentData);
            IAgentOriginBase origin = agent.Origin;
            BasicCharacterObject character2 = agent.Character;
            Formation formation = agent.Formation;
            int effectiveSkill = GetEffectiveSkill(character2, origin, agent.Formation, DefaultSkills.Athletics);
            int effectiveSkill2 = GetEffectiveSkill(character2, origin, formation, DefaultSkills.Riding);
            if (weaponComponentData != null)
            {
                WeaponComponentData weaponComponentData3 = weaponComponentData;
                int effectiveSkillForWeapon = GetEffectiveSkillForWeapon(agent, weaponComponentData3);
                if (weaponComponentData3.IsRangedWeapon)
                {
                    int thrustSpeed = weaponComponentData3.ThrustSpeed;
                    if (!agent.HasMount)
                    {
                        float num3 = MathF.Max(0f, 1f - effectiveSkillForWeapon / 500f);
                        agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = 0.125f * num3;
                        agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = 0.1f * num3;
                    }
                    else
                    {
                        float num4 = MathF.Max(0f, (1f - effectiveSkillForWeapon / 500f) * (1f - effectiveSkill2 / 1800f));
                        agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = 0.025f * num4;
                        agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = 0.12f * num4;
                    }
                    agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = MathF.Max(0f, agentDrivenProperties.WeaponMaxMovementAccuracyPenalty);
                    agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = MathF.Max(0f, agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty);
                    if (weaponComponentData3.RelevantSkill == DefaultSkills.Bow)
                    {
                        float num5 = (thrustSpeed - 45f) / 90f;
                        num5 = MBMath.ClampFloat(num5, 0f, 1f);
                        agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= 6f;
                        agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= 4.5f / MBMath.Lerp(0.75f, 2f, num5, 1E-05f);
                    }
                    else if (weaponComponentData3.RelevantSkill == DefaultSkills.Throwing)
                    {
                        float num6 = (thrustSpeed - 89f) / 13f;
                        num6 = MBMath.ClampFloat(num6, 0f, 1f);
                        agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= 3.5f * MBMath.Lerp(1.5f, 0.8f, num6, 1E-05f);
                    }
                    else if (weaponComponentData3.RelevantSkill == DefaultSkills.Crossbow)
                    {
                        agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= 2.5f;
                        agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= 1.2f;
                    }
                    if (weaponComponentData3.WeaponClass == WeaponClass.Bow)
                    {
                        agentDrivenProperties.WeaponBestAccuracyWaitTime = 0.3f + (95.75f - thrustSpeed) * 0.005f;
                        float num7 = (thrustSpeed - 45f) / 90f;
                        num7 = MBMath.ClampFloat(num7, 0f, 1f);
                        agentDrivenProperties.WeaponUnsteadyBeginTime = 0.6f + effectiveSkillForWeapon * 0.01f * MBMath.Lerp(2f, 4f, num7, 1E-05f);
                        if (agent.IsAIControlled)
                        {
                            agentDrivenProperties.WeaponUnsteadyBeginTime *= 4f;
                        }
                        agentDrivenProperties.WeaponUnsteadyEndTime = 2f + agentDrivenProperties.WeaponUnsteadyBeginTime;
                        agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.1f;
                    }
                    else if (weaponComponentData3.WeaponClass == WeaponClass.Javelin || weaponComponentData3.WeaponClass == WeaponClass.ThrowingAxe || weaponComponentData3.WeaponClass == WeaponClass.ThrowingKnife)
                    {
                        agentDrivenProperties.WeaponBestAccuracyWaitTime = 0.4f + (89f - thrustSpeed) * 0.03f;
                        agentDrivenProperties.WeaponUnsteadyBeginTime = 2.5f + effectiveSkillForWeapon * 0.01f;
                        agentDrivenProperties.WeaponUnsteadyEndTime = 10f + agentDrivenProperties.WeaponUnsteadyBeginTime;
                        agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.025f;
                    }
                    else
                    {
                        agentDrivenProperties.WeaponBestAccuracyWaitTime = 0.1f;
                        agentDrivenProperties.WeaponUnsteadyBeginTime = 0f;
                        agentDrivenProperties.WeaponUnsteadyEndTime = 0f;
                        agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.1f;
                    }
                }
                else if (weaponComponentData3.WeaponFlags.HasAllFlags(WeaponFlags.WideGrip))
                {
                    agentDrivenProperties.WeaponUnsteadyBeginTime = 1f + effectiveSkillForWeapon * 0.005f;
                    agentDrivenProperties.WeaponUnsteadyEndTime = 3f + effectiveSkillForWeapon * 0.01f;
                }
                if (agent.HasMount)
                {
                    float num8 = 1f - MathF.Max(0f, 0.2f - effectiveSkill2 * 0.002f);
                    agentDrivenProperties.SwingSpeedMultiplier *= num8;
                    agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier *= num8;
                    agentDrivenProperties.ReloadSpeed *= num8;
                }
            }
            agentDrivenProperties.TopSpeedReachDuration = 2f / MathF.Max((200f + effectiveSkill) / 300f * (weight / (weight + num2)), 0.3f);
            float num9 = 0.7f + 0.00070000015f * effectiveSkill;
            float num10 = MathF.Max(0.2f * (1f - effectiveSkill * 0.001f), 0f) * num2 / weight;
            float num11 = MBMath.ClampFloat(num9 - num10, 0f, 0.91f);
            agentDrivenProperties.MaxSpeedMultiplier = GetEnvironmentSpeedFactor(agent) * num11;
            float managedParameter = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalCombatSpeedMinMultiplier);
            float managedParameter2 = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalCombatSpeedMaxMultiplier);
            float num12 = MathF.Min(num2 / weight, 1f);
            agentDrivenProperties.CombatMaxSpeedMultiplier = MathF.Min(MBMath.Lerp(managedParameter2, managedParameter, num12, 1E-05f), 1f);

            // Adjust speed depending on race. MaxSpeed is multiplied with native_parameters.xml/bipedal_speed_multiplier
            if (agent.Character.Race != 0 && agent.Character.Race == FaceGen.GetRaceOrDefault("troll"))
            {
                agentDrivenProperties.MaxSpeedMultiplier *= 1.7f;
            }

            agentDrivenProperties.AttributeShieldMissileCollisionBodySizeAdder = 0.3f;
            Agent mountAgent = agent.MountAgent;
            float num13 = mountAgent != null ? mountAgent.GetAgentDrivenPropertyValue(DrivenProperty.AttributeRiding) : 1f;
            agentDrivenProperties.AttributeRiding = effectiveSkill2 * num13;
            agentDrivenProperties.AttributeHorseArchery = Game.Current.BasicModels.StrikeMagnitudeModel.CalculateHorseArcheryFactor(character);
            agentDrivenProperties.BipedalRangedReadySpeedMultiplier = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRangedReadySpeedMultiplier);
            agentDrivenProperties.BipedalRangedReloadSpeedMultiplier = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRangedReloadSpeedMultiplier);
            GetBannerEffectsOnAgent(agent, agentDrivenProperties, weaponComponentData);
            base.SetAiRelatedProperties(agent, agentDrivenProperties, weaponComponentData, weaponComponentData2);
            float num14 = 1f;
            if (!agent.Mission.Scene.IsAtmosphereIndoor)
            {
                float rainDensity = agent.Mission.Scene.GetRainDensity();
                float fog = agent.Mission.Scene.GetFog();
                if (rainDensity > 0f || fog > 0f)
                {
                    num14 += MathF.Min(0.3f, rainDensity + fog);
                }
                if (!MBMath.IsBetween(agent.Mission.Scene.TimeOfDay, 4f, 20.01f))
                {
                    num14 += 0.1f;
                }
            }
            agentDrivenProperties.AiShooterError *= num14;
        }

        private void UpdateHorseStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            Equipment spawnEquipment = agent.SpawnEquipment;
            EquipmentElement equipmentElement = spawnEquipment[EquipmentIndex.ArmorItemEndSlot];
            EquipmentElement equipmentElement2 = spawnEquipment[EquipmentIndex.HorseHarness];
            ItemObject item = equipmentElement.Item;
            float num = equipmentElement.GetModifiedMountSpeed(equipmentElement2) + 1;
            int modifiedMountManeuver = equipmentElement.GetModifiedMountManeuver(equipmentElement2);
            int num2 = 0;
            float environmentSpeedFactor = GetEnvironmentSpeedFactor(agent);
            if (agent.RiderAgent != null)
            {
                num2 = GetEffectiveSkill(agent.RiderAgent.Character, agent.RiderAgent.Origin, agent.RiderAgent.Formation, DefaultSkills.Riding);
                FactoredNumber factoredNumber = new FactoredNumber(num);
                FactoredNumber factoredNumber2 = new FactoredNumber(modifiedMountManeuver);
                factoredNumber.AddFactor(num2 * 0.001f);
                factoredNumber2.AddFactor(num2 * 0.0004f);
                Formation formation = agent.RiderAgent.Formation;
                BannerComponent activeBanner = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(formation);
                if (activeBanner != null)
                {
                    BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.IncreasedMountMovementSpeed, activeBanner, ref factoredNumber);
                }
                agentDrivenProperties.MountManeuver = factoredNumber2.ResultNumber;
                agentDrivenProperties.MountSpeed = environmentSpeedFactor * 0.22f * (1f + factoredNumber.ResultNumber);
            }
            else
            {
                agentDrivenProperties.MountManeuver = modifiedMountManeuver;
                agentDrivenProperties.MountSpeed = environmentSpeedFactor * 0.22f * (1f + num);
            }
            float num3 = equipmentElement.Weight / 2f + (equipmentElement2.IsEmpty ? 0f : equipmentElement2.Weight);
            agentDrivenProperties.MountDashAccelerationMultiplier = num3 > 200f ? num3 < 300f ? 1f - (num3 - 200f) / 111f : 0.1f : 1f;
            agentDrivenProperties.TopSpeedReachDuration = Game.Current.BasicModels.RidingModel.CalculateAcceleration(equipmentElement, equipmentElement2, num2);
        }

        private void GetBannerEffectsOnAgent(Agent agent, AgentDrivenProperties agentDrivenProperties, WeaponComponentData rightHandEquippedItem)
        {
            BannerComponent activeBanner = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(agent.Formation);
            if (agent.Character != null && activeBanner != null)
            {
                bool flag = rightHandEquippedItem != null && rightHandEquippedItem.IsRangedWeapon;
                FactoredNumber factoredNumber = new FactoredNumber(agentDrivenProperties.MaxSpeedMultiplier);
                FactoredNumber factoredNumber2 = new FactoredNumber(agentDrivenProperties.WeaponInaccuracy);
                if (flag && rightHandEquippedItem != null)
                {
                    BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.DecreasedRangedAccuracyPenalty, activeBanner, ref factoredNumber2);
                }
                BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.IncreasedTroopMovementSpeed, activeBanner, ref factoredNumber);
                agentDrivenProperties.MaxSpeedMultiplier = factoredNumber.ResultNumber;
                agentDrivenProperties.WeaponInaccuracy = factoredNumber2.ResultNumber;
            }
        }

        protected new void SetAiRelatedProperties(Agent agent, AgentDrivenProperties agentDrivenProperties, WeaponComponentData equippedItem, WeaponComponentData secondaryItem)
        {
            int meleeSkill = GetMeleeSkill(agent, equippedItem, secondaryItem);
            SkillObject skill = equippedItem == null ? DefaultSkills.Athletics : equippedItem.RelevantSkill;
            int effectiveSkill = GetEffectiveSkill(agent.Character, agent.Origin, agent.Formation, skill);
            float num1 = CalculateAILevel(agent, meleeSkill);
            float num2 = CalculateAILevel(agent, effectiveSkill);
            float num3 = MBMath.ClampFloat(num1, 0.0f, 1f);
            float num4 = MBMath.ClampFloat(num2, 0.0f, 1f);
            agentDrivenProperties.AiRangedHorsebackMissileRange = (float)(0.300000011920929 + 0.400000005960464 * (double)num4);
            agentDrivenProperties.AiFacingMissileWatch = (float)((double)num3 * 0.0599999986588955 - 0.959999978542328);
            agentDrivenProperties.AiFlyingMissileCheckRadius = (float)(8.0 - 6.0 * (double)num3);
            agentDrivenProperties.AiShootFreq = (float)(0.200000002980232 + 0.800000011920929 * (double)num4);
            agentDrivenProperties.AiWaitBeforeShootFactor = agent.PropertyModifiers.resetAiWaitBeforeShootFactor ? 0.0f : (float)(1.0 - 0.5 * (double)num4);
            agentDrivenProperties.AIBlockOnDecideAbility = MBMath.Lerp(0.05f, 0.95f, MBMath.ClampFloat((float)((Math.Pow((double)MBMath.Lerp(-10f, 10f, num3, 1E-05f), 3.0) + 1000.0) * 0.000500000023748726), 0.0f, 1f), 1E-05f);
            agentDrivenProperties.AIParryOnDecideAbility = MBMath.Lerp(0.05f, 0.95f, MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, num3, 1E-05f), 4.0) * 0.0001f, 0.0f, 1f), 1E-05f);
            agentDrivenProperties.AiTryChamberAttackOnDecide = (float)(((double)num3 - 0.150000005960464) * 0.100000001490116);
            agentDrivenProperties.AIAttackOnParryChance = 0.3f;
            agentDrivenProperties.AiAttackOnParryTiming = (float)(0.300000011920929 * (double)num3 - 0.200000002980232);
            agentDrivenProperties.AIDecideOnAttackChance = 0.0f;
            agentDrivenProperties.AIParryOnAttackAbility = 0.5f * MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, num3, 1E-05f), 4.0) * 0.0001f, 0.0f, 1f);
            agentDrivenProperties.AiKick = (float)(((double)num3 > 0.400000005960464 ? 0.400000005960464 : (double)num3) - 0.100000001490116);
            agentDrivenProperties.AiAttackCalculationMaxTimeFactor = num3;
            agentDrivenProperties.AiDecideOnAttackWhenReceiveHitTiming = (float)(-0.25 * (1.0 - (double)num3));
            agentDrivenProperties.AiDecideOnAttackContinueAction = (float)(-0.5 * (1.0 - (double)num3));
            agentDrivenProperties.AiDecideOnAttackingContinue = 0.1f * num3;
            agentDrivenProperties.AIParryOnAttackingContinueAbility = MBMath.Lerp(0.05f, 0.95f, MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, num3, 1E-05f), 4.0) * 0.0001f, 0.0f, 1f), 1E-05f);
            agentDrivenProperties.AIDecideOnRealizeEnemyBlockingAttackAbility = 0.5f * MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, num3, 1E-05f), 5.0) * 1E-05f, 0.0f, 1f);
            agentDrivenProperties.AIRealizeBlockingFromIncorrectSideAbility = 0.5f * MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, num3, 1E-05f), 5.0) * 1E-05f, 0.0f, 1f);
            agentDrivenProperties.AiAttackingShieldDefenseChance = (float)(0.200000002980232 + 0.300000011920929 * (double)num3);
            agentDrivenProperties.AiAttackingShieldDefenseTimer = (float)(0.300000011920929 * (double)num3 - 0.300000011920929);
            agentDrivenProperties.AiRandomizedDefendDirectionChance = (float)(1.0 - Math.Log((double)num3 * 7.0 + 1.0, 2.0) * 0.333330005407333);
            agentDrivenProperties.AISetNoAttackTimerAfterBeingHitAbility = MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, num3, 1E-05f), 2.0) * 0.01f, 0.05f, 0.95f);
            agentDrivenProperties.AISetNoAttackTimerAfterBeingParriedAbility = MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, num3, 1E-05f), 2.0) * 0.01f, 0.05f, 0.95f);
            agentDrivenProperties.AISetNoDefendTimerAfterHittingAbility = MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, num3, 1E-05f), 2.0) * 0.01f, 0.05f, 0.95f);
            agentDrivenProperties.AISetNoDefendTimerAfterParryingAbility = MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, num3, 1E-05f), 2.0) * 0.01f, 0.05f, 0.95f);
            agentDrivenProperties.AIEstimateStunDurationPrecision = 1f - MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, num3, 1E-05f), 2.0) * 0.01f, 0.05f, 0.95f);
            agentDrivenProperties.AiRaiseShieldDelayTimeBase = (float)(0.5 * (double)num3 - 0.75);
            agentDrivenProperties.AiUseShieldAgainstEnemyMissileProbability = (float)(0.100000001490116 + (double)num3 * 0.200000002980232);
            agentDrivenProperties.AiCheckMovementIntervalFactor = (float)(0.00499999988824129 * (1.0 - (double)num3));
            agentDrivenProperties.AiParryDecisionChangeValue = (float)(0.0500000007450581 + 0.699999988079071 * (double)num3);
            agentDrivenProperties.AiDefendWithShieldDecisionChanceValue = (float)(0.300000011920929 + 0.699999988079071 * (double)num3);
            agentDrivenProperties.AiMoveEnemySideTimeValue = (float)(0.5 * (double)num3 - 2.5);
            agentDrivenProperties.AiMinimumDistanceToContinueFactor = (float)(2.0 + 0.300000011920929 * (3.0 - (double)num3));
            agentDrivenProperties.AiHearingDistanceFactor = 1f + num3;
            agentDrivenProperties.AiChargeHorsebackTargetDistFactor = (float)(1.5 * (3.0 - (double)num3));

            // Reduce AI shooter error
            //agentDrivenProperties.AiShooterError = 0.004f / Math.Max(0.4f, AgentsInfoModel.Instance.Agents[agent.Index].Difficulty);
            agentDrivenProperties.AiShooterError = 0.004f; 
            float num5 = 1f - MBMath.ClampFloat(0.004f * (float)agent.Character.GetSkillValue(DefaultSkills.Bow), 0.0f, 0.99f);
            agentDrivenProperties.AiRangerLeadErrorMin = num5 * 0.2f;
            agentDrivenProperties.AiRangerLeadErrorMax = num5 * 0.3f;
            agentDrivenProperties.AiRangerVerticalErrorMultiplier = num5 * 0.1f;
            agentDrivenProperties.AiRangerHorizontalErrorMultiplier = num5 * ((float)Math.PI / 90f);
            agentDrivenProperties.AIAttackOnDecideChance = 50f;
        }

        public override void InitializeAgentStats(Agent agent, Equipment spawnEquipment, AgentDrivenProperties agentDrivenProperties, AgentBuildData agentBuildData)
        {
            agentDrivenProperties.ArmorEncumbrance = spawnEquipment.GetTotalWeightOfArmor(agent.IsHuman);
            if (agent.IsHuman)
            {
                agentDrivenProperties.ArmorHead = spawnEquipment.GetHeadArmorSum();
                agentDrivenProperties.ArmorTorso = spawnEquipment.GetHumanBodyArmorSum();
                agentDrivenProperties.ArmorLegs = spawnEquipment.GetLegArmorSum();
                agentDrivenProperties.ArmorArms = spawnEquipment.GetArmArmorSum();
                return;
            }
            agentDrivenProperties.AiSpeciesIndex = (int)spawnEquipment[EquipmentIndex.ArmorItemEndSlot].Item.Id.InternalValue;
            agentDrivenProperties.AttributeRiding = 0.8f + (spawnEquipment[EquipmentIndex.HorseHarness].Item != null ? 0.2f : 0f);
            float num = 0f;
            for (int i = 1; i < 12; i++)
            {
                if (spawnEquipment[i].Item != null)
                {
                    num += spawnEquipment[i].GetModifiedMountBodyArmor();
                }
            }
            agentDrivenProperties.ArmorTorso = num;
            ItemObject item = spawnEquipment[EquipmentIndex.ArmorItemEndSlot].Item;
            if (item != null)
            {
                HorseComponent horseComponent = item.HorseComponent;
                EquipmentElement equipmentElement = spawnEquipment[EquipmentIndex.ArmorItemEndSlot];
                EquipmentElement equipmentElement2 = spawnEquipment[EquipmentIndex.HorseHarness];
                agentDrivenProperties.MountChargeDamage = equipmentElement.GetModifiedMountCharge(equipmentElement2) * 0.01f;
                agentDrivenProperties.MountDifficulty = equipmentElement.Item.Difficulty;
            }
        }

        public override float GetDifficultyModifier()
        {
            return 1f;
        }

        public override bool CanAgentRideMount(Agent agent, Agent targetMount)
        {
            return agent.CheckSkillForMounting(targetMount);
        }

        public override float GetWeaponDamageMultiplier(BasicCharacterObject agentCharacter, IAgentOriginBase agentOrigin, Formation agentFormation, WeaponComponentData weapon)
        {
            float num = 1f;
            SkillObject skillObject = weapon != null ? weapon.RelevantSkill : null;
            if (agentCharacter != null && skillObject != null)
            {
                if (skillObject == DefaultSkills.OneHanded)
                {
                    int skillValue = agentCharacter.GetSkillValue(skillObject);
                    num += skillValue * 0.0015f;
                }
                else if (skillObject == DefaultSkills.TwoHanded)
                {
                    int skillValue2 = agentCharacter.GetSkillValue(skillObject);
                    num += skillValue2 * 0.0016f;
                }
                else if (skillObject == DefaultSkills.Polearm)
                {
                    int skillValue3 = agentCharacter.GetSkillValue(skillObject);
                    num += skillValue3 * 0.0007f;
                }
                else if (skillObject == DefaultSkills.Bow)
                {
                    int skillValue4 = agentCharacter.GetSkillValue(skillObject);
                    num += skillValue4 * 0.0011f;
                }
                else if (skillObject == DefaultSkills.Throwing)
                {
                    int skillValue5 = agentCharacter.GetSkillValue(skillObject);
                    num += skillValue5 * 0.0006f;
                }
            }
            return Math.Max(0f, num);
        }

        public override float GetKnockBackResistance(Agent agent)
        {
            BasicCharacterObject character;
            if (agent.IsHuman && (character = agent.Character) != null)
            {
                int effectiveSkill = GetEffectiveSkill(character, agent.Origin, agent.Formation, DefaultSkills.Athletics);
                float num = 0.15f + effectiveSkill * 0.001f;
                return Math.Max(0f, num);
            }
            return float.MaxValue;
        }

        public override float GetDismountResistance(Agent agent)
        {
            BasicCharacterObject character;
            if (agent.IsHuman && (character = agent.Character) != null)
            {
                int effectiveSkill = GetEffectiveSkill(character, agent.Origin, agent.Formation, DefaultSkills.Riding);
                float num = 0.4f + effectiveSkill * 0.001f;
                return Math.Max(0f, num);
            }
            return float.MaxValue;
        }

        public TrollitoStatCalculateModel()
        {
        }
    }
}
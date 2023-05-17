using MBHelpers;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace Trollito.Common
{
    /// <summary>
    /// GameModel calculating agents stats.
    /// Apply different multiplier on stats depending on agents AI difficulty or player formation.    
    /// </summary>
    public class TestAgentStatCalculateModel : AgentStatCalculateModel
    {
        private int GetSkillValueForItem(Agent agent, ItemObject primaryItem)
        {
            return agent.Character.GetSkillValue((primaryItem != null) ? primaryItem.RelevantSkill : DefaultSkills.Athletics);
        }

        public override float GetKnockDownResistance(Agent agent, StrikeType strikeType = StrikeType.Invalid)
        {
            float num = 0.5f;
            if(agent.Name.ToLower().Contains("troll"))
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
                this.UpdateHumanStats(agent, agentDrivenProperties);
                return;
            }
            this.UpdateHorseStats(agent, agentDrivenProperties);
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
            WeaponComponentData weaponComponentData = ((wieldedItemIndex3 != EquipmentIndex.None) ? equipment[wieldedItemIndex3].CurrentUsageItem : null);
            ItemObject itemObject = ((wieldedItemIndex3 != EquipmentIndex.None) ? equipment[wieldedItemIndex3].Item : null);
            EquipmentIndex wieldedItemIndex4 = agent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
            WeaponComponentData weaponComponentData2 = ((wieldedItemIndex4 != EquipmentIndex.None) ? equipment[wieldedItemIndex4].CurrentUsageItem : null);
            agentDrivenProperties.SwingSpeedMultiplier = 0.93f + 0.0007f * (float)this.GetSkillValueForItem(agent, itemObject);
            agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier = agentDrivenProperties.SwingSpeedMultiplier;
            agentDrivenProperties.HandlingMultiplier = 1f;
            agentDrivenProperties.ShieldBashStunDurationMultiplier = 1f;
            agentDrivenProperties.KickStunDurationMultiplier = 1f;
            agentDrivenProperties.ReloadSpeed = 0.93f + 0.0007f * (float)this.GetSkillValueForItem(agent, itemObject);
            agentDrivenProperties.MissileSpeedMultiplier = 1f;
            agentDrivenProperties.ReloadMovementPenaltyFactor = 1f;
            base.SetAllWeaponInaccuracy(agent, agentDrivenProperties, (int)wieldedItemIndex3, weaponComponentData);
            IAgentOriginBase origin = agent.Origin;
            BasicCharacterObject character2 = agent.Character;
            Formation formation = agent.Formation;
            int effectiveSkill = this.GetEffectiveSkill(character2, origin, agent.Formation, DefaultSkills.Athletics);
            int effectiveSkill2 = this.GetEffectiveSkill(character2, origin, formation, DefaultSkills.Riding);
            if (weaponComponentData != null)
            {
                WeaponComponentData weaponComponentData3 = weaponComponentData;
                int effectiveSkillForWeapon = this.GetEffectiveSkillForWeapon(agent, weaponComponentData3);
                if (weaponComponentData3.IsRangedWeapon)
                {
                    int thrustSpeed = weaponComponentData3.ThrustSpeed;
                    if (!agent.HasMount)
                    {
                        float num3 = MathF.Max(0f, 1f - (float)effectiveSkillForWeapon / 500f);
                        agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = 0.125f * num3;
                        agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = 0.1f * num3;
                    }
                    else
                    {
                        float num4 = MathF.Max(0f, (1f - (float)effectiveSkillForWeapon / 500f) * (1f - (float)effectiveSkill2 / 1800f));
                        agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = 0.025f * num4;
                        agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = 0.12f * num4;
                    }
                    agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = MathF.Max(0f, agentDrivenProperties.WeaponMaxMovementAccuracyPenalty);
                    agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = MathF.Max(0f, agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty);
                    if (weaponComponentData3.RelevantSkill == DefaultSkills.Bow)
                    {
                        float num5 = ((float)thrustSpeed - 45f) / 90f;
                        num5 = MBMath.ClampFloat(num5, 0f, 1f);
                        agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= 6f;
                        agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= 4.5f / MBMath.Lerp(0.75f, 2f, num5, 1E-05f);
                    }
                    else if (weaponComponentData3.RelevantSkill == DefaultSkills.Throwing)
                    {
                        float num6 = ((float)thrustSpeed - 89f) / 13f;
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
                        agentDrivenProperties.WeaponBestAccuracyWaitTime = 0.3f + (95.75f - (float)thrustSpeed) * 0.005f;
                        float num7 = ((float)thrustSpeed - 45f) / 90f;
                        num7 = MBMath.ClampFloat(num7, 0f, 1f);
                        agentDrivenProperties.WeaponUnsteadyBeginTime = 0.6f + (float)effectiveSkillForWeapon * 0.01f * MBMath.Lerp(2f, 4f, num7, 1E-05f);
                        if (agent.IsAIControlled)
                        {
                            agentDrivenProperties.WeaponUnsteadyBeginTime *= 4f;
                        }
                        agentDrivenProperties.WeaponUnsteadyEndTime = 2f + agentDrivenProperties.WeaponUnsteadyBeginTime;
                        agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.1f;
                    }
                    else if (weaponComponentData3.WeaponClass == WeaponClass.Javelin || weaponComponentData3.WeaponClass == WeaponClass.ThrowingAxe || weaponComponentData3.WeaponClass == WeaponClass.ThrowingKnife)
                    {
                        agentDrivenProperties.WeaponBestAccuracyWaitTime = 0.4f + (89f - (float)thrustSpeed) * 0.03f;
                        agentDrivenProperties.WeaponUnsteadyBeginTime = 2.5f + (float)effectiveSkillForWeapon * 0.01f;
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
                    agentDrivenProperties.WeaponUnsteadyBeginTime = 1f + (float)effectiveSkillForWeapon * 0.005f;
                    agentDrivenProperties.WeaponUnsteadyEndTime = 3f + (float)effectiveSkillForWeapon * 0.01f;
                }
                if (agent.HasMount)
                {
                    float num8 = 1f - MathF.Max(0f, 0.2f - (float)effectiveSkill2 * 0.002f);
                    agentDrivenProperties.SwingSpeedMultiplier *= num8;
                    agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier *= num8;
                    agentDrivenProperties.ReloadSpeed *= num8;
                }
            }
            agentDrivenProperties.TopSpeedReachDuration = 2f / MathF.Max((200f + (float)effectiveSkill) / 300f * ((float)weight / ((float)weight + num2)), 0.3f);
            float num9 = 0.7f + 0.00070000015f * (float)effectiveSkill;
            float num10 = MathF.Max(0.2f * (1f - (float)effectiveSkill * 0.001f), 0f) * num2 / (float)weight;
            float num11 = MBMath.ClampFloat(num9 - num10, 0f, 0.91f);
            agentDrivenProperties.MaxSpeedMultiplier = this.GetEnvironmentSpeedFactor(agent) * num11;
            float managedParameter = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalCombatSpeedMinMultiplier);
            float managedParameter2 = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalCombatSpeedMaxMultiplier);
            float num12 = MathF.Min(num2 / (float)weight, 1f);
            agentDrivenProperties.CombatMaxSpeedMultiplier = MathF.Min(MBMath.Lerp(managedParameter2, managedParameter, num12, 1E-05f), 1f);

            // Adjust speed depending on race. MaxSpeed is multiplied with native_parameters.xml/bipedal_speed_multiplier
            if (agent.Character.Race != 0 && agent.Character.Race == FaceGen.GetRaceOrDefault("troll"))
            {
                agentDrivenProperties.MaxSpeedMultiplier *= 1.7f;
            }

            agentDrivenProperties.AttributeShieldMissileCollisionBodySizeAdder = 0.3f;
            Agent mountAgent = agent.MountAgent;
            float num13 = ((mountAgent != null) ? mountAgent.GetAgentDrivenPropertyValue(DrivenProperty.AttributeRiding) : 1f);
            agentDrivenProperties.AttributeRiding = (float)effectiveSkill2 * num13;
            agentDrivenProperties.AttributeHorseArchery = Game.Current.BasicModels.StrikeMagnitudeModel.CalculateHorseArcheryFactor(character);
            agentDrivenProperties.BipedalRangedReadySpeedMultiplier = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRangedReadySpeedMultiplier);
            agentDrivenProperties.BipedalRangedReloadSpeedMultiplier = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRangedReloadSpeedMultiplier);
            this.GetBannerEffectsOnAgent(agent, agentDrivenProperties, weaponComponentData);
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
            float num = (float)(equipmentElement.GetModifiedMountSpeed(equipmentElement2) + 1);
            int modifiedMountManeuver = equipmentElement.GetModifiedMountManeuver(equipmentElement2);
            int num2 = 0;
            float environmentSpeedFactor = this.GetEnvironmentSpeedFactor(agent);
            if (agent.RiderAgent != null)
            {
                num2 = this.GetEffectiveSkill(agent.RiderAgent.Character, agent.RiderAgent.Origin, agent.RiderAgent.Formation, DefaultSkills.Riding);
                FactoredNumber factoredNumber = new FactoredNumber(num);
                FactoredNumber factoredNumber2 = new FactoredNumber((float)modifiedMountManeuver);
                factoredNumber.AddFactor((float)num2 * 0.001f);
                factoredNumber2.AddFactor((float)num2 * 0.0004f);
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
                agentDrivenProperties.MountManeuver = (float)modifiedMountManeuver;
                agentDrivenProperties.MountSpeed = environmentSpeedFactor * 0.22f * (1f + num);
            }
            float num3 = equipmentElement.Weight / 2f + (equipmentElement2.IsEmpty ? 0f : equipmentElement2.Weight);
            agentDrivenProperties.MountDashAccelerationMultiplier = ((num3 > 200f) ? ((num3 < 300f) ? (1f - (num3 - 200f) / 111f) : 0.1f) : 1f);
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
            SkillObject skill = ((equippedItem == null) ? DefaultSkills.Athletics : equippedItem.RelevantSkill);
            int effectiveSkill = GetEffectiveSkill(agent.Character, agent.Origin, agent.Formation, skill);
            float num = CalculateAILevel(agent, meleeSkill);
            float num2 = CalculateAILevel(agent, effectiveSkill);
            float num3 = num + agent.Defensiveness;
            agentDrivenProperties.AiRangedHorsebackMissileRange = 0.3f + 0.4f * num2;
            agentDrivenProperties.AiFacingMissileWatch = -0.96f + num * 0.06f;
            agentDrivenProperties.AiFlyingMissileCheckRadius = 8f - 6f * num;
            agentDrivenProperties.AiShootFreq = 0.3f + 0.7f * num2;
            agentDrivenProperties.AiWaitBeforeShootFactor = (agent.PropertyModifiers.resetAiWaitBeforeShootFactor ? 0f : (1f - 0.5f * num2));
            agentDrivenProperties.AIBlockOnDecideAbility = MBMath.Lerp(0.25f, 0.99f, MBMath.ClampFloat(num, 0f, 1f));
            agentDrivenProperties.AIParryOnDecideAbility = MBMath.Lerp(0.01f, 0.95f, MBMath.ClampFloat(MathF.Pow(num, 1.5f), 0f, 1f));
            agentDrivenProperties.AiTryChamberAttackOnDecide = (num - 0.15f) * 0.1f;
            agentDrivenProperties.AIAttackOnParryChance = 0.3f - 0.1f * agent.Defensiveness;
            agentDrivenProperties.AiAttackOnParryTiming = -0.2f + 0.3f * num;
            agentDrivenProperties.AIDecideOnAttackChance = 0.15f * agent.Defensiveness;
            agentDrivenProperties.AIParryOnAttackAbility = MBMath.ClampFloat(num * num * num, 0f, 1f);
            agentDrivenProperties.AiKick = -0.1f + ((num > 0.4f) ? 0.4f : num);
            agentDrivenProperties.AiAttackCalculationMaxTimeFactor = num;
            agentDrivenProperties.AiDecideOnAttackWhenReceiveHitTiming = -0.25f * (1f - num);
            agentDrivenProperties.AiDecideOnAttackContinueAction = -0.5f * (1f - num);
            agentDrivenProperties.AiDecideOnAttackingContinue = 0.1f * num;
            agentDrivenProperties.AIParryOnAttackingContinueAbility = MBMath.Lerp(0.05f, 0.95f, MBMath.ClampFloat(num * num * num, 0f, 1f));
            agentDrivenProperties.AIDecideOnRealizeEnemyBlockingAttackAbility = 0.5f * MBMath.ClampFloat(MathF.Pow(num, 2.5f) - 0.1f, 0f, 1f);
            agentDrivenProperties.AIRealizeBlockingFromIncorrectSideAbility = 0.5f * MBMath.ClampFloat(MathF.Pow(num, 2.5f) - 0.1f, 0f, 1f);
            agentDrivenProperties.AiAttackingShieldDefenseChance = 0.2f + 0.3f * num;
            agentDrivenProperties.AiAttackingShieldDefenseTimer = -0.3f + 0.3f * num;
            agentDrivenProperties.AiRandomizedDefendDirectionChance = 1f - MathF.Log(num * 7f + 1f, 2f) * 0.33333f;

            // Reduce AI shooter error
            //agentDrivenProperties.AiShooterError = 0.004f / Math.Max(0.4f, AgentsInfoModel.Instance.Agents[agent.Index].Difficulty);
            agentDrivenProperties.AiShooterError = 0.004f;

            agentDrivenProperties.AISetNoAttackTimerAfterBeingHitAbility = MBMath.ClampFloat(num * num, 0.05f, 0.95f);
            agentDrivenProperties.AISetNoAttackTimerAfterBeingParriedAbility = MBMath.ClampFloat(num * num, 0.05f, 0.95f);
            agentDrivenProperties.AISetNoDefendTimerAfterHittingAbility = MBMath.ClampFloat(num * num, 0.05f, 0.95f);
            agentDrivenProperties.AISetNoDefendTimerAfterParryingAbility = MBMath.ClampFloat(num * num, 0.05f, 0.95f);
            agentDrivenProperties.AIEstimateStunDurationPrecision = 1f - MBMath.ClampFloat(num * num, 0.05f, 0.95f);
            agentDrivenProperties.AIHoldingReadyMaxDuration = MBMath.Lerp(0.25f, 0f, MathF.Min(1f, num * 1.2f));
            agentDrivenProperties.AIHoldingReadyVariationPercentage = num;
            agentDrivenProperties.AiRaiseShieldDelayTimeBase = -0.75f + 0.5f * num;
            agentDrivenProperties.AiUseShieldAgainstEnemyMissileProbability = 0.1f + num * 0.6f + num3 * 0.2f;
            agentDrivenProperties.AiCheckMovementIntervalFactor = 0.005f * (1.1f - num);
            agentDrivenProperties.AiMovementDelayFactor = 4f / (3f + num2);
            agentDrivenProperties.AiParryDecisionChangeValue = 0.05f + 0.7f * num;
            agentDrivenProperties.AiDefendWithShieldDecisionChanceValue = MathF.Min(1f, 0.2f + 0.5f * num + 0.2f * num3);
            agentDrivenProperties.AiMoveEnemySideTimeValue = -2.5f + 0.5f * num;
            agentDrivenProperties.AiMinimumDistanceToContinueFactor = 2f + 0.3f * (3f - num);
            agentDrivenProperties.AiHearingDistanceFactor = 1f + num;
            agentDrivenProperties.AiChargeHorsebackTargetDistFactor = 1.5f * (3f - num);
            agentDrivenProperties.AiWaitBeforeShootFactor = (agent.PropertyModifiers.resetAiWaitBeforeShootFactor ? 0f : (1f - 0.5f * num2));

            // AI accuracy decrease
            float shootingErrorMultiplier = (1f - num2);
            agentDrivenProperties.AiRangerLeadErrorMin = (0f - shootingErrorMultiplier) * 0.35f;
            agentDrivenProperties.AiRangerLeadErrorMax = shootingErrorMultiplier * 0.2f;
            agentDrivenProperties.AiRangerVerticalErrorMultiplier = shootingErrorMultiplier * 0.1f;
            agentDrivenProperties.AiRangerHorizontalErrorMultiplier = shootingErrorMultiplier * ((float)Math.PI / 90f);

            agentDrivenProperties.AIAttackOnDecideChance = MathF.Clamp(0.23f * CalculateAIAttackOnDecideMaxValue() * (3f - agent.Defensiveness), 0.05f, 1f);
            agentDrivenProperties.SetStat(DrivenProperty.UseRealisticBlocking, (agent.Controller != Agent.ControllerType.Player) ? 1f : 0f);            
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
            agentDrivenProperties.AttributeRiding = 0.8f + ((spawnEquipment[EquipmentIndex.HorseHarness].Item != null) ? 0.2f : 0f);
            float num = 0f;
            for (int i = 1; i < 12; i++)
            {
                if (spawnEquipment[i].Item != null)
                {
                    num += (float)spawnEquipment[i].GetModifiedMountBodyArmor();
                }
            }
            agentDrivenProperties.ArmorTorso = num;
            ItemObject item = spawnEquipment[EquipmentIndex.ArmorItemEndSlot].Item;
            if (item != null)
            {
                HorseComponent horseComponent = item.HorseComponent;
                EquipmentElement equipmentElement = spawnEquipment[EquipmentIndex.ArmorItemEndSlot];
                EquipmentElement equipmentElement2 = spawnEquipment[EquipmentIndex.HorseHarness];
                agentDrivenProperties.MountChargeDamage = (float)equipmentElement.GetModifiedMountCharge(equipmentElement2) * 0.01f;
                agentDrivenProperties.MountDifficulty = (float)equipmentElement.Item.Difficulty;
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
            SkillObject skillObject = ((weapon != null) ? weapon.RelevantSkill : null);
            if (agentCharacter != null && skillObject != null)
            {
                if (skillObject == DefaultSkills.OneHanded)
                {
                    int skillValue = agentCharacter.GetSkillValue(skillObject);
                    num += (float)skillValue * 0.0015f;
                }
                else if (skillObject == DefaultSkills.TwoHanded)
                {
                    int skillValue2 = agentCharacter.GetSkillValue(skillObject);
                    num += (float)skillValue2 * 0.0016f;
                }
                else if (skillObject == DefaultSkills.Polearm)
                {
                    int skillValue3 = agentCharacter.GetSkillValue(skillObject);
                    num += (float)skillValue3 * 0.0007f;
                }
                else if (skillObject == DefaultSkills.Bow)
                {
                    int skillValue4 = agentCharacter.GetSkillValue(skillObject);
                    num += (float)skillValue4 * 0.0011f;
                }
                else if (skillObject == DefaultSkills.Throwing)
                {
                    int skillValue5 = agentCharacter.GetSkillValue(skillObject);
                    num += (float)skillValue5 * 0.0006f;
                }
            }
            return Math.Max(0f, num);
        }

        public override float GetKnockBackResistance(Agent agent)
        {
            BasicCharacterObject character;
            if (agent.IsHuman && (character = agent.Character) != null)
            {
                int effectiveSkill = this.GetEffectiveSkill(character, agent.Origin, agent.Formation, DefaultSkills.Athletics);
                float num = 0.15f + (float)effectiveSkill * 0.001f;
                return Math.Max(0f, num);
            }
            return float.MaxValue;
        }

        public override float GetDismountResistance(Agent agent)
        {
            BasicCharacterObject character;
            if (agent.IsHuman && (character = agent.Character) != null)
            {
                int effectiveSkill = this.GetEffectiveSkill(character, agent.Origin, agent.Formation, DefaultSkills.Riding);
                float num = 0.4f + (float)effectiveSkill * 0.001f;
                return Math.Max(0f, num);
            }
            return float.MaxValue;
        }

        public TestAgentStatCalculateModel()
        {
        }
    }
}
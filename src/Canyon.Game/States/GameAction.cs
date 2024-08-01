using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Ai;
using Canyon.Game.Sockets.Ai.Packets;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Events;
using Canyon.Game.States.Events.Elite;
using Canyon.Game.States.Events.Mount;
using Canyon.Game.States.Families;
using Canyon.Game.States.Items;
using Canyon.Game.States.Magics;
using Canyon.Game.States.MessageBoxes;
using Canyon.Game.States.NPCs;
using Canyon.Game.States.Syndicates;
using Canyon.Game.States.User;
using Canyon.Game.States.World;
using Canyon.Network.Packets.Ai;
using Canyon.Shared.Managers;
using Canyon.World.Enums;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Drawing;
using static Canyon.Game.Sockets.Game.Packets.MsgAction;
using static Canyon.Game.Sockets.Game.Packets.MsgName;
using static Canyon.Game.States.User.Character;

namespace Canyon.Game.States
{
    public class GameAction
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<GameAction>();
        private static readonly ILogger missingLogger = LogFactory.CreateLogger<GameAction>("missing_action_types");
        private static readonly ILogger missingAction = LogFactory.CreateLogger<GameAction>("missing_action");

        public static async Task<bool> ExecuteActionAsync(uint idAction, Character user, Role role, Item item,
            string input)
        {
            const int _MAX_ACTION_I = 64;
            const int _DEADLOCK_CHECK_I = 5;

            if (idAction == 0)
            {
                return false;
            }

            int actionCount = 0;
            int deadLookCount = 0;
            uint idNext = idAction, idOld = idAction;

            while (idNext > 0)
            {
                if (actionCount++ > _MAX_ACTION_I)
                {
                    logger.LogError($"Error: too many game action, from: {idAction}, last action: {idNext}");
                    return false;
                }

                if (idAction == idOld && deadLookCount++ >= _DEADLOCK_CHECK_I)
                {
                    logger.LogError($"Error: dead loop detected, from: {idAction}, last action: {idNext}");
                    return false;
                }

                if (idNext != idOld)
                {
                    deadLookCount = 0;
                }

                DbAction action = EventManager.GetAction(idNext);
                if (action == null)
                {
                    missingAction.LogError($"Action[{idNext}],{FormatLogString(action, null, user, role, item, input)}");
                    return false;
                }

                string param = await FormatParamAsync(action, user, role, item, input);

                if (user?.IsPm() == true)
                {
                    await user.SendAsync($"{action.Id}: [{action.IdNext},{action.IdNextfail}]. type[{action.Type}], data[{action.Data}], param:[{param}].",
                        TalkChannel.Action,
                        Color.White);
                }

                bool result = false;
                switch ((TaskActionType)action.Type)
                {
                    #region Action

                    case TaskActionType.ActionMenutext:
                        result = await ExecuteActionMenuTextAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMenulink:
                        result = await ExecuteActionMenuLinkAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMenuedit:
                        result = await ExecuteActionMenuEditAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMenupic:
                        result = await ExecuteActionMenuPicAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMenuMessage:
                        result = await ExecuteActionMenuMessageAsync(action, param, user, role, item, input);
                        break;
                    case (TaskActionType)113: // TODO find out what this has to do
                        result = true;
                        break;
                    case TaskActionType.ActionMenucreate:
                        result = await ExecuteActionMenuCreateAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionRand:
                        result = await ExecuteActionMenuRandAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionRandaction:
                        result = await ExecuteActionMenuRandActionAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionChktime:
                        result = await ExecuteActionMenuChkTimeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionPostcmd:
                        result = await ExecuteActionPostcmdAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionBrocastmsg:
                        result = await ExecuteActionBrocastmsgAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSysExecAction:
                        result = await ExecuteActionSysExecActionAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionExecutequery:
                        result = await ExecuteActionExecutequeryAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSysDoSomethingUnknown:
                        result = true;
                        break;
                    case TaskActionType.ActionSysInviteFilter:
                        result = await ExecuteActionSysInviteFilterAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSysInviteTrans:
                        result = await ExecuteActionInviteTransAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSysPathFinding:
                        result = await ExecuteActionSysPathFindingAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionVipFunctionCheck:
                        result = await ExecuteActionVipFunctionCheckAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionDynaGlobalData:
                        result = await ExecuteActionDynaGlobalDataAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region Npc

                    case TaskActionType.ActionNpcAttr:
                        result = await ExecuteActionNpcAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcErase:
                        result = await ExecuteActionNpcEraseAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcResetsynowner:
                        result = await ExecuteActionNpcResetsynownerAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcFindNextTable:
                        result = await ExecuteActionNpcFindNextTableAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcFamilyCreate:
                        result = await ExecuteActionNpcFamilyCreateAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcFamilyDestroy:
                        result = await ExecuteActionNpcFamilyDestroyAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcFamilyChangeName:
                        result = await ExecuteActionNpcFamilyChangeNameAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcChangePos:
                        result = true;
                        break;

                    #endregion

                    #region Map

                    case TaskActionType.ActionMapMovenpc:
                        result = await ExecuteActionMapMovenpcAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapMapuser:
                        result = await ExecuteActionMapMapuserAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapBrocastmsg:
                        result = await ExecuteActionMapBrocastmsgAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapDropitem:
                        result = await ExecuteActionMapDropitemAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapSetstatus:
                        result = await ExecuteActionMapSetstatusAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapAttrib:
                        result = await ExecuteActionMapAttribAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapRegionMonster:
                        result = await ExecuteActionMapRegionMonsterAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapDropMultiItems:
                        result = await ExecuteActionMapRandDropItemAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapChangeweather:
                        result = await ExecuteActionMapChangeweatherAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapChangelight:
                        result = await ExecuteActionMapChangelightAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapMapeffect:
                        result = await ExecuteActionMapMapeffectAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapFireworks:
                        result = await ExecuteActionMapFireworksAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapAbleExp:
                        result = await ExecuteActionMapAbleExpAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region Lay Item

                    case TaskActionType.ActionItemRequestlaynpc:
                        result = await ExecuteActionItemRequestlaynpcAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemCountnpc:
                        result = await ExecuteActionItemCountnpcAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemLaynpc:
                        result = await ExecuteActionItemLaynpcAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemDelthis:
                        result = await ExecuteActionItemDelthisAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region Item

                    case TaskActionType.ActionItemAdd:
                    case TaskActionType.ActionItemAdd2:
                        result = await ExecuteActionItemAddAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemDel:
                    case TaskActionType.ActionItemCheck2:
                        result = await ExecuteActionItemDelAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemCheck:
                        result = await ExecuteActionItemCheckAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemHole:
                        result = await ExecuteActionItemHoleAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemMultidel:
                        result = await ExecuteActionItemMultidelAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemMultichk:
                        result = await ExecuteActionItemMultichkAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemLeavespace:
                        result = await ExecuteActionItemLeavespaceAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemUpequipment:
                        result = await ExecuteActionItemUpequipmentAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemEquiptest:
                        result = await ExecuteActionItemEquiptestAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemEquipexist:
                        result = await ExecuteActionItemEquipexistAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemEquipcolor:
                        result = await ExecuteActionItemEquipcolorAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemTransform:
                        result = await ExecuteActionItemTransformAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemCheckrand:
                        result = await ExecuteActionItemCheckrandAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemModify:
                        result = await ExecuteActionItemModifyAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemDelAll:
                        result = await ExecuteActionItemDelAllAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemJarCreate:
                        result = await ExecuteActionItemJarCreateAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemJarVerify:
                        result = await ExecuteActionItemJarVerifyAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemRefineryAdd:
                        result = await ExecuteActionItemRefineryAddAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemSuperFlag:
                        result = await ExecuteActionItemSuperFlagAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemWeaponRChangeSubtype:
                        result = await ExecuteActionItemWeaponRChangeSubtypeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemAddSpecial:
                        result = await ExecuteActionItemAddSpecialAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region Syndicate

                    case TaskActionType.ActionSynCreate:
                        result = await ExecuteActionSynCreateAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynDestroy:
                        result = await ExecuteActionSynDestroyAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynSetAssistant:
                        result = await ExecuteActionSynSetAssistantAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynClearRank:
                        result = await ExecuteActionSynClearRankAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynChangeLeader:
                        result = await ExecuteActionSynChangeLeaderAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynAntagonize:
                        result = await ExecuteActionSynAntagonizeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynClearAntagonize:
                        result = await ExecuteActionSynClearAntagonizeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynAlly:
                        result = await ExecuteActionSynAllyAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynClearAlly:
                        result = await ExecuteActionSynClearAllyAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynAttr:
                        result = await ExecuteActionSynAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynChangeName:
                        result = await ExecuteActionSynChangeNameAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region Monster Item

                    case TaskActionType.ActionMstDropitem:
                        result = await ExecuteActionMstDropitemAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionMstTeamReward:
                        result = await ExecuteActionMstTeamRewardAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region User

                    case TaskActionType.ActionUserAttr:
                        result = await ExecuteUserAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserFull:
                        result = await ExecuteUserFullAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserChgmap:
                        result = await ExecuteUserChgMapAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserRecordpoint:
                        result = await ExecuteUserRecordpointAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserHair:
                        result = await ExecuteUserHairAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserChgmaprecord:
                        result = await ExecuteUserChgmaprecordAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserChglinkmap:
                        result = await ExecuteActionUserChglinkmapAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTransform:
                        result = await ExecuteUserTransformAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserIspure:
                        result = await ExecuteActionUserIspureAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTalk:
                        result = await ExecuteActionUserTalkAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserMagicEffect:
                        result = await ExecuteActionUserMagicEffectAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserMagic:
                        result = await ExecuteActionUserMagicAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserWeaponskill:
                        result = await ExecuteActionUserWeaponSkillAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserLog:
                        result = await ExecuteActionUserLogAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserBonus:
                        result = await ExecuteActionUserBonusAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserDivorce:
                        result = await ExecuteActionUserDivorceAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserMarriage:
                        result = await ExecuteActionUserMarriageAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserSex:
                        result = await ExecuteActionUserSexAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserEffect:
                        result = await ExecuteActionUserEffectAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskmask:
                        result = await ExecuteActionUserTaskmaskAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserMediaplay:
                        result = await ExecuteActionUserMediaplayAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserCreatemap:
                        result = await ExecuteActionUserCreatemapAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserEnterHome:
                        result = await ExecuteActionUserEnterHomeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserEnterMateHome:
                        result = await ExecuteActionUserEnterMateHomeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserUnlearnMagic:
                        result = await ExecuteActionUserUnlearnMagicAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserRebirth:
                        result = await ExecuteActionUserRebirthAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserWebpage:
                        result = await ExecuteActionUserWebpageAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserBbs:
                        result = await ExecuteActionUserBbsAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserUnlearnSkill:
                        result = await ExecuteActionUserUnlearnSkillAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserDropMagic:
                        result = await ExecuteActionUserDropMagicAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserOpenDialog:
                        result = await ExecuteActionUserOpenDialogAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserPointAllot:
                        result = await ExecuteActionUserFixAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserPlusExp:
                        result = await ExecuteActionUserExpMultiplyAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserWhPassword:
                        result = await ExecuteActionUserWhPasswordAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserSetWhPassword:
                        result = await ExecuteActionUserSetWhPasswordAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserOpeninterface:
                        result = await ExecuteActionUserOpeninterfaceAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskManager:
                        result = await ExecuteActionUserTaskManagerAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskOpe:
                        result = await ExecuteActionUserTaskOpeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskLocaltime:
                        result = await ExecuteActionUserTaskLocaltimeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskFind:
                        result = await ExecuteActionUserTaskFindAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserVarCompare:
                        result = await ExecuteActionUserVarCompareAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserVarDefine:
                        result = await ExecuteActionUserVarDefineAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserVarCompareString:
                        result = await ExecuteActionUserVarCompareStringAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserVarDefineString:
                        result = await ExecuteActionUserVarDefineStringAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserVarCalc:
                        result = await ExecuteActionUserVarCalcAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTestEquipment:
                        result = await ExecuteActionUserTestEquipmentAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserDailyStcCompare:
                        result = await ExecuteActionUserDailyStcCompareAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserDailyStcOpe:
                        result = await ExecuteActionUserDailyStcOpeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserExecAction:
                        result = await ExecuteActionUserExecActionAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTestPos:
                        result = true;
                        break; // gotta investigate
                    case TaskActionType.ActionUserStcCompare:
                        result = await ExecuteActionUserStcCompareAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserStcOpe:
                        result = await ExecuteActionUserStcOpeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserDataSync:
                        result = await ExecuteActionUserDataSyncAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserSelectToData:
                        result = await ExecuteActionUserSelectToDataAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserStcTimeCheck:
                        result = await ExecuteActionUserStcTimeCheckAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserStcTimeOperation:
                        result = await ExecuteActionUserStcTimeOpeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserAttachStatus:
                        result = await ExecuteActionUserAttachStatusAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserGodTime:
                        result = await ExecuteActionUserGodTimeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserCalExp:
                        result = await ExecuteActionUserCalExpAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserLogEvent:
                        result = await ExecuteActionUserLogAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTimeToExp:
                        result = await ExecuteActionUserExpballExpAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserPureProfessional:
                        result = await ExecuteActionUserPureProfessionalAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSomethingRelatedToRebirth:
                        result = true;
                        break;
                    case TaskActionType.ActionUserStatusCreate:
                        result = await ExecuteActionUserStatusCreateAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserStatusCheck:
                        result = await ExecuteActionUserStatusCheckAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region Team

                    case TaskActionType.ActionTeamBroadcast:
                        result = await ExecuteActionTeamBroadcastAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamAttr:
                        result = await ExecuteActionTeamAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamLeavespace:
                        result = await ExecuteActionTeamLeavespaceAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamItemAdd:
                        result = await ExecuteActionTeamItemAddAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamItemDel:
                        result = await ExecuteActionTeamItemDelAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamItemCheck:
                        result = await ExecuteActionTeamItemCheckAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamChgmap:
                        result = await ExecuteActionTeamChgmapAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamChkIsleader:
                        result = await ExecuteActionTeamChkIsleaderAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamCreateDynamap:
                        result = await ExecuteActionTeamCreateDynamapAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region General User

                    case TaskActionType.ActionFrozenGrottoEntranceChkDays:
                        result = await ExecuteActionFrozenGrottoEntranceChkDaysAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserCheckHpFull:
                        result = await ExecuteActionUserCheckHpFullAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserCheckHpManaFull:
                        result = await ExecuteActionUserCheckHpManaFullAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionGeneralLottery:
                        result = await ExecuteGeneralLotteryAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserRandTrans:
                        result = await ExecuteActionChgMapSquareAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserDecLife:
                        result = await ExecuteActionUserDecLifeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionOpenShop:
                        result = await ExecuteActionOpenShopAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionSubclassLearn:
                        result = await ExecuteActionSubclassLearnAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSubclassPromotion:
                        result = await ExecuteActionSubclassPromotionAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSubclassLevel:
                        result = await ExecuteActionSubclassLevelAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionAchievements:
                        result = await ExecuteActionAchievementsAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionMouseWaitClick:
                        result = await ExecuteActionMouseWaitClickAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMouseJudgeType:
                        result = await ExecuteActionMouseJudgeTypeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMouseClearStatus:
                        result = await ExecuteActionMouseClearStatusAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMouseDeleteChosen:
                        result = await ExecuteActionMouseDeleteChosenAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionAttachBuffStatus:
                        result = await ExecuteActionAttachBuffStatusAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionDetachBuffStatuses:
                        result = await ExecuteActionDetachBuffStatusesAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserReturn:
                        result = await ExecuteActionUserReturnAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionAutoHuntIsActive:
                        result = await ExecuteActionAutoHuntIsActiveAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionCheckUserAttributeLimit:
                        result = await ExecuteActionCheckUserAttributeLimitAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionAddProcessActivityTask:
                        result = await ExecuteActionAddProcessActivityTaskAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionAddProcessTaskSchedle:
                        result = await ExecuteActionAddProcessTaskSchedleAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region Jiang Hu

                    case TaskActionType.ActionJiangHuAttributes:
                        result = await ExecuteActionJiangHuAttributesAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionJiangHuInscribed:
                        result = await ExecuteActionJiangHuInscribedAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionJiangHuLevel:
                        result = await ExecuteActionJiangHuLevelAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionJiangHuExpProtection:
                        result = await ExecuteActionJiangHuExpProtectionAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region Event

                    case TaskActionType.ActionEventSetstatus:
                        result = await ExecuteActionEventSetstatusAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventDelnpcGenid:
                        result = await ExecuteActionEventDelnpcGenidAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCompare:
                        result = await ExecuteActionEventCompareAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCompareUnsigned:
                        result = await ExecuteActionEventCompareUnsignedAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventChangeweather:
                        result = await ExecuteActionEventChangeweatherAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCreatepet:
                        result = await ExecuteActionEventCreatepetAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCreatenewNpc:
                        result = await ExecuteActionEventCreatenewNpcAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCountmonster:
                        result = await ExecuteActionEventCountmonsterAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventDeletemonster:
                        result = await ExecuteActionEventDeletemonsterAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventBbs:
                        result = await ExecuteActionEventBbsAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventErase:
                        result = await ExecuteActionEventEraseAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventMapUserChgMap:
                        result = await ExecuteActionEventTeleportAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventMapUserExeAction:
                        result = await ExecuteActionEventMassactionAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionEventRegister:
                        result = await ExecuteActionEventRegisterAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventExit:
                        result = await ExecuteActionEventExitAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCmd:
                        result = await ExecuteActionEventCmdAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region Trap

                    case TaskActionType.ActionTrapCreate:
                        result = await ExecuteActionTrapCreateAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTrapErase:
                        result = await ExecuteActionTrapEraseAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTrapCount:
                        result = await ExecuteActionTrapCountAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTrapAttr:
                        result = await ExecuteActionTrapAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTrapTypeDelete:
                        result = await ExecuteActionTrapTypeDeleteAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region Hunter

                    case TaskActionType.ActionDetainDialog:
                        result = await ExecuteActionDetainDialogAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region Family War
                    case TaskActionType.ActionFamilyAttr:
                        result = await ExecuteActionFamilyAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionFamilyMemberAttr:
                        result = await ExecuteActionFamilyMemberAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionFamilyWarActivityCheck:
                        result = await ExecuteActionFamilyWarActivityCheckAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionFamilyWarAuthorityCheck:
                        result = await ExecuteActionFamilyWarAuthorityCheckAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionFamilyWarRegisterCheck:
                        result = await ActionFamilyWarRegisterCheckAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region Horse Racing 3600-3699

                    case TaskActionType.ActionMountRacingEventReset:
                        result = await ExecuteActionMountRacingEventResetAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    #region Event Progress Bar

                    case TaskActionType.ActionProgressBar:
                        await ExecuteActionProgressBarAsync(action, param, user, role, item, input);
                        return true;

                    #endregion

                    #region Capture The Flag

                    case TaskActionType.ActionCaptureTheFlagCheck:
                        result = await ExecuteActionCaptureTheFlagCheckAsync(action, user, role, item, input);
                        break;

                    case TaskActionType.ActionCaptureTheFlagExit:
                        result = await ExecuteActionCaptureTheFlagExitAsync(action, user, role, item, input);
                        break;

                    #endregion

                    #region Lua

                    case TaskActionType.ActionLuaLinkMain:
                        result = await ExecuteActionLuaLinkMainAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionLuaExecute:
                        result = await ExecuteActionLuaExecuteAsync(action, param, user, role, item, input);
                        break;

                    #endregion

                    default:
                        {
                            missingLogger.LogWarning(FormatLogString(action, param, user, role, item, input));
                            break;
                        }
                }

                idOld = idAction;
                idNext = result ? action.IdNext : action.IdNextfail;
            }

            return true;
        }

        #region Action 100-199

        private static async Task<bool> ExecuteActionMenuTextAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
            {
                logger.LogWarning($"Action[{action.Id}] type 101 non character");
                return false;
            }

            int messages = (int) Math.Ceiling(param.Length / (double)byte.MaxValue);
            for (int i = 0; i < messages; i++)
            {
                await user.SendAsync(new MsgTaskDialog
                {
                    InteractionType = MsgTaskDialog.TaskInteraction.Dialog,
                    Text = param.Substring(i * byte.MaxValue, Math.Min(byte.MaxValue, param.Length - byte.MaxValue * i)),
                    Data = (ushort)action.Data
                });
            }
            return true;
        }

        private static async Task<bool> ExecuteActionMenuLinkAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
            {
                logger.LogWarning($"Action[{action.Id}] type 101 not user");
                return false;
            }

            uint task = 0;
            int align = 0;
            string[] parsed = param.Split(' ');
            if (parsed.Length > 1)
            {
                uint.TryParse(parsed[1], out task);
            }

            if (parsed.Length > 2)
            {
                int.TryParse(parsed[2], out align);
            }

            await user.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog.TaskInteraction.Option,
                Text = parsed[0],
                OptionIndex = user.PushTaskId(task.ToString()),
                Data = (ushort)align
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMenuEditAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] paramStrings = SplitParam(param, 3);
            if (paramStrings.Length < 3)
            {
                logger.LogWarning($"Invalid input param length for {action.Id}, param: {param}");
                return false;
            }

            await user.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog.TaskInteraction.Input,
                OptionIndex = user.PushTaskId(paramStrings[1]),
                Data = ushort.Parse(paramStrings[0]),
                Text = paramStrings[2]
            });

            return true;
        }

        private static async Task<bool> ExecuteActionMenuPicAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParam = SplitParam(param);

            ushort x = ushort.Parse(splitParam[0]);
            ushort y = ushort.Parse(splitParam[1]);

            await user.SendAsync(new MsgTaskDialog
            {
                TaskIdentity = (uint)((x << 16) | y),
                InteractionType = MsgTaskDialog.TaskInteraction.Avatar,
                Data = ushort.Parse(splitParam[2])
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMenuMessageAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            await user.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog.TaskInteraction.MessageBox,
                Text = param,
                OptionIndex = user.PushTaskId(action.Data.ToString())
            });

            return true;
        }

        private static async Task<bool> ExecuteActionMenuCreateAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            await user.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog.TaskInteraction.Finish
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMenuRandAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            string[] paramSplit = SplitParam(param, 2);

            int x = int.Parse(paramSplit[0]);
            int y = int.Parse(paramSplit[1]);
            double chance = 0.01;
            if (x > y)
            {
                chance = 99;
            }
            else
            {
                chance = x / (double)y;
                chance *= 100;
            }

            return await ChanceCalcAsync(chance);
        }

        private static async Task<bool> ExecuteActionMenuRandActionAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] paramSplit = SplitParam(param);
            if (paramSplit.Length == 0)
            {
                return false;
            }

            uint taskId = uint.Parse(paramSplit[await NextAsync(0, paramSplit.Length) % paramSplit.Length]);
            await ExecuteActionAsync(taskId, user, role, item, input);
            return true;
        }

        private static async Task<bool> ExecuteActionMenuChkTimeAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] paramSplit = SplitParam(param);

            DateTime actual = DateTime.Now;
            var nCurWeekDay = (int)actual.DayOfWeek;
            int nCurHour = actual.Hour;
            int nCurMinute = actual.Minute;

            switch (action.Data)
            {
                #region Complete date (yyyy-mm-dd hh:mm yyyy-mm-dd hh:mm)

                case 0:
                    {
                        if (paramSplit.Length < 4)
                        {
                            return false;
                        }

                        string[] time0 = paramSplit[1].Split(':');
                        string[] date0 = paramSplit[0].Split('-');
                        string[] time1 = paramSplit[3].Split(':');
                        string[] date1 = paramSplit[2].Split('-');

                        var dTime0 = new DateTime(int.Parse(date0[0]), int.Parse(date0[1]), int.Parse(date0[2]),
                            int.Parse(time0[0]), int.Parse(time0[1]), 0);
                        var dTime1 = new DateTime(int.Parse(date1[0]), int.Parse(date1[1]), int.Parse(date1[2]),
                            int.Parse(time1[0]), int.Parse(time1[1]), 59);

                        return dTime0 <= actual && dTime1 >= actual;
                    }

                #endregion

                #region On Year date (mm-dd hh:mm mm-dd hh:mm)

                case 1:
                    {
                        if (paramSplit.Length < 4)
                        {
                            return false;
                        }

                        string[] time0 = paramSplit[1].Split(':');
                        string[] date0 = paramSplit[0].Split('-');
                        string[] time1 = paramSplit[3].Split(':');
                        string[] date1 = paramSplit[2].Split('-');

                        var dTime0 = new DateTime(DateTime.Now.Year, int.Parse(date0[1]), int.Parse(date0[2]),
                            int.Parse(time0[0]), int.Parse(time0[1]), 0);
                        var dTime1 = new DateTime(DateTime.Now.Year, int.Parse(date1[1]), int.Parse(date1[2]),
                            int.Parse(time1[0]), int.Parse(time1[1]), 59);

                        return dTime0 <= actual && dTime1 >= actual;
                    }

                #endregion

                #region Day of the month (dd hh:mm dd hh:mm)

                case 2:
                    {
                        if (paramSplit.Length < 4)
                        {
                            return false;
                        }

                        string[] time0 = paramSplit[1].Split(':');
                        string date0 = paramSplit[0];
                        string[] time1 = paramSplit[3].Split(':');
                        string date1 = paramSplit[2];

                        var dTime0 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, int.Parse(date0),
                            int.Parse(time0[0]), int.Parse(time0[1]), 0);
                        var dTime1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, int.Parse(date1),
                            int.Parse(time1[0]), int.Parse(time1[1]), 59);

                        return dTime0 <= actual && dTime1 >= actual;
                    }

                #endregion

                #region Day of the week (dw hh:mm dw hh:mm)

                case 3:
                    {
                        if (paramSplit.Length < 4)
                        {
                            return false;
                        }

                        string[] time0 = paramSplit[1].Split(':');
                        string[] time1 = paramSplit[3].Split(':');

                        int nDay0 = int.Parse(paramSplit[0]);
                        int nDay1 = int.Parse(paramSplit[2]);
                        int nHour0 = int.Parse(time0[0]);
                        int nHour1 = int.Parse(time1[0]);
                        int nMinute0 = int.Parse(time0[1]);
                        int nMinute1 = int.Parse(time1[1]);

                        int timeNow = nCurWeekDay * 24 * 60 + nCurHour * 60 + nCurMinute;
                        int from = nDay0 * 24 * 60 + nHour0 * 60 + nMinute0;
                        int to = nDay1 * 24 * 60 + nHour1 * 60 + nMinute1;

                        return timeNow >= from && timeNow <= to;
                    }

                #endregion

                #region Hour check (hh:mm hh:mm)

                case 4:
                    {
                        if (paramSplit.Length < 2)
                        {
                            return false;
                        }

                        string[] time0 = paramSplit[0].Split(':');
                        string[] time1 = paramSplit[1].Split(':');

                        int nHour0 = int.Parse(time0[0]);
                        int nHour1 = int.Parse(time1[0]);
                        int nMinute0 = int.Parse(time0[1]);
                        int nMinute1 = int.Parse(time1[1]);

                        int timeNow = nCurHour * 60 + nCurMinute;
                        int from = nHour0 * 60 + nMinute0;
                        int to = nHour1 * 60 + nMinute1;

                        return timeNow >= from && timeNow <= to;
                    }

                #endregion

                #region Minute check (mm mm)

                case 5:
                    {
                        if (paramSplit.Length < 2)
                        {
                            return false;
                        }

                        return nCurMinute >= int.Parse(paramSplit[0]) && nCurMinute <= int.Parse(paramSplit[1]);
                    }

                    #endregion
            }

            return false;
        }

        private static async Task<bool> ExecuteActionPostcmdAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            await user.SendAsync(new MsgAction
            {
                Identity = user.Identity,
                Command = action.Data,
                Action = ActionType.ClientCommand,
                ArgumentX = user.X,
                ArgumentY = user.Y
            });
            return true;
        }

        private static async Task<bool> ExecuteActionBrocastmsgAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            await BroadcastWorldMsgAsync(param, (TalkChannel)action.Data, Color.White);
            return true;
        }

        private static async Task<bool> ExecuteActionSysExecActionAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParams = SplitParam(param, 3);
            if (splitParams.Length < 3)
            {
                return false;
            }

            if (!int.TryParse(splitParams[0], out int secSpan)
                || !uint.TryParse(splitParams[1], out uint idAction))
            {
                return false;
            }

            EventManager.QueueAction(new QueuedAction(secSpan, idAction, 0));
            return true;
        }

        private static async Task<bool> ExecuteActionExecutequeryAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            try
            {
                if (param.Trim().StartsWith("SELECT", StringComparison.InvariantCultureIgnoreCase) ||
                    param.Trim().StartsWith("UPDATE", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!param.Contains("WHERE") || !param.Contains("LIMIT"))
                    {
                        logger.LogCritical($"ExecuteActionExecutequery {action.Id} doesn't have WHERE or LIMIT clause [{param}]");
                        return false;
                    }
                }

                await new ServerDbContext().Database.ExecuteSqlRawAsync(param);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Could not execute query action", ex.Message);
                return false;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionSysInviteFilterAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (action.Data == 0) // create invitation list
            {
                string[] splitParam = SplitParam(param);
                if (splitParam.Length < 4 || splitParam.Length % 3 != 1)
                {
                    logger.LogWarning($"Invalid param count for ExecuteActionSysInviteFilterAsync({param}): eventId (attr opt value)[]");
                    return true;
                }

                if (!int.TryParse(splitParam[0], out int idEvent))
                {
                    logger.LogWarning($"Invalid idEvent for ExecuteActionSysInviteFilterAsync: {param}");
                    return true;
                }

                var players = RoleManager.QueryUserSet().AsEnumerable();
                for (int i = 1; i < splitParam.Length; i += 3)
                {
                    string attr = splitParam[i];
                    string opt = splitParam[i + 1];
                    string valueString = splitParam[i + 2];
                    int.TryParse(valueString, out var value);

                    if (attr.Equals("level"))
                    {
                        if (opt.Equals("=="))
                        {
                            players = players.Where(x => x.Level == value);
                        }
                        else if (opt.Equals("<="))
                        {
                            players = players.Where(x => x.Level <= value);
                        }
                        else if (opt.Equals(">="))
                        {
                            players = players.Where(x => x.Level >= value);
                        }
                    }
                    else if (attr.Equals("profession"))
                    {
                        if (opt.Equals("=="))
                        {
                            players = players.Where(x => x.Profession == value);
                        }
                        else if (opt.Equals("<="))
                        {
                            players = players.Where(x => x.Profession <= value);
                        }
                        else if (opt.Equals(">="))
                        {
                            players = players.Where(x => x.Profession >= value);
                        }
                    }
                    else if (attr.Equals("rankshow"))
                    {
                        if (opt.Equals("=="))
                        {
                            players = players.Where(x => (int)x.SyndicateRank == value);
                        }
                        else if (opt.Equals("<="))
                        {
                            players = players.Where(x => (int)x.SyndicateRank <= value);
                        }
                        else if (opt.Equals(">="))
                        {
                            players = players.Where(x => (int)x.SyndicateRank >= value);
                        }
                    }
                    else if (attr.Equals("metempsychosis"))
                    {
                        if (opt.Equals("=="))
                        {
                            players = players.Where(x => x.Metempsychosis == value);
                        }
                        else if (opt.Equals("<="))
                        {
                            players = players.Where(x => x.Metempsychosis <= value);
                        }
                        else if (opt.Equals(">="))
                        {
                            players = players.Where(x => x.Metempsychosis >= value);
                        }
                    }
                    else if (attr.Equals("battlelev"))
                    {
                        if (opt.Equals("=="))
                        {
                            players = players.Where(x => x.BattlePower == value);
                        }
                        else if (opt.Equals("<="))
                        {
                            players = players.Where(x => x.BattlePower <= value);
                        }
                        else if (opt.Equals(">="))
                        {
                            players = players.Where(x => x.BattlePower >= value);
                        }
                    }
                }

                foreach (var player in players)
                {
                    EventManager.AddToInvitationList(idEvent, player.Identity);
                }
                return true;
            }
            else if (action.Data == 1) // clear invitation list
            {
                if (int.TryParse(param, out var value))
                {
                    EventManager.ClearInvitationList(value);
                }
                return true;
            }
            return false;
        }

        private static async Task<bool> ExecuteActionInviteTransAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] split = param.Trim().Split(' ');
            if (split.Length != 21)
            {
                logger.LogWarning("ActionInviteTrans must have 21 parameters. mapid 8pos msg acceptmsg type seconds");
                return false;
            }

            uint idMap = uint.Parse(split[0]);
            ushort[] px = new ushort[8];
            ushort[] py = new ushort[8];
            for (int i = 0; i < 8; i++)
            {
                px[i] = ushort.Parse(split[i * 2 + 1]);
                py[i] = ushort.Parse(split[i * 2 + 2]);
            }
            int sendMsg = int.Parse(split[17]);
            int acceptMsg = int.Parse(split[18]);
            int eventId = int.Parse(split[19]);
            int seconds = int.Parse(split[20]);

            var invitatedPlayersIds = EventManager.QueryInvitationList(eventId);
            foreach (var idUser in invitatedPlayersIds)
            {
                try
                {
                    var target = RoleManager.GetUser(idUser);
                    if (target?.Map == null)
                    {
                        continue;
                    }

                    if (target.Map.IsPrisionMap())
                    {
                        continue;
                    }

                    if (target.Map.IsTeleportDisable())
                    {
                        continue;
                    }

                    if (target.Map.IsArenicMapInGeneral())
                    {
                        continue;
                    }

                    TimedMessageBox timedMessageBox = new(target, seconds);
                    timedMessageBox.MessageId = sendMsg;
                    timedMessageBox.AcceptMsgId = acceptMsg;
                    timedMessageBox.InviteId = eventId;
                    timedMessageBox.TargetMapIdentity = idMap;
                    timedMessageBox.TargetMapX = px;
                    timedMessageBox.TargetMapY = py;
                    target.MessageBox = timedMessageBox;
                    await timedMessageBox.SendAsync();
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Failed to send invitation for player [{playerId}]. Error: {message}", idUser, ex.Message);
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionSysPathFindingAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] @params = param.Split(' ');
            ushort x = ushort.Parse(@params[0]);
            ushort y = ushort.Parse(@params[1]);
            uint npc = uint.Parse(@params[2]);
            uint idMap = user.MapIdentity;

            if (@params.Length > 3)
            {
                idMap = uint.Parse(@params[3]);
            }

            var msg = new MsgAction();
            msg.Action = ActionType.PathFinding;
            msg.Identity = user.Identity;
            msg.Timestamp = npc;
            msg.Command = idMap;
            msg.X = x;
            msg.Y = y;
            await user.SendAsync(msg);
            return true;
        }

        private static async Task<bool> ExecuteActionVipFunctionCheckAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }
            return user.UserVipFlag.HasFlag((VipFlags)action.Data);
        }

        private static async Task<bool> ExecuteActionDynaGlobalDataAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            string[] splitParams = SplitParam(param, 4);
            if (splitParams.Length < 3)
            {
                logger.LogWarning("Invalid number of params for action {action}", action.Id);
                return false;
            }

            uint dataId = uint.Parse(splitParams[0]);
            string type = splitParams[1];
            string opt = splitParams[2];
            string strValue = string.Empty;// = splitParams[3];
            if (splitParams.Length > 3)
            {
                strValue = splitParams[3];
            }
            long.TryParse(strValue, out var value);
            long cmpValue = 0;
            string cmpStrValue = string.Empty;

            DbDynaGlobalData data = await DynamicGlobalDataManager.GetAsync(action.Data);
            if (type.Equals("data"))
            {
                cmpValue = DynamicGlobalDataManager.GetData(data, (int)dataId);
                if (opt.Equals("set"))
                {
                    DynamicGlobalDataManager.ChangeData(data, (int)dataId, value);
                    return await DynamicGlobalDataManager.SaveAsync(data);
                }
                else if (opt.Equals("+="))
                {
                    DynamicGlobalDataManager.ChangeData(data, (int)dataId, cmpValue + value);
                    return await DynamicGlobalDataManager.SaveAsync(data);
                }
                else if (opt.Equals("resetall"))
                {
                    DynamicGlobalDataManager.ChangeData(data, 0, 0);
                    DynamicGlobalDataManager.ChangeData(data, 1, 0);
                    DynamicGlobalDataManager.ChangeData(data, 2, 0);
                    DynamicGlobalDataManager.ChangeData(data, 3, 0);
                    DynamicGlobalDataManager.ChangeData(data, 4, 0);
                    DynamicGlobalDataManager.ChangeData(data, 5, 0);
                    return await DynamicGlobalDataManager.SaveAsync(data);
                }
            }
            else if (type.Equals("datastr"))
            {
                cmpStrValue = DynamicGlobalDataManager.GetStringData(data, (int)dataId);
                if (opt.Equals("set"))
                {
                    DynamicGlobalDataManager.ChangeStringData(data, (int)dataId, strValue);
                    return await DynamicGlobalDataManager.SaveAsync(data);
                }
            }
            else if (type.Equals("time"))
            {
                cmpValue = DynamicGlobalDataManager.GetData(data, (int)dataId);
                if (opt.Equals("set"))
                {
                    DynamicGlobalDataManager.ChangeTime(data, (int)dataId, (uint)value);
                    return await DynamicGlobalDataManager.SaveAsync(data);
                }
            }
            else
            {
                logger.LogInformation($"ExecuteActionDynaGlobalDataAsync: Invalid cmp type for type 150 [{type}] {action.Id}");
                return false;
            }

            switch (opt)
            {
                case "==":
                    {
                        if (type.Equals("datastr"))
                        {
                            return cmpStrValue.Equals(strValue);
                        }
                        else
                        {
                            return value == cmpValue;
                        }
                    }
                case "!=": return cmpValue != value;
                case ">=": return cmpValue >= value;
                case "<=": return cmpValue <= value;
                case ">": return cmpValue > value;
                case "<": return cmpValue < value;
            }

            return true;
        }

        #endregion

        #region Npc 200-299

        private static async Task<bool> ExecuteActionNpcAttrAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            string[] splitParams = SplitParam(param);
            if (splitParams.Length < 3)
            {
                logger.LogWarning($"ExecuteActionNpcAttr invalid param num {param}, {action.Id}");
                return false;
            }

            string ope = splitParams[0].ToLower();
            string opt = splitParams[1].ToLower();
            bool isInt = int.TryParse(splitParams[2], out int data);
            string strData = splitParams[2];

            if (splitParams.Length < 4 || !uint.TryParse(splitParams[3], out uint idNpc))
            {
                idNpc = role?.Identity ?? user?.InteractingNpc ?? 0;
            }

            var npc = RoleManager.GetRole<BaseNpc>(idNpc);
            if (npc == null)
            {
                logger.LogWarning($"ExecuteActionNpcAttr invalid NPC id {idNpc} for action {action.Id}");
                return false;
            }

            var cmp = 0;
            var strCmp = "";
            if (ope.Equals("life", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    return await npc.SetAttributesAsync(ClientUpdateType.Hitpoints, (ulong)data);
                }

                if (opt == "+=")
                {
                    return await npc.AddAttributesAsync(ClientUpdateType.Hitpoints, data);
                }

                cmp = (int)npc.Life;
            }
            else if (ope.Equals("lookface", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    return await npc.SetAttributesAsync(ClientUpdateType.Mesh, (ulong)data);
                }

                cmp = (int)npc.Mesh;
            }
            else if (ope.Equals("ownerid", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    if (!(npc is DynamicNpc dyna))
                    {
                        return false;
                    }

                    return await dyna.SetOwnerAsync((uint)data);
                }

                cmp = (int)npc.OwnerIdentity;
            }
            else if (ope.Equals("ownertype", StringComparison.InvariantCultureIgnoreCase))
            {
                cmp = (int)npc.OwnerType;
            }
            else if (ope.Equals("maxlife", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    return await npc.SetAttributesAsync(ClientUpdateType.MaxHitpoints, (ulong)data);
                }

                cmp = (int)npc.MaxLife;
            }
            else if (ope.StartsWith("data", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    npc.SetData(ope, data);
                    return await npc.SaveAsync();
                }

                if (opt == "+=")
                {
                    npc.SetData(ope, npc.GetData(ope) + data);
                    return await npc.SaveAsync();
                }

                cmp = npc.GetData(ope);
                isInt = true;
            }
            else if (ope.Equals("datastr", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    npc.DataStr = strData;
                    return await npc.SaveAsync();
                }

                if (opt == "+=")
                {
                    npc.DataStr += strData;
                    return await npc.SaveAsync();
                }

                strCmp = npc.DataStr;
            }

            switch (opt)
            {
                case "==": return isInt && cmp == data || strCmp == strData;
                case ">=": return isInt && cmp >= data;
                case "<=": return isInt && cmp <= data;
                case ">": return isInt && cmp > data;
                case "<": return isInt && cmp < data;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionNpcEraseAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            BaseNpc npc = RoleManager.GetRole<BaseNpc>(user.InteractingNpc) ?? role as BaseNpc;
            if (npc == null)
            {
                return false;
            }

            if (action.Data == 0)
            {
                await npc.DelNpcAsync();
                user.InteractingNpc = 0;
                return true;
            }

            foreach (DynamicNpc del in RoleManager.QueryRoleByType<DynamicNpc>().Where(x => x.Type == action.Data))
            {
                await del.DelNpcAsync();
            }

            return true;
        }

        private static async Task<bool> ExecuteActionNpcResetsynownerAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (!(role is DynamicNpc npc))
            {
                return false;
            }

            if ((npc.IsSynNpc() && !npc.Map.IsSynMap())
                || (npc.IsCtfFlag() && !npc.Map.IsSynMap()))
            {
                return false;
            }

            DynamicNpc.Score score = npc.GetTopScore();
            if (score != null)
            {
                Syndicate syn = SyndicateManager.GetSyndicate((int)score.Identity);
                if (npc.IsSynFlag() && syn != null)
                {
                    await BroadcastWorldMsgAsync(string.Format(StrWarWon, syn.Name),
                                                        TalkChannel.Center);
                    npc.Map.OwnerIdentity = syn.Identity;
                }
                else if (npc.IsCtfFlag())
                {
                    if (user?.IsPm() == true)
                    {
                        await user.SendAsync("CTF Flag is not handled");
                    }

                    return true;
                }

                if (syn != null)
                {
                    await npc.SetOwnerAsync(syn.Identity, true);
                }

                npc.ClearScores();
                await npc.Map.SaveAsync();
                await npc.SaveAsync();
            }

            foreach (Character player in npc.Map.QueryRoles(x => x is Character).Cast<Character>())
            {
                player.BattleSystem.ResetBattle();
                await player.MagicData.AbortMagicAsync(true);
            }

            if (npc.IsSynFlag())
            {
                foreach (BaseNpc resetNpc in RoleManager.QueryRoleByMap<BaseNpc>(npc.MapIdentity))
                {
                    if (resetNpc.IsSynFlag())
                    {
                        continue;
                    }

                    resetNpc.OwnerIdentity = npc.OwnerIdentity;
                    await resetNpc.SaveAsync();
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionNpcFindNextTableAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 4)
            {
                return false;
            }

            uint idNpc = uint.Parse(splitParam[0]);
            uint idMap = uint.Parse(splitParam[1]);
            ushort usMapX = ushort.Parse(splitParam[2]);
            ushort usMapY = ushort.Parse(splitParam[3]);

            var npc = RoleManager.GetRole<BaseNpc>(idNpc);
            if (npc == null)
            {
                return false;
            }

            npc.SetData("data0", (int)idMap);
            npc.SetData("data1", usMapX);
            npc.SetData("data2", usMapY);
            await npc.SaveAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionNpcFamilyCreateAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user == null || user.Family != null)
            {
                return false;
            }

            if (user.Level < 50 || user.Silvers < 500000)
            {
                return false;
            }

            return await user.CreateFamilyAsync(input, 500000);
        }

        private static async Task<bool> ExecuteActionNpcFamilyDestroyAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Family == null)
            {
                return false;
            }

            return await user.DisbandFamilyAsync();
        }

        private static async Task<bool> ExecuteActionNpcFamilyChangeNameAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user?.Family == null)
            {
                return false;
            }

            if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
            {
                return false;
            }

            string[] splitParams = SplitParam(param, 2);
            uint nextIdAction = uint.Parse(splitParams[0]);

            if (await user.ChangeFamilyNameAsync(input))
            {
                return await ExecuteActionAsync(nextIdAction, user, role, item, input);
            }
            return false;
        }

        #endregion

        #region Map 300-399

        private static async Task<bool> ExecuteActionMapMovenpcAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3)
            {
                return false;
            }

            uint idMap = uint.Parse(splitParam[0]);
            ushort nPosX = ushort.Parse(splitParam[1]), nPosY = ushort.Parse(splitParam[2]);

            if (idMap <= 0 || nPosX <= 0 || nPosY <= 0)
            {
                return false;
            }

            var npc = RoleManager.GetRole<BaseNpc>(action.Data);
            if (npc == null)
            {
                return false;
            }

            return await npc.ChangePosAsync(idMap, nPosX, nPosY);
        }

        private static async Task<bool> ExecuteActionMapMapuserAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3)
            {
                return false;
            }

            var amount = 0;

            if (splitParam[0].Equals("map_user", StringComparison.InvariantCultureIgnoreCase))
            {
                amount += MapManager.GetMap(action.Data)?.PlayerCount ?? 0;
            }
            else if (splitParam[0].Equals("alive_user", StringComparison.InvariantCultureIgnoreCase))
            {
                amount += RoleManager.QueryRoleByMap<Character>(action.Data).Count(x => x.IsAlive);
            }
            else
            {
                logger.LogWarning($"ExecuteActionMapMapuser invalid cmd {splitParam[0]} for action {action.Id}, {param}");
                return false;
            }

            switch (splitParam[1])
            {
                case "==":
                    return amount == int.Parse(splitParam[2]);
                case "<=":
                    return amount <= int.Parse(splitParam[2]);
                case ">=":
                    return amount >= int.Parse(splitParam[2]);
            }

            return false;
        }

        private static async Task<bool> ExecuteActionMapBrocastmsgAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            GameMap map = MapManager.GetMap(action.Data);
            if (map == null)
            {
                return false;
            }

            await map.BroadcastMsgAsync(param);
            return true;
        }

        private static async Task<bool> ExecuteActionMapDropitemAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 4)
            {
                return false;
            }

            uint idMap = uint.Parse(splitParam[1]);
            uint idItemtype = uint.Parse(splitParam[0]);
            ushort x = ushort.Parse(splitParam[2]);
            ushort y = ushort.Parse(splitParam[3]);

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                return false;
            }

            var mapItem = new MapItem((uint)IdentityManager.MapItem.GetNextIdentity);
            if (mapItem.Create(map, new Point(x, y), idItemtype, 0, 0, 0, 0, MapItem.DropMode.Common))
            {
                if (user != null && user.Map.Partition == map.Partition)
                {
                    await mapItem.EnterMapAsync();
                }
                else
                {
                    Kernel.Services.Processor.Queue(map.Partition, () => mapItem.EnterMapAsync());
                }
            }
            else
            {
                IdentityManager.MapItem.ReturnIdentity(mapItem.Identity);
                return false;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionMapSetstatusAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3)
            {
                return false;
            }

            uint idMap = uint.Parse(splitParam[0]);
            byte dwStatus = byte.Parse(splitParam[1]);
            bool flag = splitParam[2] != "0";

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                return false;
            }

            await map.SetStatusAsync(dwStatus, flag);
            return true;
        }

        private static async Task<bool> ExecuteActionMapAttribAsync(DbAction action, string param, Character user,
                                                                    Role role,
                                                                    Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3)
            {
                return false;
            }

            string szField = splitParam[0];
            string szOpt = splitParam[1];
            var x = 0;
            int data = int.Parse(splitParam[2]);
            uint idMap = 0;

            if (splitParam.Length >= 4)
            {
                idMap = uint.Parse(splitParam[3]);
            }

            GameMap map;
            if (idMap == 0)
            {
                if (user == null)
                {
                    return false;
                }

                map = MapManager.GetMap(user.MapIdentity);
            }
            else
            {
                map = MapManager.GetMap(idMap);
            }

            if (map == null)
            {
                return false;
            }

            if (szField.Equals("status", StringComparison.InvariantCultureIgnoreCase))
            {
                switch (szOpt.ToLowerInvariant())
                {
                    case "test":
                        return map.IsWarTime();
                    case "set":
                        await map.SetStatusAsync((ulong)data, true);
                        return true;
                    case "reset":
                        await map.SetStatusAsync((ulong)data, false);
                        return true;
                }
            }
            else if (szField.Equals("type", StringComparison.InvariantCultureIgnoreCase))
            {
                switch (szOpt.ToLowerInvariant())
                {
                    case "test":
                        return map.Type.HasFlag((MapTypeFlag)data);
                }
            }
            else if (szField.Equals("mapdoc", StringComparison.InvariantCultureIgnoreCase))
            {
                if (szOpt.Equals("="))
                {
                    map.MapDoc = (uint)data;
                    await map.SaveAsync();
                    return true;
                }

                x = (int)map.MapDoc;
            }
            else if (szField.Equals("portal0_x", StringComparison.InvariantCultureIgnoreCase))
            {
                if (szOpt.Equals("="))
                {
                    map.PortalX = (ushort)data;
                    await map.SaveAsync();
                    return true;
                }

                x = map.PortalX;
            }
            else if (szField.Equals("portal0_y", StringComparison.InvariantCultureIgnoreCase))
            {
                if (szOpt.Equals("="))
                {
                    map.PortalY = (ushort)data;
                    await map.SaveAsync();
                    return true;
                }

                x = map.PortalY;
            }
            else if (szField.Equals("res_lev", StringComparison.InvariantCultureIgnoreCase))
            {
                if (szOpt.Equals("="))
                {
                    map.ResLev = (byte)data;
                    await map.SaveAsync();
                    return true;
                }

                x = map.ResLev;
            }
            else
            {
                logger.LogWarning($"ExecuteActionMapAttrib invalid field {szField} for action {action.Id}, {param}");
                return false;
            }

            switch (szOpt)
            {
                case "==": return x == data;
                case ">=": return x >= data;
                case "<=": return x <= data;
                case "<": return x < data;
                case ">": return x > data;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionMapRegionMonsterAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 8)
            {
                logger.LogWarning($"ERROR: Invalid param amount on actionid: [{action.Id}]");
                return false;
            }

            string szOpt = splitParam[6];
            uint idMap = uint.Parse(splitParam[0]);
            uint idType = uint.Parse(splitParam[5]);
            ushort nRegionX = ushort.Parse(splitParam[1]),
                   nRegionY = ushort.Parse(splitParam[2]),
                   nRegionCX = ushort.Parse(splitParam[3]),
                   nRegionCY = ushort.Parse(splitParam[4]);
            int nData = int.Parse(splitParam[7]);

            GameMap map;
            if (idMap == 0)
            {
                if (user == null)
                {
                    return false;
                }

                idMap = user.MapIdentity;
                map = user.Map;
            }
            else
            {
                map = MapManager.GetMap(idMap);
            }

            if (map == null)
            {
                return false;
            }

            int count = map.QueryRoles(x => x is Monster monster && (idType != 0 && monster.Type == idType || idType == 0) && x.X >= nRegionX &&
                                                                             x.X < nRegionX + nRegionCX
                                                                             && x.Y >= nRegionY &&
                                                                             x.Y < nRegionY + nRegionCY).Count();

            switch (szOpt)
            {
                case "==": return count == nData;
                case "<=": return count <= nData;
                case ">=": return count >= nData;
                case "<": return count < nData;
                case ">": return count > nData;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionMapRandDropItemAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            // Example: 728006 3030 186 187 304 307 250 3600
            //          ItemID MAP  X   Y   CX  CY  AMOUNT DURATION
            string[] splitParam = SplitParam(param, 8);
            if (splitParam.Length != 8)
            {
                logger.LogWarning($"ExecuteActionMapRandDropItem: ItemID MAP  X   Y   CX  CY  AMOUNT DURATION :: {param} ({action.Id})");
                return false;
            }

            uint idItemtype = uint.Parse(splitParam[0]); // the item to be dropped
            uint idMap = uint.Parse(splitParam[1]);      // the map
            ushort initX = ushort.Parse(splitParam[2]);  // start coordinates
            ushort initY = ushort.Parse(splitParam[3]);  // start coordinates 
            ushort endX = ushort.Parse(splitParam[4]);   // end coordinates
            ushort endY = ushort.Parse(splitParam[5]);   // end coordinates
            int amount = int.Parse(splitParam[6]);       // amount of items to be dropped
            int duration = int.Parse(splitParam[7]);     // duration of the item in the floor

            DbItemtype itemtype = ItemManager.GetItemtype(idItemtype);
            if (itemtype == null)
            {
                logger.LogWarning($"Invalid itemtype {idItemtype}, {param}, {action.Id}");
                return false;
            }

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                logger.LogWarning($"Invalid map {idMap}, {param}, {action.Id}");
                return false;
            }

            for (var i = 0; i < amount; i++)
            {
                var mapItem = new MapItem((uint)IdentityManager.MapItem.GetNextIdentity);
                var positionRetry = 0;
                var posSuccess = true;

                int targetX = initX + await NextAsync(endX);
                int targetY = initY + await NextAsync(endY);

                var pos = new Point(targetX, targetY);
                while (!map.FindDropItemCell(9, ref pos))
                {
                    if (positionRetry++ >= 5)
                    {
                        posSuccess = false;
                        break;
                    }

                    targetX = initX + await NextAsync(endX);
                    targetY = initY + await NextAsync(endY);

                    pos = new Point(targetX, targetY);
                }

                if (!posSuccess)
                {
                    IdentityManager.MapItem.ReturnIdentity(mapItem.Identity);
                    continue;
                }

                if (!mapItem.Create(map, pos, idItemtype, 0, 0, 0, 0, MapItem.DropMode.Common))
                {
                    IdentityManager.MapItem.ReturnIdentity(mapItem.Identity);
                    continue;
                }

                mapItem.SetAliveTimeout(duration);
                if (user?.Map != null && user.Map.Partition == map.Partition)
                {
                    await mapItem.EnterMapAsync();
                }
                else
                {
                    Kernel.Services.Processor.Queue(map.Partition, () => mapItem.EnterMapAsync());
                }
            }
            return true;
        }

        private static async Task<bool> ExecuteActionMapChangeweatherAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 5)
            {
                return false;
            }

            int nType = int.Parse(pszParam[0]), nIntensity = int.Parse(pszParam[1]), nDir = int.Parse(pszParam[2]);
            uint dwColor = uint.Parse(pszParam[3]), dwKeepSecs = uint.Parse(pszParam[4]);

            GameMap map;
            if (action.Data == 0)
            {
                if (user == null)
                {
                    return false;
                }

                map = user.Map;
            }
            else
            {
                map = MapManager.GetMap(action.Data);
            }

            if (map == null)
            {
                return false;
            }

            await map.Weather.SetNewWeatherAsync((Weather.WeatherType)nType, nIntensity, nDir, (int)dwColor,
                                                 (int)dwKeepSecs, 0);
            await map.Weather.SendWeatherAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionMapChangelightAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 2)
            {
                return false;
            }

            uint idMap = uint.Parse(splitParam[0]), dwRgb = uint.Parse(splitParam[1]);

            GameMap map;
            if (action.Data == 0)
            {
                if (user == null)
                {
                    return false;
                }

                map = user.Map;
            }
            else
            {
                map = MapManager.GetMap(idMap);
            }

            if (map == null)
            {
                return false;
            }

            map.Light = dwRgb;
            await map.BroadcastMsgAsync(new MsgAction
            {
                Identity = 1,
                Command = dwRgb,
                Argument = 0,
                Action = ActionType.MapArgb
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMapMapeffectAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);

            if (splitParam.Length < 4)
            {
                return false;
            }

            uint idMap = uint.Parse(splitParam[0]);
            ushort posX = ushort.Parse(splitParam[1]), posY = ushort.Parse(splitParam[2]);
            string szEffect = splitParam[3];

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                return false;
            }

            await map.BroadcastRoomMsgAsync(posX, posY, new MsgName
            {
                Identity = 0,
                Action = StringAction.MapEffect,
                X = posX,
                Y = posY,
                Strings = new List<string>
                {
                    szEffect
                }
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMapFireworksAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (user != null)
            {
                await user.BroadcastRoomMsgAsync(new MsgName
                {
                    Identity = user.Identity,
                    Action = StringAction.Fireworks
                }, true);
            }

            return true;
        }

        private static async Task<bool> ExecuteActionMapAbleExpAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            return ((user?.Map ?? role?.Map)?.IsNoExpMap() == true);
        }

        #endregion

        #region Lay Item 400-499

        private static async Task<bool> ExecuteActionItemRequestlaynpcAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParams = SplitParam(param, 5);

            uint idNextTask = uint.Parse(splitParams[0]);
            uint dwType = uint.Parse(splitParams[1]);
            uint dwSort = uint.Parse(splitParams[2]);
            uint dwLookface = uint.Parse(splitParams[3]);
            uint dwRegion = 0;

            if (splitParams.Length > 4)
            {
                uint.TryParse(splitParams[4], out dwRegion);
            }

            if (idNextTask != 0)
            {
                user.InteractingItem = idNextTask;
            }

            await user.SendAsync(new MsgNpc
            {
                Identity = dwRegion,
                Data = dwLookface,
                Event = (ushort)dwType,
                RequestType = MsgNpc.NpcActionType.LayNpc
            });
            return true;
        }

        private static async Task<bool> ExecuteActionItemCountnpcAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user?.Map == null)
            {
                return false;
            }

            string[] splitParam = SplitParam(param, 4);
            if (splitParam.Length != 4)
            {
                return false;
            }

            string field = splitParam[0];
            string data = splitParam[1];
            string opt = splitParam[2];
            int num = int.Parse(splitParam[3]);

            int count = 0;
            if (field.Equals("all"))
            {
                count = user.Map.QueryRoles().Count;
            }
            else if (field.Equals("furniture"))
            {
                count = user.Map.QueryRoles(x => x is BaseNpc npc && (npc.Type == BaseNpc.ROLE_FURNITURE_NPC && npc.Type == BaseNpc.ROLE_3DFURNITURE_NPC)).Count;
            }
            else if (field.Equals("name"))
            {
                count = user.Map.QueryRoles(x => x is BaseNpc && x.Name.Equals(data)).Count;
            }
            else if (field.Equals("type"))
            {
                count = user.Map.QueryRoles(x => x is BaseNpc npc && npc.Type == int.Parse(data)).Count;
            }
            else return false;

            switch (opt)
            {
                case "==": return count == num;
                case ">=": return count >= num;
                case "<=": return count <= num;
                case ">": return count > num;
                case "<": return count < num;
            }
            return false;
        }

        private static async Task<bool> ExecuteActionItemLaynpcAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            string[] splitParam = SplitParam(input, 5);
            if (splitParam.Length < 3)
            {
                logger.LogWarning($"Invalid input count for action [{action.Id}]: {input}");
                return false;
            }

            if (!ushort.TryParse(splitParam[0], out ushort mapX)
                || !ushort.TryParse(splitParam[1], out ushort mapY)
                || !uint.TryParse(splitParam[2], out uint lookface))
            {
                logger.LogWarning($"Invalid input params for action [{action.Id}]1: {input}");
                return false;
            }

            uint frame = 0;
            uint pose = 0;
            if (splitParam.Length >= 4)
            {
                uint.TryParse(splitParam[3], out frame);
                uint.TryParse(splitParam[4], out pose);
            }

            if (user.Map.IsSuperPosition(mapX, mapY))
            {
                await user.SendAsync(StrLayNpcSuperPosition);
                return false;
            }

            splitParam = SplitParam(param, 21);

            if (param.Length < 5)
            {
                return false;
            }

            uint nRegionType = 0;
            string szName = splitParam[0];
            ushort usType = ushort.Parse(splitParam[1]);
            ushort usSort = ushort.Parse(splitParam[2]);
            uint dwOwnerType = uint.Parse(splitParam[4]);
            uint dwLife = 0;
            uint idBase = 0;
            uint idLink = 0;
            uint idTask0 = 0;
            uint idTask1 = 0;
            uint idTask2 = 0;
            uint idTask3 = 0;
            uint idTask4 = 0;
            uint idTask5 = 0;
            uint idTask6 = 0;
            uint idTask7 = 0;
            var idData0 = 0;
            var idData1 = 0;
            var idData2 = 0;
            var idData3 = 0;

            if (splitParam.Length >= 6)
            {
                dwLife = uint.Parse(splitParam[5]);
            }

            if (splitParam.Length >= 7)
            {
                nRegionType = uint.Parse(splitParam[6]);
            }

            if (splitParam.Length >= 8)
            {
                idBase = uint.Parse(splitParam[7]);
            }

            if (splitParam.Length >= 9)
            {
                idLink = uint.Parse(splitParam[8]);
            }

            if (splitParam.Length >= 10)
            {
                idTask0 = uint.Parse(splitParam[9]);
            }

            if (splitParam.Length >= 11)
            {
                idTask1 = uint.Parse(splitParam[10]);
            }

            if (splitParam.Length >= 12)
            {
                idTask2 = uint.Parse(splitParam[11]);
            }

            if (splitParam.Length >= 13)
            {
                idTask3 = uint.Parse(splitParam[12]);
            }

            if (splitParam.Length >= 14)
            {
                idTask4 = uint.Parse(splitParam[13]);
            }

            if (splitParam.Length >= 15)
            {
                idTask5 = uint.Parse(splitParam[14]);
            }

            if (splitParam.Length >= 16)
            {
                idTask6 = uint.Parse(splitParam[15]);
            }

            if (splitParam.Length >= 17)
            {
                idTask7 = uint.Parse(splitParam[16]);
            }

            if (splitParam.Length >= 18)
            {
                idData0 = int.Parse(splitParam[17]);
            }

            if (splitParam.Length >= 19)
            {
                idData1 = int.Parse(splitParam[18]);
            }

            if (splitParam.Length >= 20)
            {
                idData2 = int.Parse(splitParam[19]);
            }

            if (splitParam.Length >= 21)
            {
                idData3 = int.Parse(splitParam[20]);
            }

            if (usType == BaseNpc.SYNTRANS_NPC && user.Map.IsTeleportDisable())
            {
                await user.SendAsync(StrLayNpcSynTransInvalidMap);
                return false;
            }

            if (usType == BaseNpc.STATUARY_NPC)
            {
                szName = user.Name;
                lookface = user.Mesh % 10;
                idTask0 = user.Headgear?.Type ?? 0;
                idTask1 = user.Armor?.Type ?? 0;
                idTask2 = user.RightHand?.Type ?? 0;
                idTask3 = user.LeftHand?.Type ?? 0;
                idTask4 = frame;
                idTask5 = pose;
                idTask6 = user.Mesh;
                idTask7 = ((uint)user.SyndicateRank << 16) + user.Hairstyle;
            }

            if (nRegionType > 0 && !user.Map.QueryRegion((RegionType)nRegionType, mapX, mapY))
            {
                return false;
            }

            uint idOwner = 0;
            switch (dwOwnerType)
            {
                case 1:
                    if (user.Identity == 0)
                    {
                        return false;
                    }

                    idOwner = user.Identity;
                    break;
                case 2:
                    if (user.SyndicateIdentity == 0)
                    {
                        return false;
                    }

                    idOwner = user.SyndicateIdentity;
                    break;
            }

            DynamicNpc npc;
            if (usType != 15)
            {
                npc = new DynamicNpc(new DbDynanpc
                {
                    Name = szName,
                    Ownerid = idOwner,
                    OwnerType = dwOwnerType,
                    Type = usType,
                    Sort = usSort,
                    Life = dwLife,
                    Maxlife = dwLife,
                    Base = idBase,
                    Linkid = idLink,
                    Task0 = idTask0,
                    Task1 = idTask1,
                    Task2 = idTask2,
                    Task3 = idTask3,
                    Task4 = idTask4,
                    Task5 = idTask5,
                    Task6 = idTask6,
                    Task7 = idTask7,
                    Data0 = idData0,
                    Data1 = idData1,
                    Data2 = idData2,
                    Data3 = idData3,
                    Datastr = "",
                    Defence = 0,
                    Cellx = mapX,
                    Celly = mapY,
                    Idxserver = 0,
                    Itemid = 0,
                    Lookface = (ushort)lookface,
                    MagicDef = 0,
                    Mapid = user.MapIdentity
                });

                if (!await npc.InitializeAsync())
                {
                    return false;
                }
            }
            else
            {
                npc = RoleManager.QueryRoleByType<DynamicNpc>().FirstOrDefault(x => x.LinkId == idLink);
                npc.SetType(usType);
                npc.OwnerIdentity = idOwner;
                npc.OwnerType = (byte)dwOwnerType;
                await npc.SetOwnerAsync(idOwner);
                await npc.SetAttributesAsync(ClientUpdateType.Mesh, lookface);
                npc.SetSort(usSort);
                npc.SetTask(0, idTask0);
                npc.SetTask(1, idTask1);
                npc.SetTask(2, idTask2);
                npc.SetTask(3, idTask3);
                npc.SetTask(4, idTask4);
                npc.SetTask(5, idTask5);
                npc.SetTask(6, idTask6);
                npc.SetTask(7, idTask7);
                npc.Data0 = idData0;
                npc.Data1 = idData1;
                npc.Data2 = idData2;
                npc.Data3 = idData3;
                await npc.SetAttributesAsync(ClientUpdateType.MaxHitpoints, dwLife);
                npc.X = mapX;
                npc.Y = mapY;
            }

            /**
             * Reminder:
             * This action will not be queued! This requires user action and packets are processed in map queue.
             */
            await npc.SaveAsync();
            await npc.ChangePosAsync(user.MapIdentity, mapX, mapY);

            role = npc;
            user.InteractingNpc = npc.Identity;
            return true;
        }

        private static async Task<bool> ExecuteActionItemDelthisAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            user.InteractingItem = 0;
            if (item != null)
            {
                _ = user.UserPackage.SpendItemAsync(item);
            }

            _ = user.SendAsync(StrUseItem);
            return true;
        }

        #endregion

        #region Item 500-599

        private static async Task<bool> ExecuteActionItemAddAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            if (!user.UserPackage.IsPackSpare(1, action.Data))
            {
                return false;
            }

            DbItemtype itemtype = ItemManager.GetItemtype(action.Data);
            if (itemtype == null)
            {
                logger.LogWarning($"Invalid itemtype: {action.Id}, {action.Type}, {action.Data}");
                return false;
            }

            string[] splitParam = SplitParam(param);
            DbItem newItem = Item.CreateEntity(action.Data);
            newItem.PlayerId = user.Identity;

            if (item != null)
            {
                newItem.Monopoly = item.Monopoly;
            }

            bool autoActive = false;
            for (var i = 0; i < splitParam.Length; i++)
            {
                if (!int.TryParse(splitParam[i], out int value))
                {
                    continue;
                }

                switch (i)
                {
                    case 0: // amount
                        if (value > 0)
                        {
                            newItem.Amount = (ushort)Math.Min(value, ushort.MaxValue);
                        }

                        break;
                    case 1: // amount limit
                        if (value > 0)
                        {
                            newItem.AmountLimit = (ushort)Math.Min(value, ushort.MaxValue);
                        }

                        break;
                    case 2: // socket progress
                        newItem.Data = (uint)Math.Min(value, ushort.MaxValue);
                        break;
                    case 3: // gem 1
                        if (Enum.IsDefined(typeof(Item.SocketGem), (byte)value))
                        {
                            newItem.Gem1 = (byte)value;
                        }

                        break;
                    case 4: // gem 2
                        if (Enum.IsDefined(typeof(Item.SocketGem), (byte)value))
                        {
                            newItem.Gem2 = (byte)value;
                        }

                        break;
                    case 5: // effect magic 1
                        if (Enum.IsDefined(typeof(Item.ItemEffect), (ushort)value))
                        {
                            newItem.Magic1 = (byte)value;
                        }

                        break;
                    case 6: // magic 2
                        newItem.Magic2 = (byte)value;
                        break;
                    case 7: // magic 3
                        newItem.Magic3 = (byte)value;
                        break;
                    case 8: // reduce dmg
                        newItem.ReduceDmg = (byte)Math.Min(byte.MaxValue, value); // R
                        break;
                    case 9: // add life
                        newItem.AddLife = (byte)Math.Min(byte.MaxValue, value); // B
                        break;
                    case 10: // anti monster
                        newItem.AntiMonster = (byte)Math.Min(byte.MaxValue, value); // G
                        break;
                    case 11: // color
                        if (Enum.IsDefined(typeof(Item.ItemColor), (byte)value))
                        {
                            newItem.Color = (byte)value;
                        }
                        break;
                    case 12: // monopoly
                        newItem.Monopoly = (byte)Math.Min(byte.MaxValue, Math.Max(0, value));
                        break;
                    case 13: // mount color
                        newItem.Data = (uint)value;
                        break;
                    case 16: // active
                        autoActive = value != 0;
                        break;
                    case 17: // Accumulate Num
                        newItem.AccumulateNum = (uint)value;
                        break;
                    case 18: // save time
                        if (value > 0)
                        {
                            newItem.SaveTime = (uint) value;
                        }
                        else if (itemtype.SaveTime != 0)
                        {
                            newItem.SaveTime = itemtype.SaveTime;
                        }
                        break;
                }
            }

            item = new Item(user);
            if (!await item.CreateAsync(newItem))
            {
                return false;
            }

            if (autoActive && item.IsActivable())
            {
                await item.ActivateAsync();
            }

            return await user.UserPackage.AddItemAsync(item);
        }

        private static async Task<bool> ExecuteActionItemDelAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            if (action.Data != 0)
            {
                if (item != null && item.Type == action.Data)
                {
                    return await user.UserPackage.SpendItemAsync(item);
                }
                return await user.UserPackage.MultiSpendItemAsync(action.Data, action.Data, 1);
            }

            if (!string.IsNullOrEmpty(param))
            {
                item = user.UserPackage[param];
                if (item == null)
                {
                    return false;
                }

                return await user.UserPackage.SpendItemAsync(item);
            }

            return false;
        }

        private static async Task<bool> ExecuteActionItemCheckAsync(DbAction action, string param, Character user,
                                                                    Role role,
                                                                    Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            if (action.Data != 0)
            {
                return user.UserPackage.MultiCheckItem(action.Data, action.Data, 1);
            }

            if (!string.IsNullOrEmpty(param))
            {
                return user.UserPackage[param] != null;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionItemHoleAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            string[] splitParam = SplitParam(param, 2);
            if (param.Length < 2)
            {
                logger.LogWarning($"ExecuteActionItemHole invalid [{param}] param split length for action {action.Id}");
                return false;
            }

            string opt = splitParam[0];
            if (!int.TryParse(splitParam[1], out int value))
            {
                logger.LogWarning($"ExecuteActionItemHole invalid value number [{param}] for action {action.Id}");
                return false;
            }

            Item target = user.UserPackage[Item.ItemPosition.RightHand];
            if (target == null)
            {
                await user.SendAsync(StrItemErrRepairItem);
                return false;
            }

            if (opt.Equals("chkhole", StringComparison.InvariantCultureIgnoreCase))
            {
                if (value == 1)
                {
                    return target.SocketOne > Item.SocketGem.NoSocket;
                }

                if (value == 2)
                {
                    return target.SocketTwo > Item.SocketGem.NoSocket;
                }

                return false;
            }

            if (opt.Equals("makehole", StringComparison.InvariantCultureIgnoreCase))
            {
                if (value == 1 && target.SocketOne == Item.SocketGem.NoSocket)
                {
                    target.SocketOne = Item.SocketGem.EmptySocket;
                }
                else if (value == 2 && target.SocketTwo == Item.SocketGem.NoSocket)
                {
                    target.SocketTwo = Item.SocketGem.EmptySocket;
                }
                else
                {
                    return false;
                }

                await user.SendAsync(new MsgItemInfo(target, MsgItemInfo.ItemMode.Update));
                await target.SaveAsync();
                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionItemMultidelAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            string[] splitParams = SplitParam(param);
            var first = 0;
            var last = 0;
            byte amount = 0;

            if (action.Data == Item.TYPE_METEOR)
            {
                if (!byte.TryParse(splitParams[0], out amount))
                {
                    amount = 1;
                }

                if (splitParams.Length <= 1 || !int.TryParse(splitParams[1], out first))
                {
                    first = 0; // bound check
                }
                // todo set meteor bind check
                return await user.UserPackage.SpendMeteorsAsync(amount);
            }

            if (action.Data == Item.TYPE_DRAGONBALL)
            {
                if (!byte.TryParse(splitParams[0], out amount))
                {
                    amount = 1;
                }

                if (splitParams.Length <= 1 || !int.TryParse(splitParams[1], out first))
                {
                    first = 0;
                }

                return await user.UserPackage.SpendDragonBallsAsync(amount, first != 0);
            }

            if (action.Data != 0)
            {
                return false; // only Mets and DBs are supported
            }

            if (splitParams.Length < 3)
            {
                return false; // invalid format
            }

            first = int.Parse(splitParams[0]);
            last = int.Parse(splitParams[1]);
            amount = byte.Parse(splitParams[2]);

            if (splitParams.Length < 4)
            {
                return await user.UserPackage.MultiSpendItemAsync((uint)first, (uint)last, amount, true);
            }

            return await user.UserPackage.MultiSpendItemAsync((uint)first, (uint)last, amount,
                                                              int.Parse(splitParams[3]) != 0);
        }

        private static async Task<bool> ExecuteActionItemMultichkAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            string[] splitParams = SplitParam(param);
            int first;
            byte amount;
            if (action.Data == Item.TYPE_METEOR)
            {
                if (!byte.TryParse(splitParams[0], out amount))
                {
                    amount = 1;
                }

                if (splitParams.Length <= 1 || !int.TryParse(splitParams[1], out first))
                {
                    first = 0; // bound check
                }
                // todo set meteor bind check
                return user.UserPackage.MeteorAmount() >= amount;
            }

            if (action.Data == Item.TYPE_DRAGONBALL)
            {
                if (!byte.TryParse(splitParams[0], out amount))
                {
                    amount = 1;
                }

                if (splitParams.Length <= 1 || !int.TryParse(splitParams[1], out first))
                {
                    first = 0;
                }

                return user.UserPackage.DragonBallAmount(first != 0) >= amount;
            }

            if (action.Data != 0)
            {
                return false; // only Mets and DBs are supported
            }

            if (splitParams.Length < 3)
            {
                return false; // invalid format
            }

            first = int.Parse(splitParams[0]);
            int last = int.Parse(splitParams[1]);
            amount = byte.Parse(splitParams[2]);

            if (splitParams.Length < 4)
            {
                return user.UserPackage.MultiCheckItem((uint)first, (uint)last, amount, true);
            }

            return user.UserPackage.MultiCheckItem((uint)first, (uint)last, amount, int.Parse(splitParams[3]) != 0);
        }

        private static async Task<bool> ExecuteActionItemLeavespaceAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            return user?.UserPackage != null && user.UserPackage.IsPackSpare((int)action.Data);
        }

        private static async Task<bool> ExecuteActionItemUpequipmentAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 2)
            {
                return false;
            }

            string command = splitParam[0];
            byte pos = byte.Parse(splitParam[1]);

            Item pItem = user.UserPackage[(Item.ItemPosition)pos];
            if (pItem == null)
            {
                return false;
            }

            switch (command)
            {
                case "up_lev":
                    {
                        return await pItem.UpEquipmentLevelAsync();
                    }

                case "recover_dur":
                    {
                        var szPrice = (uint)pItem.GetRecoverDurCost();
                        return await user.SpendMoneyAsync((int)szPrice) && await pItem.RecoverDurabilityAsync();
                    }

                case "up_levultra":
                case "up_levultra2":
                    {
                        return await pItem.UpUltraEquipmentLevelAsync();
                    }

                case "up_quality":
                    {
                        return await pItem.UpItemQualityAsync();
                    }

                default:
                    logger.LogWarning($"ERROR: [509] [0] [{param}] not properly handled.");
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionItemEquiptestAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            /* param: position type opt value (4 quality == 9) */
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 4)
            {
                return false;
            }

            byte position = byte.Parse(splitParam[0]);
            string command = splitParam[1];
            string opt = splitParam[2];
            int data = int.Parse(splitParam[3]);

            Item pItem = user.UserPackage[(Item.ItemPosition)position];
            if (pItem == null)
            {
                return false;
            }

            var nTestData = 0;
            switch (command)
            {
                case "level":
                    nTestData = pItem.GetLevel();
                    break;
                case "quality":
                    nTestData = pItem.GetQuality();
                    break;
                case "durability":
                    if (data == -1)
                    {
                        data = pItem.MaximumDurability / 100;
                    }

                    nTestData = pItem.MaximumDurability / 100;
                    break;
                case "max_dur":
                    {
                        if (data == -1)
                        {
                            data = pItem.Itemtype.AmountLimit / 100;
                        }
                        // TODO Kylin Gem Support
                        nTestData = pItem.MaximumDurability / 100;
                        break;
                    }

                default:
                    logger.LogWarning($"ACTION: EQUIPTEST error {command}");
                    return false;
            }

            if (opt == "==")
            {
                return nTestData == data;
            }

            if (opt == "<=")
            {
                return nTestData <= data;
            }

            if (opt == ">=")
            {
                return nTestData >= data;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionItemEquipexistAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            string[] splitParam = SplitParam(param);
            if (param.Length >= 1 && user.UserPackage[(Item.ItemPosition)action.Data] != null)
            {
                return user.UserPackage[(Item.ItemPosition)action.Data].GetItemSubType() == ushort.Parse(splitParam[0]);
            }

            return user.UserPackage[(Item.ItemPosition)action.Data] != null;
        }

        private static async Task<bool> ExecuteActionItemEquipcolorAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            string[] splitParam = SplitParam(param);

            if (splitParam.Length < 2)
            {
                return false;
            }

            if (!Enum.IsDefined(typeof(Item.ItemColor), byte.Parse(splitParam[1])))
            {
                return false;
            }

            Item pItem = user.UserPackage[(Item.ItemPosition)byte.Parse(splitParam[0])];
            if (pItem == null)
            {
                return false;
            }

            Item.ItemPosition pos = pItem.GetPosition();
            if (pos != Item.ItemPosition.Armor
                && pos != Item.ItemPosition.Headwear
                && (pos != Item.ItemPosition.LeftHand || pItem.GetItemSort() != Item.ItemSort.ItemsortWeaponShield))
            {
                return false;
            }

            pItem.Color = (Item.ItemColor)byte.Parse(splitParam[1]);
            await pItem.SaveAsync();
            await user.SendAsync(new MsgItemInfo(pItem, MsgItemInfo.ItemMode.Update));
            return true;
        }

        private static async Task<bool> ExecuteActionItemTransformAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParam = SplitParam(param, 4);
            int unknown0 = int.Parse(splitParam[0]);
            int unknown1 = int.Parse(splitParam[1]);
            var transformation = uint.Parse(splitParam[2]);
            int seconds = int.Parse(splitParam[3]);
            return await user.TransformAsync(transformation, seconds, true);
        }

        private static async Task<bool> ExecuteActionItemCheckrandAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 6)
            {
                return false;
            }

            byte initValue = byte.Parse(pszParam[3]), endValue = byte.Parse(pszParam[5]);

            var lPos = new List<Item.ItemPosition>(15);

            byte pIdx = byte.Parse(pszParam[1]);

            if (initValue == 0 && pIdx == 14)
            {
                initValue = 1;
            }

            for (var i = Item.ItemPosition.EquipmentBegin; i <= Item.ItemPosition.EquipmentEnd; i++)
            {
                if (user.UserPackage[i] != null)
                {
                    if (pIdx == 14 && user.UserPackage[i].Position == Item.ItemPosition.Steed)
                    {
                        continue;
                    }

                    if (user.UserPackage[i].IsArrowSort())
                    {
                        continue;
                    }

                    switch (pIdx)
                    {
                        case 14:
                            if (user.UserPackage[i].ReduceDamage >= initValue
                                && user.UserPackage[i].ReduceDamage <= endValue)
                            {
                                continue;
                            }

                            break;
                    }

                    lPos.Add(i);
                }
            }

            byte pos = 0;

            if (lPos.Count > 0)
            {
                pos = (byte)lPos[await NextAsync(lPos.Count) % lPos.Count];
            }

            if (pos == 0)
            {
                return false;
            }

            Item pItem = user.UserPackage[(Item.ItemPosition)pos];
            if (pItem == null)
            {
                return false;
            }

            byte pPos = byte.Parse(pszParam[0]);
            string opt = pszParam[2];

            switch (pIdx)
            {
                case 14: // bless
                    user.VarData[7] = pos;
                    return true;
                default:
                    logger.LogWarning($"ACTION: 516: {pIdx} not handled id: {action.Id}");
                    break;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionItemModifyAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            // structure param:
            // pos  type    action  value   update
            // 1    7       ==      1       1
            // pos = Item Position
            // type = 7 Reduce Damage
            // action = Operator == or set
            // value = value lol
            // update = if the client will update live

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 5)
            {
                logger.LogWarning($"ACTION: incorrect param, pos type action value update, for action (id:{action.Id})");
                return false;
            }

            int pos = int.Parse(pszParam[0]);
            int type = int.Parse(pszParam[1]);
            string opt = pszParam[2];
            long cmpValue = 0;
            long value = int.Parse(pszParam[3]);
            bool update = int.Parse(pszParam[4]) > 0;

            Item updateItem = user.UserPackage[(Item.ItemPosition)pos];
            if (updateItem == null)
            {
                await user.SendAsync(StrUnableToUseItem);
                return false;
            }

            switch (type)
            {
                case 1: // itemtype
                    {
                        if (opt == "set")
                        {
                            DbItemtype itemt = ItemManager.GetItemtype((uint)value);
                            if (itemt == null)
                            {
                                // new item doesnt exist
                                logger.LogWarning($"ACTION: itemtype not found (type:{value}, action:{action.Id})");
                                return false;
                            }

                            if (updateItem.Type / 1000 != itemt.Type / 1000)
                            {
                                logger.LogWarning($"ACTION: cant change to different type (type:{updateItem.Type}, new:{value}, action:{action.Id})");
                                return false;
                            }

                            if (!await updateItem.ChangeTypeAsync(itemt.Type))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            cmpValue = updateItem.Type;
                        }
                        break;
                    }

                case 2: // owner id
                case 3: // player id
                    return false;
                case 4: // dura
                    {
                        if (opt == "set")
                        {
                            if (value > ushort.MaxValue)
                            {
                                value = ushort.MaxValue;
                            }
                            else if (value < 0)
                            {
                                value = 0;
                            }

                            updateItem.Durability = (ushort)value;
                        }
                        else
                        {
                            cmpValue = updateItem.Durability;
                        }

                        break;
                    }

                case 5: // max dura
                    {
                        if (opt == "set")
                        {
                            if (value > ushort.MaxValue)
                            {
                                value = ushort.MaxValue;
                            }
                            else if (value < 0)
                            {
                                value = 0;
                            }

                            if (value < updateItem.Durability)
                            {
                                updateItem.Durability = (ushort)value;
                            }

                            updateItem.MaximumDurability = (ushort)value;
                        }
                        else
                        {
                            cmpValue = updateItem.MaximumDurability;
                        }
                        break;
                    }

                case 6:
                case 7: // position
                    {
                        return false;
                    }

                case 8: // gem1
                    {
                        if (opt == "set")
                        {
                            updateItem.SocketOne = (Item.SocketGem)value;
                        }
                        else
                        {
                            cmpValue = (long)updateItem.SocketOne;
                        }

                        break;
                    }

                case 9: // gem2
                    {
                        if (opt == "set")
                        {
                            updateItem.SocketTwo = (Item.SocketGem)value;
                        }
                        else
                        {
                            cmpValue = (long)updateItem.SocketTwo;
                        }
                        break;
                    }

                case 10: // magic1
                    {
                        if (opt == "set")
                        {
                            if (value is < 200 or > 203)
                            {
                                return false;
                            }

                            updateItem.Effect = (Item.ItemEffect)value;
                        }
                        else
                        {
                            cmpValue = (long)updateItem.Effect;
                        }

                        break;
                    }

                case 11: // magic2
                    return false;
                case 12: // magic3
                    {
                        if (opt == "set")
                        {
                            if (value < 0)
                            {
                                value = 0;
                            }
                            else if (value > 12)
                            {
                                value = 12;
                            }

                            updateItem.ChangeAddition((byte)value);
                        }
                        else
                        {
                            cmpValue = updateItem.Plus;
                        }
                        break;
                    }

                case 13: // data
                    {
                        if (opt == "set")
                        {
                            if (value < 0)
                            {
                                value = 0;
                            }
                            else if (value > 20000)
                            {
                                value = 20000;
                            }

                            updateItem.SocketProgress = (ushort)value;
                        }
                        else
                        {
                            cmpValue = updateItem.SocketProgress;
                        }
                        break;
                    }

                case 14: // reduce damage
                    {
                        if (opt == "set")
                        {
                            if (value < 0)
                            {
                                value = 0;
                            }
                            else if (value > 7)
                            {
                                value = 7;
                            }

                            updateItem.ReduceDamage = (byte)value;
                        }
                        else
                        {
                            cmpValue = updateItem.ReduceDamage;
                        }
                        break;
                    }

                case 15: // add life
                    {
                        if (opt == "set")
                        {
                            if (value < 0)
                            {
                                value = 0;
                            }
                            else if (value > 255)
                            {
                                value = 255;
                            }

                            updateItem.Enchantment = (byte)value;
                        }
                        else
                        {
                            cmpValue = updateItem.Enchantment;
                        }
                        break;
                    }

                case 16: // anti monster
                case 17: // chk sum
                case 18: // plunder
                case 19: // special flag
                    return false;
                case 20: // color
                    {
                        if (opt == "set")
                        {
                            if (!Enum.IsDefined(typeof(Item.ItemColor), value))
                            {
                                return false;
                            }

                            updateItem.Color = (Item.ItemColor)value;
                        }
                        else
                        {
                            cmpValue = (long)updateItem.Color;
                        }
                        break;
                    }

                case 21: // add lev exp
                    {
                        if (opt == "set")
                        {
                            if (value < 0)
                            {
                                value = 0;
                            }

                            if (value > ushort.MaxValue)
                            {
                                value = ushort.MaxValue;
                            }

                            updateItem.CompositionProgress = (ushort)value;
                        }
                        else
                        {
                            cmpValue = updateItem.CompositionProgress;
                        }
                        break;
                    }
                default:
                    return false;
            }

            if ("==".Equals(opt))
            {
                return cmpValue == value;
            }
            if ("<".Equals(opt))
            {
                return cmpValue < value;
            }
            if (">".Equals(opt))
            {
                return cmpValue > value;
            }
            if (">=".Equals(opt))
            {
                return cmpValue >= value;
            }
            if ("<=".Equals(opt))
            {
                return cmpValue <= value;
            }

            await updateItem.SaveAsync();
            if (update)
            {
                await user.SendAsync(new MsgItemInfo(updateItem, MsgItemInfo.ItemMode.Update));
            }

            return true;
        }

        private static async Task<bool> ExecuteActionItemDelAllAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            Item delItem;
            while ((delItem = user.UserPackage.GetItemByType(action.Data)) != null)
            {
                await user.UserPackage.SpendItemAsync(delItem);
            }
            return true;
        }

        private static async Task<bool> ExecuteActionItemJarCreateAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            if (!user.UserPackage.IsPackSpare(1))
            {
                return false;
            }

            if (user.UserPackage.GetItemByType(Item.TYPE_JAR) != null)
            {
                await user.UserPackage.SpendItemAsync(user.UserPackage.GetItemByType(Item.TYPE_JAR));
            }

            DbItemtype itemtype = ItemManager.GetItemtype(action.Data);
            if (itemtype == null)
            {
                return false;
            }

            string[] pszParam = SplitParam(param);

            var newItem = new DbItem
            {
                AddLife = 0,
                AddlevelExp = 0,
                AntiMonster = 0,
                ChkSum = 0,
                Color = 3,
                Data = 0,
                Gem1 = 0,
                Gem2 = 0,
                Ident = 0,
                Magic1 = 0,
                Magic2 = 0,
                ReduceDmg = 0,
                Plunder = 0,
                Specialflag = 0,
                Type = itemtype.Type,
                Position = 0,
                PlayerId = user.Identity,
                Monopoly = 0,
                Magic3 = itemtype.Magic3,
                Amount = 0,
                AmountLimit = 0
            };
            for (var i = 0; i < pszParam.Length; i++)
            {
                uint value = uint.Parse(pszParam[i]);
                if (value <= 0)
                {
                    continue;
                }

                switch (i)
                {
                    case 0:
                        newItem.Amount = (ushort)value;
                        break;
                    case 1:
                        newItem.AmountLimit = (ushort)value; //(ushort) (1 << ((ushort) value));
                        break;
                    case 2:
                        // Socket Progress
                        newItem.Data = value;
                        break;
                    case 3:
                        if (Enum.IsDefined(typeof(Item.SocketGem), (byte)value))
                        {
                            newItem.Gem1 = (byte)value;
                        }

                        break;
                    case 4:
                        if (Enum.IsDefined(typeof(Item.SocketGem), (byte)value))
                        {
                            newItem.Gem2 = (byte)value;
                        }

                        break;
                    case 5:
                        if (Enum.IsDefined(typeof(Item.ItemEffect), (ushort)value))
                        {
                            newItem.Magic1 = (byte)value;
                        }

                        break;
                    case 6:
                        // magic2.. w/e
                        break;
                    case 7:
                        if (value < 256)
                        {
                            newItem.Magic3 = (byte)value;
                        }

                        break;
                    case 8:
                        if (value < 8)
                        {
                            newItem.ReduceDmg = (byte)value;
                        }

                        break;
                    case 9:
                        if (value < 256)
                        {
                            newItem.AddLife = (byte)value;
                        }

                        break;
                    case 10:
                        newItem.Specialflag = value;
                        break;
                    case 11:
                        if (Enum.IsDefined(typeof(Item.ItemColor), value))
                        {
                            newItem.Color = (byte)value;
                        }

                        break;
                    case 12:
                        if (value < 256)
                        {
                            newItem.Monopoly = (byte)value;
                        }

                        break;
                    case 13:
                    case 14:
                    case 15:
                        // R -> For Steeds only
                        // G -> For Steeds only
                        // B -> For Steeds only
                        // G == 8 R == 16
                        newItem.Data = value | (uint.Parse(pszParam[14]) << 8) | (uint.Parse(pszParam[13]) << 16);
                        break;
                }
            }

            var createItem = new Item(user);
            if (!await createItem.CreateAsync(newItem))
            {
                return false;
            }

            await user.UserPackage.AddItemAsync(createItem);

            await user.SendAsync(new MsgInteract
            {
                Action = MsgInteract.MsgInteractType.IncreaseJar,
                SenderIdentity = user.Identity,
                TargetIdentity = user.Identity,
                MagicLevel = createItem.MaximumDurability,
                Padding = Environment.TickCount,
                Timestamp = Environment.TickCount
            });
            return true;
        }

        private static async Task<bool> ExecuteActionItemJarVerifyAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            if (!user.UserPackage.IsPackSpare(1))
            {
                return false;
            }

            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 2)
            {
                return false;
            }

            uint amount = uint.Parse(pszParam[1]);
            uint monster = uint.Parse(pszParam[0]);

            Item jar = user.UserPackage.GetItemByType(action.Data);
            if (jar != null && jar.MaximumDurability == monster && amount <= jar.Data)
            {
                await user.UserPackage.SpendItemAsync(jar);
                return true;
            }
            return false;
        }

        private static async Task<bool> ExecuteActionItemRefineryAddAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.UserPackage == null || item == null)
            {
                return false;
            }

            Item minor = user.UserPackage[user.InteractingItem];
            if (minor == null || !minor.IsRefinery())
            {
                logger.LogInformation("User tried to refine a non refinery item");
                return false;
            }

            int refineryType = (int)action.Data;

            if (!ItemManager.QuenchInfoData(refineryType, out _))
            {
                logger.LogInformation("User tried to refiner an invalid type {data}", refineryType);
                return false;
            }

            string[] splitParams = SplitParam(param, 8);
            byte level = byte.Parse(splitParams[0]);
            int power1 = int.Parse(splitParams[1]);
            int unknown = int.Parse(splitParams[2]);
            int duration = int.Parse(splitParams[3]);
            int[] acceptableEquipment = new int[3];
            for (int i = 0; i < 3; i++)
            {
                acceptableEquipment[i] = int.Parse(splitParams[4 + i]);
            }
            int power2 = 0;
            if (splitParams.Length > 7)
            {
                power2 = int.Parse(splitParams[7]);
            }

            bool success = false;
            foreach (int weaponSubtype in acceptableEquipment.Where(x => x != 0))
            {
                switch (weaponSubtype.ToString().Length)
                {
                    case 1:
                        {
                            Item.ItemPosition checkPos = item.GetPosition();
                            if (item.IsWeaponTwoHand())
                            {
                                checkPos = Item.ItemPosition.LeftHand;
                            }
                            if (checkPos != (Item.ItemPosition)weaponSubtype)
                            {
                                continue;
                            }
                            success = true;
                            break;
                        }
                    case 2:
                        {
                            if (item.Type / 10000 != weaponSubtype)
                            {
                                continue;
                            }
                            success = true;
                            break;
                        }
                    case 3:
                        {
                            int itemSubtype = (int)(item.Type / 1000);
                            if (itemSubtype != weaponSubtype)
                            {
                                if (itemSubtype == 150 && itemSubtype == 151)
                                {
                                    success = true;
                                    break;
                                }
                                continue;
                            }
                            success = true;
                            break;
                        }
                    default:
                        {
                            logger.LogWarning("Invalid refinery weapon type for action[{aciton}] param[{param}]", action.Id, param);
                            return false;
                        }
                }

                if (success)
                {
                    break;
                }
            }

            if (!success)
            {
                return false;
            }

            switch (user.VipLevel)
            {
                case 1:
                case 2:
                    {
                        duration += (int)user.VipLevel * 60 * 60 * 24;
                        break;
                    }
                case 3:
                    {
                        duration += 4 * 60 * 60 * 24;
                        break;
                    }
                case 4:
                case 5:
                case 6:
                    {
                        duration += 7 * 60 * 60 * 24;
                        break;
                    }
            }

            DbItemStatus status = new()
            {
                ItemId = item.Identity,
                Level = level,
                RealSeconds = (uint)UnixTimestamp.FromDateTime(DateTime.Now.AddSeconds(duration)),
                Status = minor.Type,
                Power1 = (uint)power1,
                Power2 = (uint)power2,
                UserId = user.Identity,
                Data = (uint)refineryType
            };
            var data = await item.Quench.AppendAsync(status);
            if (data == null)
            {
                return false;
            }
            item.Quench.ActivateNextRefinery();
            await item.Quench.SendToAsync(user);
            return true;
        }

        private static async Task<bool> ExecuteActionItemSuperFlagAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user == null)
            {
                logger.LogWarning($"Invalid actor for action {action.Id} 544");
                return false;
            }
            if (item == null)
            {
                logger.LogWarning($"Invalid item for action {action.Id} 544");
                return false;
            }

            if (user.Map == null)
            {
                logger.LogWarning($"Actor {user.Identity} not in map group");
                return false;
            }

            if (user.Map.IsChgMapDisable() || user.Map.IsPrisionMap())
            {
                logger.LogWarning("Agate not allowed");
                await user.SendAsync(StrSuperFlagNotAllowedInMap);
                return false;
            }

            await item.SendSuperFlagListAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionItemWeaponRChangeSubtypeAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParams = SplitParam(param, 2);
            if (splitParams.Length < 2)
            {
                logger.LogWarning($"Invalid param count for ExecuteActionItemWeaponRChangeSubtypeAsync: [pos] [newSubType]");
                return false;
            }

            Item.ItemPosition position = (Item.ItemPosition)byte.Parse(splitParams[0]);
            ushort newSubType = ushort.Parse(splitParams[1]);

            item = user.UserPackage[position];
            if (item == null)
            {
                logger.LogWarning($"No equipment found on position [{position}], user {user.Name}");
                return false;
            }

            uint newType = (uint)(item.Type % 1000 + newSubType * 1000);
            DbItemtype newItemtype = ItemManager.GetItemtype(newType);
            if (newItemtype == null)
            {
                logger.LogInformation($"Change subtype invalid new itemtype for item: {item.Type} >> {newType}");
                return false;
            }

            if (Item.GetPosition(newType) != Item.GetPosition(item.Type))
            {
                logger.LogInformation($"Change subtype invalid new item positon: {item.Type} >> {newType}");
                return false;
            }

            return await item.ChangeTypeAsync(newType);
        }

        private static async Task<bool> ExecuteActionItemAddSpecialAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
            {
                return false;
            }

            if (!user.UserPackage.IsPackSpare(1, action.Data))
            {
                return false;
            }

            DbItemtype itemtype = ItemManager.GetItemtype(action.Data);
            if (itemtype == null)
            {
                logger.LogWarning($"Invalid itemtype: {action.Id}, {action.Type}, {action.Data}");
                return false;
            }

            string[] splitParam = SplitParam(param);
            bool mergeable = itemtype.AccumulateLimit > 1;
            int amount = 1;
            if (splitParam.Length > 2 && !mergeable)
            {
                amount = Math.Max(1, int.Parse(splitParam[1]));
            }

            for (int itemCounter = 0; itemCounter < amount; itemCounter++)
            {
                DbItem newItem = Item.CreateEntity(action.Data);
                newItem.PlayerId = user.Identity;

                if (item != null)
                {
                    newItem.Monopoly = item.Monopoly;
                }

                for (var i = 0; i < splitParam.Length; i++)
                {
                    if (!int.TryParse(splitParam[i], out int value))
                    {
                        continue;
                    }

                    switch (i)
                    {
                        case 0:
                            {
                                break;
                            }
                        case 1:
                            {
                                if (mergeable)
                                {
                                    newItem.AccumulateNum = (uint)value;
                                }
                                break;
                            }
                        case 2:
                            {
                                newItem.Monopoly = (byte)value;
                                break;
                            }
                        case 3:
                            {
                                if (value > 0)
                                {
                                    newItem.SaveTime = (uint)value;
                                }
                                break;
                            }
                        case 4:
                            {
                                break;
                            }
                        case 5:
                            {
                                break;
                            }
                        case 6:
                            {
                                newItem.Data = (uint)value;
                                break;
                            }
                        case 7:
                            {
                                newItem.ReduceDmg = (byte)value;
                                break;
                            }
                        case 8:
                            {
                                newItem.AddLife = (byte)value;
                                break;
                            }
                        case 9:
                            {
                                newItem.AntiMonster = (byte)value;
                                break;
                            }
                        case 10:
                            {
                                newItem.Magic3 = (byte)value;
                                break;
                            }
                    }
                }

                item = new Item(user);
                if (!await item.CreateAsync(newItem))
                {
                    break;
                }

                if (item.IsActivable())
                {
                    await item.ActivateAsync();
                }

                await user.UserPackage.AddItemAsync(item);
            }
            return true;
        }

        #endregion

        #region Syndicate 700-799

        private static async Task<bool> ExecuteActionSynCreateAsync(DbAction action, string param, Character user,
                                                                    Role role,
                                                                    Item item, string input)
        {
            if (user == null || user.Syndicate != null)
            {
                return false;
            }

            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 2)
            {
                logger.LogWarning($"Invalid param count for guild creation: {param}, {action.Id}");
                return false;
            }

            if (!int.TryParse(splitParam[0], out int level))
            {
                return false;
            }

            if (user.Level < level)
            {
                await user.SendAsync(StrNotEnoughLevel);
                return false;
            }

            if (!int.TryParse(splitParam[1], out int price))
            {
                return false;
            }

            if (user.Silvers < (ulong)price)
            {
                await user.SendAsync(StrNotEnoughMoney);
                return false;
            }

            return await user.CreateSyndicateAsync(input, price);
        }


        private static async Task<bool> ExecuteActionSynDestroyAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user?.Syndicate == null)
            {
                return false;
            }

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                await user.SendAsync(StrSynNotLeader);
                return false;
            }

            return await user.DisbandSyndicateAsync();
        }

        private static async Task<bool> ExecuteActionSynSetAssistantAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user?.Syndicate == null || user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                return false;
            }

            return await user.Syndicate.PromoteAsync(user, input, SyndicateMember.SyndicateRank.DeputyLeader);
        }

        private static async Task<bool> ExecuteActionSynClearRankAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (user?.Syndicate == null || user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                return false;
            }

            return await user.Syndicate.DemoteAsync(user, input);
        }

        private static async Task<bool> ExecuteActionSynChangeLeaderAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user?.Syndicate == null || user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                return false;
            }

            return await user.Syndicate.PromoteAsync(user, input, SyndicateMember.SyndicateRank.GuildLeader);
        }

        private static async Task<bool> ExecuteActionSynAntagonizeAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.Syndicate == null)
            {
                return false;
            }

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                return false;
            }

            Syndicate target = SyndicateManager.GetSyndicate(input);
            if (target == null)
            {
                return false;
            }

            if (target.Identity == user.SyndicateIdentity)
            {
                return false;
            }

            return await user.Syndicate.AntagonizeAsync(user, target);
        }

        private static async Task<bool> ExecuteActionSynClearAntagonizeAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Syndicate == null)
            {
                return false;
            }

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                return false;
            }

            Syndicate target = SyndicateManager.GetSyndicate(input);
            if (target == null)
            {
                return false;
            }

            if (target.Identity == user.SyndicateIdentity)
            {
                return false;
            }

            return await user.Syndicate.PeaceAsync(user, target);
        }

        private static async Task<bool> ExecuteActionSynAllyAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user?.Syndicate == null)
            {
                return false;
            }

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                return false;
            }

            Syndicate target = user.Team?.Members.FirstOrDefault(x => x.Identity != user.Identity)?.Syndicate;
            if (target == null)
            {
                return false;
            }

            if (target.Identity == user.SyndicateIdentity)
            {
                return false;
            }

            return await user.Syndicate.CreateAllianceAsync(user, target);
        }

        private static async Task<bool> ExecuteActionSynClearAllyAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (user?.Syndicate == null)
            {
                return false;
            }

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                return false;
            }

            Syndicate target = SyndicateManager.GetSyndicate(input);
            if (target == null)
            {
                return false;
            }

            if (target.Identity == user.SyndicateIdentity)
            {
                return false;
            }

            return await user.Syndicate.DisbandAllianceAsync(user, target);
        }

        private static async Task<bool> ExecuteActionSynAttrAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            string[] splitParam = SplitParam(param, 4);
            if (splitParam.Length < 3)
            {
                return false;
            }

            string field = splitParam[0],
                   opt = splitParam[1];
            long value = long.Parse(splitParam[2]);

            Syndicate target = null;
            if (splitParam.Length < 4)
            {
                target = user.Syndicate;
            }
            else
            {
                target = SyndicateManager.GetSyndicate(int.Parse(splitParam[3]));
            }

            if (target == null)
            {
                return true;
            }

            long data = 0;
            if (field.Equals("money", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt.Equals("+="))
                {
                    if (target.Money + value < 0)
                    {
                        return false;
                    }

                    target.Money = (int)Math.Max(0, target.Money + value);
                    return await target.SaveAsync();
                }

                data = target.Money;
            }
            else if (field.Equals("membernum", StringComparison.InvariantCultureIgnoreCase))
            {
                data = target.MemberCount;
            }

            switch (opt)
            {
                case "==": return data == value;
                case ">=": return data >= value;
                case "<=": return data <= value;
                case ">": return data > value;
                case "<": return data < value;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionSynChangeNameAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user?.Syndicate == null)
            {
                return false;
            }

            string[] splitParams = SplitParam(param, 2);
            uint nextIdAction = uint.Parse(splitParams[0]);

            if (await user.ChangeSyndicateNameAsync(input))
            {
                return await ExecuteActionAsync(nextIdAction, user, role, item, input);
            }
            return false;
        }

        #endregion

        #region Monster 800-899

        private static async Task<bool> ExecuteActionMstDropitemAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (role == null || !(role is Monster monster))
                return false;

            string[] splitParam = SplitParam(param, 2);
            if (splitParam.Length < 2)
                return false;

            string ope = splitParam[0];
            uint data = uint.Parse(splitParam[1]);

            var percent = 100;
            if (splitParam.Length >= 3)
                percent = int.Parse(splitParam[2]);

            var flag = 0;
            if (splitParam.Length >= 4)
                flag = int.Parse(splitParam[3]);

            if (ope.Equals("dropitem"))
            {
                int quality = (int)(data % 10);
                if (Item.IsEquipment(data) && quality > 5)
                {
                    ServerStatisticManager.DropQualityItem(quality);
                }
                else if (data == Item.TYPE_METEOR)
                {
                    ServerStatisticManager.DropMeteor();
                }
                else if (data == Item.TYPE_DRAGONBALL)
                {
                    ServerStatisticManager.DropDragonBall();

                    if (user != null)
                    {
                        await monster.SendEffectAsync(user, "darcue");
                    }
                    else
                    {
                        await monster.SendEffectAsync("darcue", false);
                    }
                }
                else if (Item.IsGem(data))
                {
                    ServerStatisticManager.DropGem((Item.SocketGem)(data % 1000));
                }
                await monster.DropItemAsync(data, user, (MapItem.DropMode)flag);
                return true;
            }

            if (ope.Equals("dropmoney"))
            {
                percent %= 100;
                var dwMoneyDrop = (uint)(data * (percent + await NextAsync(100 - percent)) / 100);
                if (dwMoneyDrop <= 0)
                    return false;
                uint idUser = user?.Identity ?? 0u;
                await monster.DropMoneyAsync(dwMoneyDrop, idUser, (MapItem.DropMode)flag);
                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionMstTeamRewardAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParams = SplitParam(param, 3);
            if (splitParams.Length < 3)
            {
                logger.LogWarning($"{action.Id} invalid number of params: unknown unknown study_points");
                return false;
            }

            int u0 = int.Parse(splitParams[0]);
            int u1 = int.Parse(splitParams[1]);
            int studyPoints = int.Parse(splitParams[2]);

            if (studyPoints > 0)
            {
                if (user.Team != null)
                {
                    foreach (var member in user.Team.Members)
                    {
                        if (member.Identity != user.Identity)
                        {
                            if (member.MapIdentity != user.MapIdentity || user.GetDistance(member) > Screen.VIEW_SIZE * 2)
                            {
                                continue;
                            }
                        }
                        await member.AwardCultivationAsync(studyPoints);
                    }
                }
                else
                {
                    await user.AwardCultivationAsync(studyPoints);
                }
            }
            return true;
        }

        #endregion

        #region User 1000-1099

        private static async Task<bool> ExecuteUserAttrAsync(DbAction action, string param, Character user, Role role,
                                                             Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] parsedParam = SplitParam(param);
            if (parsedParam.Length < 3)
            {
                logger.LogWarning($"GameAction::ExecuteUserAttr[{action.Id}] invalid param num {param}");
                return false;
            }

            string type = "", opt = "", value = "", last = "";
            type = parsedParam[0];
            opt = parsedParam[1];
            value = parsedParam[2];
            if (parsedParam.Length > 3)
            {
                last = parsedParam[3];
            }

            switch (type.ToLower())
            {
                #region Force (>, >=, <, <=, =, +=, set)

                case "force":
                case "strength":
                    int forceValue = int.Parse(value);
                    if (opt.Equals(">"))
                    {
                        return user.Strength > forceValue;
                    }

                    if (opt.Equals(">="))
                    {
                        return user.Strength >= forceValue;
                    }

                    if (opt.Equals("<"))
                    {
                        return user.Strength < forceValue;
                    }

                    if (opt.Equals("<="))
                    {
                        return user.Strength <= forceValue;
                    }

                    if (opt.Equals("=") || opt.Equals("=="))
                    {
                        return user.Strength == forceValue;
                    }

                    if (opt.Equals("+="))
                    {
                        await user.AddAttributesAsync(ClientUpdateType.Strength, forceValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Strength, (ulong)forceValue);
                        return true;
                    }

                    break;

                #endregion

                #region Speed (>, >=, <, <=, =, +=, set)

                case "agility":
                case "speed":
                    int speedValue = int.Parse(value);
                    if (opt.Equals(">"))
                    {
                        return user.Speed > speedValue;
                    }

                    if (opt.Equals(">="))
                    {
                        return user.Speed >= speedValue;
                    }

                    if (opt.Equals("<"))
                    {
                        return user.Speed < speedValue;
                    }

                    if (opt.Equals("<="))
                    {
                        return user.Speed <= speedValue;
                    }

                    if (opt.Equals("=") || opt.Equals("=="))
                    {
                        return user.Speed == speedValue;
                    }

                    if (opt.Equals("+="))
                    {
                        await user.AddAttributesAsync(ClientUpdateType.Agility, speedValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Agility, (ulong)speedValue);
                        return true;
                    }

                    break;

                #endregion

                #region Health (>, >=, <, <=, =, +=, set)

                case "vitality":
                case "health":
                    int healthValue = int.Parse(value);
                    if (opt.Equals(">"))
                    {
                        return user.Vitality > healthValue;
                    }

                    if (opt.Equals(">="))
                    {
                        return user.Vitality >= healthValue;
                    }

                    if (opt.Equals("<"))
                    {
                        return user.Vitality < healthValue;
                    }

                    if (opt.Equals("<="))
                    {
                        return user.Vitality <= healthValue;
                    }

                    if (opt.Equals("=") || opt.Equals("=="))
                    {
                        return user.Vitality == healthValue;
                    }

                    if (opt.Equals("+="))
                    {
                        await user.AddAttributesAsync(ClientUpdateType.Vitality, healthValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Vitality, (ulong)healthValue);
                        return true;
                    }

                    break;

                #endregion

                #region Soul (>, >=, <, <=, =, +=, set)

                case "spirit":
                case "soul":
                    int soulValue = int.Parse(value);
                    if (opt.Equals(">"))
                    {
                        return user.Spirit > soulValue;
                    }

                    if (opt.Equals(">="))
                    {
                        return user.Spirit >= soulValue;
                    }

                    if (opt.Equals("<"))
                    {
                        return user.Spirit < soulValue;
                    }

                    if (opt.Equals("<="))
                    {
                        return user.Spirit <= soulValue;
                    }

                    if (opt.Equals("=") || opt.Equals("=="))
                    {
                        return user.Spirit == soulValue;
                    }

                    if (opt.Equals("+="))
                    {
                        await user.AddAttributesAsync(ClientUpdateType.Spirit, soulValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Spirit, (ulong)soulValue);
                        return true;
                    }

                    break;

                #endregion

                #region Attribute Points (>, >=, <, <=, =, +=, set)

                case "attr_points":
                case "attr":
                    int attrValue = int.Parse(value);
                    if (opt.Equals(">"))
                    {
                        return user.AttributePoints > attrValue;
                    }

                    if (opt.Equals(">="))
                    {
                        return user.AttributePoints >= attrValue;
                    }

                    if (opt.Equals("<"))
                    {
                        return user.AttributePoints < attrValue;
                    }

                    if (opt.Equals("<="))
                    {
                        return user.AttributePoints <= attrValue;
                    }

                    if (opt.Equals("=") || opt.Equals("=="))
                    {
                        return user.AttributePoints == attrValue;
                    }

                    if (opt.Equals("+="))
                    {
                        await user.AddAttributesAsync(ClientUpdateType.Atributes, attrValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Atributes, (ulong)attrValue);
                        return true;
                    }

                    break;

                #endregion

                #region Level (>, >=, <, <=, =, +=, set)

                case "level":
                    int levelValue = int.Parse(value);
                    if (opt.Equals(">"))
                    {
                        return user.Level > levelValue;
                    }

                    if (opt.Equals(">="))
                    {
                        return user.Level >= levelValue;
                    }

                    if (opt.Equals("<"))
                    {
                        return user.Level < levelValue;
                    }

                    if (opt.Equals("<="))
                    {
                        return user.Level <= levelValue;
                    }

                    if (opt.Equals("=") || opt.Equals("=="))
                    {
                        return user.Level == levelValue;
                    }

                    if (opt.Equals("+="))
                    {
                        await user.AddAttributesAsync(ClientUpdateType.Level, levelValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Level, (ulong)levelValue);
                        return true;
                    }

                    break;

                #endregion

                #region Metempsychosis (>, >=, <, <=, =, +=, set)

                case "metempsychosis":
                    int metempsychosisValue = int.Parse(value);
                    if (opt.Equals(">"))
                    {
                        return user.Metempsychosis > metempsychosisValue;
                    }

                    if (opt.Equals(">="))
                    {
                        return user.Metempsychosis >= metempsychosisValue;
                    }

                    if (opt.Equals("<"))
                    {
                        return user.Metempsychosis < metempsychosisValue;
                    }

                    if (opt.Equals("<="))
                    {
                        return user.Metempsychosis <= metempsychosisValue;
                    }

                    if (opt.Equals("=") || opt.Equals("=="))
                    {
                        return user.Metempsychosis == metempsychosisValue;
                    }

                    if (opt.Equals("+="))
                    {
                        await user.AddAttributesAsync(ClientUpdateType.Reborn, metempsychosisValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Reborn, (ulong)metempsychosisValue);
                        return true;
                    }

                    break;

                #endregion

                #region Money (>, >=, <, <=, =, +=, set)

                case "money":
                case "silver":
                    {
                        var moneyValue = long.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.Silvers > (ulong)moneyValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.Silvers >= (ulong)moneyValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.Silvers < (ulong)moneyValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.Silvers <= (ulong)moneyValue;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.Silvers == (ulong)moneyValue;
                        }

                        if (opt.Equals("+="))
                        {
                            return await user.ChangeMoneyAsync(moneyValue);
                        }

                        if (opt.Equals("set"))
                        {
                            await user.SetAttributesAsync(ClientUpdateType.Money, (ulong)moneyValue);
                            return true;
                        }

                        break;
                    }

                #endregion

                #region Emoney (>, >=, <, <=, =, +=, set)

                case "emoney":
                case "e_money":
                case "cps":
                    {
                        long emoneyValue = long.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.ConquerPoints > emoneyValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.ConquerPoints >= emoneyValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.ConquerPoints < emoneyValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.ConquerPoints <= emoneyValue;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.ConquerPoints == emoneyValue;
                        }

                        if (opt.Equals("+="))
                        {
                            return await user.ChangeConquerPointsAsync((int)emoneyValue);
                        }

                        if (opt.Equals("set"))
                        {
                            await user.SetAttributesAsync(ClientUpdateType.ConquerPoints, (ulong)emoneyValue);
                            return true;
                        }

                        break;
                    }
                #endregion

                #region Emoney Bound (>, >=, <, <=, =, +=, set)

                case "e_money_mono":
                    {
                        int emoneyValue = int.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.ConquerPointsBound > emoneyValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.ConquerPointsBound >= emoneyValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.ConquerPointsBound < emoneyValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.ConquerPointsBound <= emoneyValue;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.ConquerPointsBound == emoneyValue;
                        }

                        if (opt.Equals("+="))
                        {
                            return await user.ChangeBoundConquerPointsAsync(emoneyValue);
                        }

                        if (opt.Equals("set"))
                        {
                            await user.SetAttributesAsync(ClientUpdateType.BoundConquerPoints, (ulong)emoneyValue);
                            return true;
                        }

                        break;
                    }
                #endregion

                #region Rankshow (>, >=, <, <=, =)

                case "rank":
                case "rankshow":
                case "rank_show":
                    {
                        int rankShowValue = int.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.SyndicateRank > (SyndicateMember.SyndicateRank)rankShowValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.SyndicateRank >= (SyndicateMember.SyndicateRank)rankShowValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.SyndicateRank < (SyndicateMember.SyndicateRank)rankShowValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.SyndicateRank <= (SyndicateMember.SyndicateRank)rankShowValue;
                        }

                        if (opt.Equals("==") || opt.Equals("="))
                        {
                            return user.SyndicateRank == (SyndicateMember.SyndicateRank)rankShowValue;
                        }

                        break;
                    }

                #endregion

                #region Syn User Time (>, >=, <, <=, =)

                case "syn_user_time":
                    {
                        int synTime = int.Parse(value);
                        if (user.Syndicate == null)
                        {
                            return false;
                        }

                        var synDays = (int)(DateTime.Now - user.SyndicateMember.JoinDate).TotalDays;
                        if (opt.Equals("==") || opt.Equals("="))
                        {
                            return synDays == synTime;
                        }

                        if (opt.Equals(">="))
                        {
                            return synDays >= synTime;
                        }

                        if (opt.Equals(">"))
                        {
                            return synDays > synTime;
                        }

                        if (opt.Equals("<="))
                        {
                            return synDays <= synTime;
                        }

                        if (opt.Equals("<"))
                        {
                            return synDays < synTime;
                        }

                        break;
                    }

                #endregion

                #region Experience (>, >=, <, <=, =, +=, set)

                case "exp":
                case "experience":
                    {
                        ulong expValue = ulong.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.Experience > expValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.Experience >= expValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.Experience < expValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.Experience <= expValue;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.Experience == expValue;
                        }

                        if (opt.Equals("+="))
                        {
                            if (user.Map != null && user.Map.IsNoExpMap())
                            {
                                return true;
                            }

                            return await user.AwardExperienceAsync((long)expValue, last.Equals("nocontribute"));
                        }

                        if (opt.Equals("set"))
                        {
                            await user.SetAttributesAsync(ClientUpdateType.Experience, expValue);
                            return true;
                        }

                        break;
                    }

                #endregion

                #region Stamina (>, >=, <, <=, =, +=, set)

                case "ep":
                case "energy":
                case "stamina":
                    {
                        int energyValue = int.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.Energy > energyValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.Energy >= energyValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.Energy < energyValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.Energy <= energyValue;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.Energy == energyValue;
                        }

                        if (opt.Equals("+="))
                        {
                            return await user.AddAttributesAsync(ClientUpdateType.Stamina, energyValue);
                        }

                        if (opt.Equals("set"))
                        {
                            await user.SetAttributesAsync(ClientUpdateType.Stamina, (ulong)energyValue);
                            return true;
                        }

                        break;
                    }

                #endregion

                #region Life (>, >=, <, <=, =, +=, set)

                case "life":
                case "hp":
                    {
                        int lifeValue = int.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.Life > lifeValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.Life >= lifeValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.Life < lifeValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.Life <= lifeValue;
                        }

                        if (opt.Equals("=="))
                        {
                            return user.Life == lifeValue;
                        }

                        if (opt.Equals("+="))
                        {
                            user.QueueAction(async () =>
                            {
                                await user.AddAttributesAsync(ClientUpdateType.Hitpoints, lifeValue);
                                //    if (!user.IsAlive)
                                //        await user.BeKillAsync(null);
                            });
                            return true;
                        }

                        if (opt.Equals("set") || opt.Equals("="))
                        {
                            user.QueueAction(async () =>
                            {
                                await user.SetAttributesAsync(ClientUpdateType.Hitpoints, (ulong)lifeValue);
                                //if (!user.IsAlive)
                                //    await user.BeKillAsync(null);
                            });
                            return true;
                        }

                        break;
                    }

                #endregion

                #region Mana (>, >=, <, <=, =, +=, set)

                case "mana":
                case "mp":
                    {
                        int manaValue = int.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.Mana > manaValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.Mana >= manaValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.Mana < manaValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.Mana <= manaValue;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.Mana == manaValue;
                        }

                        if (opt.Equals("+="))
                        {
                            return await user.AddAttributesAsync(ClientUpdateType.Mana, manaValue);
                        }

                        if (opt.Equals("set"))
                        {
                            await user.SetAttributesAsync(ClientUpdateType.Mana, (ulong)manaValue);
                            return true;
                        }

                        break;
                    }

                #endregion

                #region Pk (>, >=, <, <=, =, +=, set)

                case "pk":
                case "pkp":
                    {
                        int pkValue = int.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.PkPoints > pkValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.PkPoints >= pkValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.PkPoints < pkValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.PkPoints <= pkValue;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.PkPoints == pkValue;
                        }

                        if (opt.Equals("+="))
                        {
                            return await user.AddAttributesAsync(ClientUpdateType.PkPoints, pkValue);
                        }

                        if (opt.Equals("set"))
                        {
                            await user.SetAttributesAsync(ClientUpdateType.PkPoints, (ulong)pkValue);
                            return true;
                        }

                        break;
                    }

                #endregion

                #region Profession (>, >=, <, <=, =, +=, set)

                case "profession":
                case "pro":
                    {
                        int proValue = int.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.Profession > proValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.Profession >= proValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.Profession < proValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.Profession <= proValue;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.Profession == proValue;
                        }

                        if (opt.Equals("+="))
                        {
                            return await user.AddAttributesAsync(ClientUpdateType.Class, proValue);
                        }

                        if (opt.Equals("set"))
                        {
                            await user.SetAttributesAsync(ClientUpdateType.Class, (ulong)proValue);
                            return true;
                        }

                        break;
                    }

                #endregion

                #region First Profession (>, >=, <, <=, =)

                case "first_prof":
                    {
                        int proValue = int.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.FirstProfession > proValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.FirstProfession >= proValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.FirstProfession < proValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.FirstProfession <= proValue;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.FirstProfession == proValue;
                        }

                        break;
                    }

                #endregion

                #region Last Profession (>, >=, <, <=, =)

                case "old_prof":
                    {
                        int proValue = int.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.PreviousProfession > proValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.PreviousProfession >= proValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.PreviousProfession < proValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.PreviousProfession <= proValue;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.PreviousProfession == proValue;
                        }

                        break;
                    }

                #endregion

                #region Transform (>, >=, <, <=, =, ==)

                case "transform":
                    int transformValue = int.Parse(value);
                    if (opt.Equals(">"))
                    {
                        return user.TransformationMesh > transformValue;
                    }

                    if (opt.Equals(">="))
                    {
                        return user.TransformationMesh >= transformValue;
                    }

                    if (opt.Equals("<"))
                    {
                        return user.TransformationMesh < transformValue;
                    }

                    if (opt.Equals("<="))
                    {
                        return user.TransformationMesh <= transformValue;
                    }

                    if (opt.Equals("=") || opt.Equals("=="))
                    {
                        return user.TransformationMesh == transformValue;
                    }

                    break;

                #endregion

                #region Virtue (>, >=, <, <=, =, +=, set)

                case "virtue":
                case "vp":
                    int virtueValue = int.Parse(value);
                    if (opt.Equals(">"))
                    {
                        return user.VirtuePoints > virtueValue;
                    }

                    if (opt.Equals(">="))
                    {
                        return user.VirtuePoints >= virtueValue;
                    }

                    if (opt.Equals("<"))
                    {
                        return user.VirtuePoints < virtueValue;
                    }

                    if (opt.Equals("<="))
                    {
                        return user.VirtuePoints <= virtueValue;
                    }

                    if (opt.Equals("=") || opt.Equals("=="))
                    {
                        return user.VirtuePoints == virtueValue;
                    }

                    if (opt.Equals("+="))
                    {
                        if (virtueValue > 0)
                        {
                            user.VirtuePoints += (uint)virtueValue;
                        }
                        else
                        {
                            user.VirtuePoints -= (uint)(virtueValue * -1);
                        }

                        return await user.SaveAsync();
                    }

                    if (opt.Equals("set"))
                    {
                        user.VirtuePoints = (uint)virtueValue;
                        return await user.SaveAsync();
                    }

                    break;

                #endregion

                #region Vip (>, >=, <, <=, =, ==)

                case "vip":
                    {
                        int vipValue = int.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.VipLevel > vipValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.VipLevel >= vipValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.VipLevel < vipValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.VipLevel <= vipValue;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.VipLevel == vipValue;
                        }

                        break;
                    }

                #endregion

                #region Xp (>, >=, <, <=, =, +=, set)

                case "xp":
                    int xpValue = int.Parse(value);
                    if (opt.Equals(">"))
                    {
                        return user.XpPoints > xpValue;
                    }

                    if (opt.Equals(">="))
                    {
                        return user.XpPoints >= xpValue;
                    }

                    if (opt.Equals("<"))
                    {
                        return user.XpPoints < xpValue;
                    }

                    if (opt.Equals("<="))
                    {
                        return user.XpPoints <= xpValue;
                    }

                    if (opt.Equals("=") || opt.Equals("=="))
                    {
                        return user.XpPoints == xpValue;
                    }

                    if (opt.Equals("+="))
                    {
                        await user.AddXpAsync((byte)xpValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetXpAsync((byte)xpValue);
                        return true;
                    }

                    break;

                #endregion

                #region Iterator (>, >=, <, <=, =, +=, set)

                case "iterator":
                    int iteratorValue = int.Parse(value);
                    if (opt.Equals(">"))
                    {
                        return user.Iterator > iteratorValue;
                    }

                    if (opt.Equals(">="))
                    {
                        return user.Iterator >= iteratorValue;
                    }

                    if (opt.Equals("<"))
                    {
                        return user.Iterator < iteratorValue;
                    }

                    if (opt.Equals("<="))
                    {
                        return user.Iterator <= iteratorValue;
                    }

                    if (opt.Equals("=="))
                    {
                        return user.Iterator == iteratorValue;
                    }

                    if (opt.Equals("+="))
                    {
                        user.Iterator += iteratorValue;
                        return true;
                    }

                    if (opt.Equals("set") || opt == "=")
                    {
                        user.Iterator = iteratorValue;
                        return true;
                    }

                    break;

                #endregion

                #region Merchant (==, set)

                case "business":
                    int merchantValue = int.Parse(value);
                    if (opt.Equals("=="))
                    {
                        return user.Merchant == merchantValue;
                    }

                    if (opt.Equals("set") || opt == "=")
                    {
                        if (merchantValue == 0)
                        {
                            await user.RemoveMerchantAsync();
                        }
                        else
                        {
                            await user.SetMerchantAsync();
                        }

                        return true;
                    }

                    break;

                #endregion

                #region Look (==, set)

                case "look":
                    {
                        switch (opt)
                        {
                            case "==": return user.Mesh % 10 == ushort.Parse(value);
                            case "set":
                                {
                                    ushort usVal = ushort.Parse(value);
                                    if (user.Gender == 1 && (usVal == 3 || usVal == 4))
                                    {
                                        user.Body = (BodyType)(1000 + usVal);
                                        await user.SynchroAttributesAsync(ClientUpdateType.Mesh, user.Mesh, true);
                                        await user.SaveAsync();
                                        return true;
                                    }

                                    if (user.Gender == 2 && (usVal == 1 || usVal == 2))
                                    {
                                        user.Body = (BodyType)(2000 + usVal);
                                        await user.SynchroAttributesAsync(ClientUpdateType.Mesh, user.Mesh, true);
                                        await user.SaveAsync();
                                        return true;
                                    }

                                    return false;
                                }
                        }

                        return false;
                    }

                #endregion

                #region Body (set)

                case "body":
                    {
                        switch (opt)
                        {
                            case "set":
                                {
                                    ushort usNewBody = ushort.Parse(value);
                                    if (usNewBody == 1003 || usNewBody == 1004)
                                    {
                                        if (user.Body != BodyType.AgileFemale && user.Body != BodyType.MuscularFemale)
                                        {
                                            return false; // to change body use the fucking item , asshole
                                        }
                                    }

                                    if (usNewBody == 2001 || usNewBody == 2002)
                                    {
                                        if (user.Body != BodyType.AgileMale && user.Body != BodyType.MuscularMale)
                                        {
                                            return false; // to change body use the fucking item , asshole
                                        }
                                    }

                                    if (user.UserPackage[Item.ItemPosition.Garment] != null)
                                    {
                                        await user.UserPackage.UnEquipAsync(Item.ItemPosition.Garment);
                                    }

                                    user.Body = (BodyType)usNewBody;
                                    await user.SynchroAttributesAsync(ClientUpdateType.Mesh, user.Mesh, true);
                                    await user.SaveAsync();
                                    return true;
                                }
                        }

                        return false;
                    }

                #endregion

                #region Sex

                case "sex":
                    {
                        switch (opt)
                        {
                            case "set":
                                {
                                    BodyType newBody = user.Body;
                                    ushort usNewBody = ushort.Parse(value);
                                    if (usNewBody == 1)
                                    {
                                        if (user.Body != BodyType.AgileFemale && user.Body != BodyType.MuscularFemale)
                                        {
                                            return false;
                                        }

                                        if (user.Body == BodyType.AgileFemale)
                                        {
                                            newBody = BodyType.AgileMale;
                                        }
                                        else if (user.Body == BodyType.MuscularFemale)
                                        {
                                            newBody = BodyType.MuscularMale;
                                        }
                                    }

                                    if (usNewBody == 2)
                                    {
                                        if (user.Body != BodyType.AgileMale && user.Body != BodyType.MuscularMale)
                                        {
                                            return false;
                                        }

                                        if (user.Body == BodyType.AgileMale)
                                        {
                                            newBody = BodyType.AgileFemale;
                                        }
                                        else if (user.Body == BodyType.MuscularMale)
                                        {
                                            newBody = BodyType.MuscularFemale;
                                        }
                                    }

                                    if (user.UserPackage[Item.ItemPosition.Garment] != null)
                                    {
                                        await user.UserPackage.UnEquipAsync(Item.ItemPosition.Garment);
                                    }

                                    user.Body = newBody;
                                    await user.SynchroAttributesAsync(ClientUpdateType.Mesh, user.Mesh, true);
                                    await user.SaveAsync();
                                    return true;
                                    ;
                                }
                        }
                        return false;
                    }

                #endregion

                #region Cultivation

                case "cultivation":
                    {
                        int cultivationValue = int.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.StudyPoints > cultivationValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.StudyPoints >= cultivationValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.StudyPoints < cultivationValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.StudyPoints <= cultivationValue;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.StudyPoints == cultivationValue;
                        }

                        if (opt.Equals("+="))
                        {
                            return await user.ChangeCultivationAsync(cultivationValue);
                        }

                        break;
                    }

                #endregion

                #region Strength Value

                case "strengthvalue":
                    {
                        int strengthValue = int.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.ChiPoints > strengthValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.ChiPoints >= strengthValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.ChiPoints < strengthValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.ChiPoints <= strengthValue;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.ChiPoints == strengthValue;
                        }

                        if (opt.Equals("+="))
                        {
                            return await user.ChangeStrengthValueAsync(strengthValue);
                        }

                        break;
                    }

                #endregion

                #region Mentor

                case "mentor":
                    {
                        int mentorValue = int.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.EnlightenPoints > mentorValue;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.EnlightenPoints >= mentorValue;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.EnlightenPoints < mentorValue;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.EnlightenPoints <= mentorValue;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.EnlightenPoints == mentorValue;
                        }

                        if (opt.Equals("+="))
                        {
                            if (mentorValue > 0)
                            {
                                user.EnlightenPoints += (uint)mentorValue;
                            }
                            else if (mentorValue < 0)
                            {
                                if (mentorValue > user.EnlightenPoints)
                                {
                                    return false;
                                }

                                user.EnlightenPoints -= (uint)mentorValue;
                            }
                            else
                            {
                                break;
                            }

                            await user.SynchroAttributesAsync(ClientUpdateType.EnlightenPoints, user.EnlightenPoints, true);
                            return true;
                        }

                        break;
                    }

                #endregion

                #region Riding Point

                case "ridingpoint":
                    {
                        int ridePetPoint = int.Parse(value);
                        if (opt.Equals(">"))
                        {
                            return user.HorseRacingPoints > ridePetPoint;
                        }

                        if (opt.Equals(">="))
                        {
                            return user.HorseRacingPoints >= ridePetPoint;
                        }

                        if (opt.Equals("<"))
                        {
                            return user.HorseRacingPoints < ridePetPoint;
                        }

                        if (opt.Equals("<="))
                        {
                            return user.HorseRacingPoints <= ridePetPoint;
                        }

                        if (opt.Equals("=") || opt.Equals("=="))
                        {
                            return user.HorseRacingPoints == ridePetPoint;
                        }

                        if (opt.Equals("+="))
                        {
                            return await user.ChangeHorseRacePointsAsync(ridePetPoint);
                        }
                        break;
                    }

                #endregion

                default:
                    {
                        logger.LogWarning($"Unhandled {type} to ExecuteUserAttrAsync [{action.Id}]");
                        break;
                    }
            }

            return false;
        }

        private static async Task<bool> ExecuteUserFullAsync(DbAction action, string param, Character user, Role role,
                                                             Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            if (param.Equals("life", StringComparison.InvariantCultureIgnoreCase))
            {
                user.QueueAction(() => user.SetAttributesAsync(ClientUpdateType.Hitpoints, user.MaxLife));
                return true;
            }

            if (param.Equals("mana", StringComparison.InvariantCultureIgnoreCase))
            {
                user.QueueAction(() => user.SetAttributesAsync(ClientUpdateType.Mana, user.MaxMana));
                return true;
            }

            if (param.Equals("xp", StringComparison.InvariantCultureIgnoreCase))
            {
                await user.SetXpAsync(100);
                await user.BurstXpAsync();
                return true;
            }

            if (param.Equals("sp", StringComparison.InvariantCultureIgnoreCase))
            {
                await user.SetAttributesAsync(ClientUpdateType.Stamina, user.MaxEnergy);
                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteUserChgMapAsync(DbAction action, string param, Character user, Role role,
                                                               Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] paramStrings = SplitParam(param);
            if (paramStrings.Length < 3)
            {
                logger.LogWarning($"Action {action.Id}:{action.Type} invalid param length: {param}");
                return false;
            }

            if (!uint.TryParse(paramStrings[0], out uint idMap)
                || !ushort.TryParse(paramStrings[1], out ushort x)
                || !ushort.TryParse(paramStrings[2], out ushort y))
            {
                return false;
            }

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                logger.LogWarning($"Invalid map identity {idMap} for action {action.Id}");
                return false;
            }

            if (!user.Map.IsTeleportDisable() ||
                paramStrings.Length >= 4 && byte.TryParse(paramStrings[3], out byte forceTeleport) &&
                forceTeleport != 0)
            {
                return await user.FlyMapAsync(idMap, x, y);
            }

            return false;
        }

        private static async Task<bool> ExecuteUserRecordpointAsync(DbAction action, string param, Character user,
                                                                    Role role,
                                                                    Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] paramStrings = SplitParam(param);
            if (paramStrings.Length < 3)
            {
                logger.LogWarning(
                                        $"Action {action.Id}:{action.Type} invalid param length: {param}");
                return false;
            }

            if (!uint.TryParse(paramStrings[0], out uint idMap)
                || !ushort.TryParse(paramStrings[1], out ushort x)
                || !ushort.TryParse(paramStrings[2], out ushort y))
            {
                return false;
            }

            if (idMap == 0)
            {
                await user.SavePositionAsync(user.MapIdentity, user.X, user.Y);
                return true;
            }

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                logger.LogWarning($"Invalid map identity {idMap} for action {action.Id}");
                return false;
            }

            await user.SavePositionAsync(idMap, x, y);
            return true;
        }

        private static async Task<bool> ExecuteUserHairAsync(DbAction action, string param, Character user, Role role,
                                                             Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParams = SplitParam(param);

            if (splitParams.Length < 1)
            {
                logger.LogWarning(
                                        $"Action {action.Id}:{action.Type} has not enough argments: {param}");
                return false;
            }

            var cmd = "style";
            var value = 0;
            if (splitParams.Length > 1)
            {
                cmd = splitParams[0];
                value = int.Parse(splitParams[1]);
            }
            else
            {
                value = int.Parse(splitParams[0]);
            }

            if (cmd.Equals("style", StringComparison.InvariantCultureIgnoreCase))
            {
                await user.SetAttributesAsync(ClientUpdateType.HairStyle,
                                              (ushort)(value + (user.Hairstyle - user.Hairstyle % 100)));
                return true;
            }

            if (cmd.Equals("color", StringComparison.InvariantCultureIgnoreCase))
            {
                await user.SetAttributesAsync(ClientUpdateType.HairStyle,
                                              (ushort)(user.Hairstyle % 100 + value * 100));
                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteUserChgmaprecordAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            await user.FlyMapAsync(user.RecordMapIdentity, user.RecordMapX, user.RecordMapY);
            return true;
        }

        private static async Task<bool> ExecuteActionUserChglinkmapAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user?.Map == null)
            {
                return false;
            }

            if (user.IsPm())
            {
                await user.SendAsync("ExecuteActionUserChglinkmap");
            }

            return true;
        }

        private static async Task<bool> ExecuteUserTransformAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParam = SplitParam(param);

            if (splitParam.Length < 4)
            {
                logger.LogWarning(
                                        $"Invalid param count for {action.Id}:{action.Type}, {param}");
                return false;
            }

            uint transformation = uint.Parse(splitParam[2]);
            int time = int.Parse(splitParam[3]);
            return await user.TransformAsync(transformation, time, true);
        }

        private static async Task<bool> ExecuteActionUserIspureAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            return user.ProfessionSort == user.PreviousProfession / 10 &&
                   user.FirstProfession / 10 == user.ProfessionSort;
        }

        private static async Task<bool> ExecuteActionUserTalkAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            await user.SendAsync(param, (TalkChannel)action.Data, Color.White);
            return true;
        }

        private static async Task<bool> ExecuteActionUserMagicEffectAsync(DbAction action, string param, Character user,
                                                                    Role role,
                                                                    Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParams = SplitParam(param, 3);
            if (splitParams.Length < 3)
            {
                return false;
            }

            Magic magic = user.MagicData[(ushort)action.Data];
            if (magic == null)
            {
                return false;
            }

            var soulType = byte.Parse(splitParams[0]);
            var soulTypeFlag = (uint)(1 << soulType);
            var monopoly = uint.Parse(splitParams[1]);
            var monopolyFlag = 0u;
            if (monopoly != 0)
            {
                monopolyFlag = (uint)(1 << soulType);
            }
            var exorbitant = uint.Parse(splitParams[2]);
            var exorbitantFlag = 0u;
            if (exorbitant != 0)
            {
                exorbitantFlag = (uint)(1 << soulType);
            }

            if ((magic.AvailableEffectType & soulTypeFlag) == soulTypeFlag)
            {
                return false;
            }

            magic.AvailableEffectType |= soulTypeFlag;

            magic.CurrentEffectType = soulType;

            if (monopolyFlag != 0)
            {
                magic.EffectMonopoly |= monopolyFlag;
            }

            if (exorbitantFlag != 0)
            {
                magic.EffectExorbitant |= exorbitantFlag;
            }

            await magic.SaveAsync();
            await magic.SendAsync();
            await magic.SendAsync(MsgMagicInfo.MagicAction.AddEffectType);
            await magic.SendAsync(MsgMagicInfo.MagicAction.SetMagicEffect);
            return true;
        }

        private static async Task<bool> ExecuteActionUserMagicAsync(DbAction action, string param, Character user,
                                                                    Role role,
                                                                    Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 2)
            {
                logger.LogWarning(
                                        $"Invalid ActionUserMagic param length: {action.Id}, {param}");
                return false;
            }

            switch (splitParam[0].ToLowerInvariant())
            {
                case "check":
                    if (splitParam.Length >= 3)
                    {
                        return user.MagicData.CheckLevel(ushort.Parse(splitParam[1]), ushort.Parse(splitParam[2]));
                    }

                    return user.MagicData.CheckType(ushort.Parse(splitParam[1]));

                case "learn":
                    if (splitParam.Length >= 3)
                    {
                        return await user.MagicData.CreateAsync(ushort.Parse(splitParam[1]), byte.Parse(splitParam[2]));
                    }

                    return await user.MagicData.CreateAsync(ushort.Parse(splitParam[1]), 0);

                case "uplev":
                case "up_lev":
                case "uplevel":
                case "up_level":
                    return await user.MagicData.UpLevelByTaskAsync(ushort.Parse(splitParam[1]));

                case "addexp":
                    return await user.MagicData.AwardExpAsync(ushort.Parse(splitParam[1]), 0, int.Parse(splitParam[2]));

                default:
                    logger.LogWarning($"[ActionType: {action.Type}] Unknown {splitParam[0]} param {action.Id}");
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserWeaponSkillAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParam = SplitParam(param);

            if (splitParam.Length < 3)
            {
                logger.LogWarning($"Invalid param amount: {param} [{action.Id}]");
                return false;
            }

            if (!ushort.TryParse(splitParam[1], out ushort type)
                || !int.TryParse(splitParam[2], out int value))
            {
                logger.LogWarning(
                                        $"Invalid weapon skill type {param} for action {action.Id}");
                return false;
            }

            switch (splitParam[0].ToLowerInvariant())
            {
                case "check":
                    return user.WeaponSkill[type]?.Level >= value;

                case "learn":
                    return await user.WeaponSkill.CreateAsync(type, (byte)value);

                case "addexp":
                    await user.AddWeaponSkillExpAsync(type, value, true);
                    return true;

                default:
                    logger.LogWarning($"ExecuteActionUserWeaponSkill {splitParam[0]} invalid {action.Id}");
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserLogAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParam = SplitParam(param, 2);

            if (splitParam.Length < 2)
            {
                logger.LogWarning(
                                        $"ExecuteActionUserLog length [id:{action.Id}], {param}");
                return true;
            }

            string file = splitParam[0];
            string message = splitParam[1];

            if (file.StartsWith("gmlog/"))
            {
                file = file.Remove(0, "gmlog/".Length);
            }

            ILogger gmLogger = LogFactory.CreateGmLogger(file);
            gmLogger.LogInformation(message);
            return true;
        }

        private static async Task<bool> ExecuteActionUserBonusAsync(DbAction action, string param, Character user,
                                                                    Role role,
                                                                    Item item, string input)
        {
            if (user == null)
            {
                return false;
            }
            //return await user.DoBonusAsync();
            return false;
        }

        private static async Task<bool> ExecuteActionUserDivorceAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            if (user.MateIdentity == 0)
            {
                return false;
            }

            Character mate = RoleManager.GetUser(user.MateIdentity);
            if (mate == null)
            {
                DbCharacter dbMate = await CharacterRepository.FindByIdentityAsync(user.MateIdentity);
                if (dbMate == null)
                {
                    return false;
                }

                dbMate.Mate = 0;
                await ServerDbContext.SaveAsync(dbMate);

                DbItem dbItem = Item.CreateEntity(Item.TYPE_METEORTEAR);
                dbItem.PlayerId = user.Identity;
                await ServerDbContext.SaveAsync(dbItem);
            }
            else
            {
                mate.MateIdentity = 0;
                mate.MateName = StrNone;
                await mate.UserPackage.AwardItemAsync(Item.TYPE_METEORTEAR);

                await mate.SendAsync(new MsgName
                {
                    Action = StringAction.Mate,
                    Identity = mate.Identity,
                    Strings = new List<string>
                    {
                        mate.MateName
                    }
                });
            }

            user.MateIdentity = 0;
            user.MateName = StrNone;
            await user.SaveAsync();

            await user.SendAsync(new MsgName
            {
                Action = StringAction.Mate,
                Identity = user.Identity,
                Strings = new List<string>
                {
                    user.MateName
                }
            });
            return true;
        }

        private static async Task<bool> ExecuteActionUserMarriageAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            return user?.MateIdentity != 0;
        }

        private static async Task<bool> ExecuteActionUserSexAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            return user?.Gender == 1;
        }

        private static async Task<bool> ExecuteActionUserEffectAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] parsedString = SplitParam(param);
            if (parsedString.Length < 2)
            {
                logger.LogWarning($"Invalid parsed param[{param}] ExecuteActionUserEffect[{action.Id}]");
                return false;
            }

            var msg = new MsgName
            {
                Identity = user.Identity,
                Action = StringAction.RoleEffect
            };
            msg.Strings.Add(parsedString[1]);
            switch (parsedString[0].ToLower())
            {
                case "self":
                    await user.BroadcastRoomMsgAsync(msg, true);
                    return true;

                case "couple":
                    await user.BroadcastRoomMsgAsync(msg, true);

                    Character couple = RoleManager.GetUser(user.MateIdentity);
                    if (couple == null)
                    {
                        return true;
                    }

                    msg.Identity = couple.Identity;
                    await couple.BroadcastRoomMsgAsync(msg, true);
                    return true;

                case "team":
                    if (user.Team == null)
                    {
                        return false;
                    }

                    foreach (Character member in user.Team.Members)
                    {
                        msg.Identity = member.Identity;
                        await member.BroadcastRoomMsgAsync(msg, true);
                    }

                    return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserTaskmaskAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] parsedParam = SplitParam(param);
            if (parsedParam.Length < 2)
            {
                logger.LogWarning(
                                        $"ExecuteActionUserTaskmask invalid param num [{param}] for action {action.Id}");
                return false;
            }

            if (!int.TryParse(parsedParam[1], out int flag) || flag < 0 || flag >= 32)
            {
                logger.LogWarning($"ExecuteActionUserTaskmask invalid mask num {param}");
                return false;
            }

            switch (parsedParam[0].ToLower())
            {
                case "check":
                case "chk":
                    return user.CheckTaskMask(flag);
                case "add":
                    await user.AddTaskMaskAsync(flag);
                    return true;
                case "cls":
                case "clr":
                case "clear":
                    await user.ClearTaskMaskAsync(flag);
                    return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserMediaplayAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 2)
            {
                return false;
            }

            var msg = new MsgName 
            { 
                Action = StringAction.PlayerWave
            };
            msg.Strings.Add(pszParam[1]);

            switch (pszParam[0].ToLower())
            {
                case "play":
                    await user.SendAsync(msg);
                    return true;
                case "broadcast":
                    await user.BroadcastRoomMsgAsync(msg, true);
                    return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserCreatemapAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] safeParam = SplitParam(param);

            if (safeParam.Length < 10)
            {
                logger.LogWarning($"ExecuteActionUserCreatemap ({action.Id}) with invalid param length [{param}]");
                return false;
            }

            string szName = safeParam[0];
            uint idOwner = uint.Parse(safeParam[2]),
                 idRebornMap = uint.Parse(safeParam[7]);
            byte nOwnerType = byte.Parse(safeParam[1]);
            uint nMapDoc = uint.Parse(safeParam[3]);
            ulong nType = ulong.Parse(safeParam[4]);
            uint nRebornPortal = uint.Parse(safeParam[8]);
            byte nResLev = byte.Parse(safeParam[9]);
            ushort usPortalX = ushort.Parse(safeParam[5]),
                   usPortalY = ushort.Parse(safeParam[6]);

            var pMapInfo = new DbDynamap
            {
                Name = szName,
                OwnerIdentity = idOwner,
                OwnerType = nOwnerType,
                Description = $"{user.Name}`{szName}",
                RebornMap = idRebornMap,
                PortalX = usPortalX,
                PortalY = usPortalY,
                LinkMap = user.MapIdentity,
                LinkX = user.X,
                LinkY = user.Y,
                MapDoc = nMapDoc,
                Type = nType,
                RebornPortal = nRebornPortal,
                ResourceLevel = nResLev,
                ServerIndex = -1
            };

            if (!await ServerDbContext.SaveAsync(pMapInfo) || pMapInfo.Identity < 1000000)
            {
                logger.LogError($"ExecuteActionUserCreatemap error when saving map\n\t{JsonConvert.SerializeObject(pMapInfo)}");
                return false;
            }

            var map = new GameMap(pMapInfo);
            if (!await map.InitializeAsync())
            {
                return false;
            }

            user.HomeIdentity = pMapInfo.Identity;
            await user.SaveAsync();
            return await MapManager.AddMapAsync(map);
        }

        private static async Task<bool> ExecuteActionUserEnterHomeAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user == null || user.HomeIdentity == 0)
            {
                return false;
            }

            GameMap target = MapManager.GetMap(user.HomeIdentity);
            if (target == null)
            {
                logger.LogWarning($"User[{user.Identity}] is attempting to enter an house he doesn't have.");
                return false;
            }

            uint idMap = user.MapIdentity;
            int x = user.X;
            int y = user.Y;

            await user.FlyMapAsync(target.Identity, target.PortalX, target.PortalY);

            if (user.Team != null)
            {
                foreach (Character member in user.Team.Members)
                {
                    if (member.Identity == user.Identity)
                    {
                        continue;
                    }

                    if (member.MapIdentity != idMap || member.GetDistance(x, y) > 5)
                    {
                        continue;
                    }

                    await member.FlyMapAsync(target.Identity, target.PortalX, target.PortalY);
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserEnterMateHomeAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            uint idMap = 0;
            Character mate = RoleManager.GetUser(user.MateIdentity);
            if (mate == null)
            {
                DbCharacter dbMate = await CharacterRepository.FindByIdentityAsync(user.MateIdentity);
                idMap = dbMate.HomeIdentity;
            }
            else
            {
                idMap = mate.HomeIdentity;
            }

            if (idMap == 0)
            {
                return false;
            }

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                return false;
            }

            await user.FlyMapAsync(map.Identity, map.PortalX, map.PortalY);
            return true;
        }

        private static async Task<bool> ExecuteActionUserUnlearnMagicAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] magicsIds = SplitParam(param);

            foreach (string id in magicsIds)
            {
                ushort idMagic = ushort.Parse(id);
                if (user.MagicData.CheckType(idMagic))
                {
                    await user.MagicData.UnlearnMagicAsync(idMagic, false);
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserRebirthAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParam = SplitParam(param);

            if (!ushort.TryParse(splitParam[0], out ushort prof)
                || !ushort.TryParse(splitParam[1], out ushort look))
            {
                logger.LogWarning($"Invalid parameter to rebirth {param}, {action.Id}");
                return false;
            }

            return await user.RebirthAsync(prof, look);
        }

        private static async Task<bool> ExecuteActionUserWebpageAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            await user.SendAsync(param, TalkChannel.Website);
            return true;
        }

        private static async Task<bool> ExecuteActionUserBbsAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            await user.SendAsync(param, TalkChannel.Bbs);
            return true;
        }

        private static async Task<bool> ExecuteActionUserUnlearnSkillAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            return await user.UnlearnAllSkillAsync();
        }

        private static async Task<bool> ExecuteActionUserDropMagicAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] magicsIds = SplitParam(param);

            foreach (string id in magicsIds)
            {
                ushort idMagic = ushort.Parse(id);
                if (user.MagicData.CheckType(idMagic))
                {
                    await user.MagicData.UnlearnMagicAsync(idMagic, true);
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserOpenDialogAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            switch ((OpenWindow)action.Data)
            {
                case OpenWindow.VipWarehouse:
                    if (user.BaseVipLevel == 0)
                    {
                        return false;
                    }

                    break;
            }

            await user.SendAsync(new MsgAction
            {
                Action = MsgAction.ActionType.ClientDialog,
                Identity = user.Identity,
                Command = action.Data,
                ArgumentX = user.X,
                ArgumentY = user.Y
            });
            return true;
        }

        private static async Task<bool> ExecuteActionUserFixAttrAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            var attr = (ushort)(user.Speed + user.Vitality + user.Strength + user.Spirit + user.AttributePoints -
                                 10);
            ushort profSort = user.ProfessionSort;
            if (profSort == 13 || profSort == 14)
            {
                profSort = 10;
            }

            DbPointAllot pointAllot = ExperienceManager.GetPointAllot(profSort, 1);
            if (pointAllot != null)
            {
                await user.SetAttributesAsync(ClientUpdateType.Strength, pointAllot.Strength);
                await user.SetAttributesAsync(ClientUpdateType.Agility, pointAllot.Agility);
                await user.SetAttributesAsync(ClientUpdateType.Vitality, pointAllot.Vitality);
                await user.SetAttributesAsync(ClientUpdateType.Spirit, pointAllot.Spirit);
            }
            else
            {
                await user.SetAttributesAsync(ClientUpdateType.Strength, 5);
                await user.SetAttributesAsync(ClientUpdateType.Agility, 2);
                await user.SetAttributesAsync(ClientUpdateType.Vitality, 3);
                await user.SetAttributesAsync(ClientUpdateType.Spirit, 0);
            }

            await user.SetAttributesAsync(ClientUpdateType.Atributes, attr);
            return true;
        }

        private static async Task<bool> ExecuteActionUserExpMultiplyAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 2)
            {
                return false;
            }

            uint time = uint.Parse(pszParam[1]);
            float multiply = int.Parse(pszParam[0]) / 100f;
            await user.SetExperienceMultiplierAsync(time, multiply);
            return true;
        }

        private static async Task<bool> ExecuteActionUserWhPasswordAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            if (user.SecondaryPassword == 0)
            {
                return true;
            }

            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            if (input.Length < 1 || input.Length > ulong.MaxValue.ToString().Length)
            {
                return false;
            }

            if (!ulong.TryParse(input, out ulong password))
            {
                return false;
            }

            return user.SecondaryPassword == password;
        }

        private static async Task<bool> ExecuteActionUserSetWhPasswordAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            return user.IsUnlocked();
        }

        private static async Task<bool> ExecuteActionUserOpeninterfaceAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            await user.SendAsync(new MsgAction
            {
                Identity = user.Identity,
                Command = action.Data,
                Action = ActionType.ClientCommand,
                ArgumentX = user.X,
                ArgumentY = user.Y
            });
            return true;
        }

        private static async Task<bool> ExecuteActionUserTaskManagerAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user?.TaskDetail == null)
            {
                return false;
            }

            if (action.Data == 0)
            {
                return false;
            }

            switch (param.ToLowerInvariant())
            {
                case "new":
                    if (user.TaskDetail.QueryTaskData(action.Data) != null)
                    {
                        return false;
                    }

                    return await user.TaskDetail.CreateNewAsync(action.Data);
                case "isexit":
                    {
                        return user.TaskDetail.QueryTaskData(action.Data) != null;
                    }
                case "delete":
                    {
                        return await user.TaskDetail.DeleteTaskAsync(action.Data);
                    }
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserTaskOpeAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user?.TaskDetail == null)
            {
                return false;
            }

            if (action.Data == 0)
            {
                return false;
            }

            string[] splitParam = SplitParam(param, 3);
            if (splitParam.Length != 3)
            {
                return false;
            }

            string ope = splitParam[0].ToLowerInvariant(),
                   opt = splitParam[1].ToLowerInvariant();
            int data = int.Parse(splitParam[2]);

            if (ope.Equals("complete"))
            {
                if (opt.Equals("=="))
                {
                    return user.TaskDetail.QueryTaskData(action.Data)?.CompleteFlag == data;
                }

                if (opt.Equals("set"))
                {
                    return await user.TaskDetail.SetCompleteAsync(action.Data, data);
                }

                return false;
            }

            if (ope.StartsWith("data"))
            {
                switch (opt)
                {
                    case ">":
                        return user.TaskDetail.GetData(action.Data, ope) > data;
                    case "<":
                        return user.TaskDetail.GetData(action.Data, ope) < data;
                    case ">=":
                        return user.TaskDetail.GetData(action.Data, ope) >= data;
                    case "<=":
                        return user.TaskDetail.GetData(action.Data, ope) <= data;
                    case "==":
                        return user.TaskDetail.GetData(action.Data, ope) == data;
                    case "+=":
                        return await user.TaskDetail.AddDataAsync(action.Data, ope, data);
                    case "set":
                        return await user.TaskDetail.SetDataAsync(action.Data, ope, data);
                }

                return false;
            }

            if (ope.Equals("notify"))
            {
                DbTaskDetail detail = user.TaskDetail.QueryTaskData(action.Data);
                if (detail == null)
                {
                    return false;
                }

                detail.NotifyFlag = (byte)data;
                return await user.TaskDetail.SaveAsync(detail);
            }

            if (ope.Equals("overtime"))
            {
                DbTaskDetail detail = user.TaskDetail.QueryTaskData(action.Data);
                if (detail == null)
                {
                    return false;
                }

                detail.TaskOvertime = (uint)data;
                return await user.TaskDetail.SaveAsync(detail);
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserTaskLocaltimeAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.TaskDetail == null)
            {
                return false;
            }

            if (action.Data == 0)
            {
                return false;
            }

            string[] splitParam = SplitParam(param, 3);
            if (splitParam.Length != 3)
            {
                return false;
            }

            string ope = splitParam[0].ToLowerInvariant(),
                   opt = splitParam[1].ToLowerInvariant();
            int data = int.Parse(splitParam[2]);

            if (ope.StartsWith("interval", StringComparison.InvariantCultureIgnoreCase))
            {
                DbTaskDetail detail = user.TaskDetail.QueryTaskData(action.Data);
                if (detail == null)
                {
                    return true;
                }

                int mode = int.Parse(GetParenthesys(ope));
                switch (mode)
                {
                    case 0: // seconds
                        {
                            DateTime timeStamp = DateTime.Now;
                            var nDiff = (int)(timeStamp - UnixTimestamp.ToDateTime((int)detail.TaskOvertime)).TotalSeconds;
                            switch (opt)
                            {
                                case "==": return nDiff == data;
                                case "<": return nDiff < data;
                                case ">": return nDiff > data;
                                case "<=": return nDiff <= data;
                                case ">=": return nDiff >= data;
                                case "<>":
                                case "!=": return nDiff != data;
                            }

                            return false;
                        }

                    case 1: // days
                        int interval = (DateTime.Now.Date - UnixTimestamp.ToDateTime((int)detail.TaskOvertime)).Days;
                        switch (opt)
                        {
                            case "==": return interval == data;
                            case "<": return interval < data;
                            case ">": return interval > data;
                            case "<=": return interval <= data;
                            case ">=": return interval >= data;
                            case "!=":
                            case "<>": return interval != data;
                        }

                        return false;
                    default:
                        logger.LogWarning(
                                                $"Unhandled Time mode ({mode}) on action (id:{action.Id})");
                        return false;
                }
            }

            if (opt.Equals("set"))
            {
                DbTaskDetail detail = user.TaskDetail.QueryTaskData(action.Data);
                if (detail == null)
                {
                    return false;
                }

                if (data == 0)
                {
                    detail.TaskOvertime = (uint)UnixTimestamp.Now;
                }
                else
                {
                    detail.TaskOvertime = (uint)data;
                }
                return await user.TaskDetail.SaveAsync(detail);
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserTaskFindAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            logger.LogWarning("ExecuteActionUserTaskFind unhandled");
            return false;
        }

        private static async Task<bool> ExecuteActionUserVarCompareAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
            {
                return false;
            }

            byte varId = VarId(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);

            if (varId >= Role.MAX_VAR_AMOUNT)
            {
                return false;
            }

            switch (opt)
            {
                case "==":
                    return user.VarData[varId] == value;
                case ">=":
                    return user.VarData[varId] >= value;
                case "<=":
                    return user.VarData[varId] <= value;
                case ">":
                    return user.VarData[varId] > value;
                case "<":
                    return user.VarData[varId] < value;
                case "!=":
                    return user.VarData[varId] != value;
                default:
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserVarDefineAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] safeParam = SplitParam(param);
            if (safeParam.Length < 3)
            {
                return false;
            }

            byte varId = VarId(safeParam[0]);
            string opt = safeParam[1];
            long value = long.Parse(safeParam[2]);

            if (varId >= Role.MAX_VAR_AMOUNT)
            {
                return false;
            }

            try
            {
                switch (opt)
                {
                    case "set":
                        user.VarData[varId] = value;
                        return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserVarCompareStringAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param, 3);
            if (pszParam.Length < 3)
            {
                return false;
            }

            byte varId = VarId(pszParam[0]);
            string opt = pszParam[1];
            string value = pszParam[2];

            if (varId >= Role.MAX_VAR_AMOUNT)
            {
                return false;
            }

            switch (opt)
            {
                case "==":
                    return user.VarString[varId].Equals(value);
                default:
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserVarDefineStringAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param, 3);
            if (pszParam.Length < 3)
            {
                return false;
            }

            byte varId = VarId(pszParam[0]);
            string opt = pszParam[1];
            string value = pszParam[2];

            if (varId >= Role.MAX_VAR_AMOUNT)
            {
                return false;
            }

            switch (opt)
            {
                case "set":
                    user.VarString[varId] = value;
                    return true;
            }
            return false;
        }

        private static async Task<bool> ExecuteActionUserVarCalcAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            string[] safeParam = SplitParam(param);
            if (safeParam.Length < 3)
            {
                return false;
            }

            byte varId = VarId(safeParam[0]);
            string opt = safeParam[1];
            long value = long.Parse(safeParam[2]);

            if (opt == "/=" && value == 0)
            {
                return false; // division by zero
            }

            if (varId >= Role.MAX_VAR_AMOUNT)
            {
                return false;
            }

            switch (opt)
            {
                case "+=":
                    user.VarData[varId] += value;
                    return true;
                case "-=":
                    user.VarData[varId] -= value;
                    return true;
                case "*=":
                    user.VarData[varId] *= value;
                    return true;
                case "/=":
                    user.VarData[varId] /= value;
                    return true;
                case "mod=":
                    user.VarData[varId] %= value;
                    return true;
                default:
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserTestEquipmentAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParams = SplitParam(param, 2);
            if (!ushort.TryParse(splitParams[0], out ushort pos)
                || !int.TryParse(splitParams[1], out int type))
            {
                logger.LogWarning($"Invalid parsed param ExecuteActionUserTestEquipment, id[{action.Id}]");
                return false;
            }

            if (!Enum.IsDefined(typeof(Item.ItemPosition), pos))
            {
                return false;
            }

            Item temp = user.UserPackage[(Item.ItemPosition)pos];
            if (temp == null)
            {
                return false;
            }

            return temp.GetItemSubType() == type;
        }

        private static async Task<bool> ExecuteActionUserDailyStcCompareAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
            {
                return false;
            }

            string szStc = GetParenthesys(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);

            string[] pStc = szStc.Trim().Split(',');

            if (pStc.Length < 2)
            {
                return false;
            }

            uint idEvent = uint.Parse(pStc[0]);
            uint idType = uint.Parse(pStc[1]);

            DbStatisticDaily dbStc = user.Statistic.GetDailyStc(idEvent, idType);
            long data = dbStc?.Data ?? 0;
            switch (opt)
            {
                case ">=":
                    return data >= value;
                case "<=":
                    return data <= value;
                case ">":
                    return data > value;
                case "<":
                    return data < value;
                case "!=":
                case "<>":
                    return data != value;
                case "=":
                case "==":
                    return data == value;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserDailyStcOpeAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
            {
                return false;
            }

            string szStc = GetParenthesys(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);

            string[] pStc = szStc.Trim().Split(',');

            if (pStc.Length < 2)
            {
                return false;
            }

            uint idEvent = uint.Parse(pStc[0]);
            uint idType = uint.Parse(pStc[1]);

            if (!user.Statistic.HasDailyEvent(idEvent, idType))
            {
                return await user.Statistic.AddOrUpdateDailyAsync(idEvent, idType, (uint)value);
            }

            switch (opt)
            {
                case "+=":
                    {
                        if (value == 0)
                        {
                            return false;
                        }

                        long tempValue = user.Statistic.GetDailyValue(idEvent, idType) + value;
                        return await user.Statistic.AddOrUpdateDailyAsync(idEvent, idType, (uint)Math.Max(0, tempValue));
                    }
                case "=":
                case "set":
                    {
                        if (value < 0)
                        {
                            return false;
                        }

                        return await user.Statistic.AddOrUpdateDailyAsync(idEvent, idType, (uint)Math.Max(0, value));
                    }
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserExecActionAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParams = SplitParam(param, 3);
            if (splitParams.Length < 3)
            {
                return false;
            }

            if (!int.TryParse(splitParams[0], out int secSpan)
                || !uint.TryParse(splitParams[1], out uint idAction))
            {
                return false;
            }

            EventManager.QueueAction(new QueuedAction(secSpan, idAction, user.Identity));
            return true;
        }

        private static async Task<bool> ExecuteActionUserStcCompareAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
            {
                return false;
            }

            string szStc = GetParenthesys(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);

            string[] pStc = szStc.Trim().Split(',');

            if (pStc.Length < 2)
            {
                return false;
            }

            uint idEvent = uint.Parse(pStc[0]);
            uint idType = uint.Parse(pStc[1]);

            DbStatistic dbStc = user.Statistic.GetStc(idEvent, idType);
            long data = dbStc?.Data ?? 0;
            switch (opt)
            {
                case ">=":
                    return data >= value;
                case "<=":
                    return data <= value;
                case ">":
                    return data > value;
                case "<":
                    return data < value;
                case "!=":
                case "<>":
                    return data != value;
                case "=":
                case "==":
                    return data == value;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserStcOpeAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
            {
                return false;
            }

            string szStc = GetParenthesys(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);
            bool bUpdate = pszParam[3] != "0";

            string[] pStc = szStc.Trim().Split(',');

            if (pStc.Length < 2)
            {
                return false;
            }

            uint idEvent = uint.Parse(pStc[0]);
            uint idType = uint.Parse(pStc[1]);

            if (!user.Statistic.HasEvent(idEvent, idType))
            {
                return await user.Statistic.AddOrUpdateAsync(idEvent, idType, (uint)value, bUpdate);
            }

            switch (opt)
            {
                case "+=":
                    if (value == 0)
                    {
                        return false;
                    }

                    long tempValue = user.Statistic.GetValue(idEvent, idType) + value;
                    return await user.Statistic.AddOrUpdateAsync(idEvent, idType, (uint)Math.Max(0, tempValue),
                                                                 bUpdate);
                case "=":
                case "set":
                    if (value < 0)
                    {
                        return false;
                    }

                    return await user.Statistic.AddOrUpdateAsync(idEvent, idType, (uint)Math.Max(0, value), bUpdate);
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserDataSyncAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3)
            {
                return false;
            }

            string act = splitParam[0];
            uint type = uint.Parse(splitParam[1]);
            uint data = uint.Parse(splitParam[2]);

            if (act.Equals("send"))
            {
                await user.SendAsync(new MsgAction
                {
                    Identity = user.Identity,
                    Action = (ActionType)type,
                    Command = data,
                    ArgumentX = user.X,
                    ArgumentY = user.Y,
                    X = user.X,
                    Y = user.Y
                });
                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserSelectToDataAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            try
            {
                user.VarData[action.Data] = long.Parse(await ServerDbContext.ScalarAsync(param));
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error executing select to data", ex.Message);
                return false;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserStcTimeCheckAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Statistic == null)
            {
                return false;
            }

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
            {
                return false;
            }

            string szStc = GetParenthesys(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);

            string[] pStc = szStc.Trim().Split(',');

            if (pStc.Length <= 2)
            {
                return false;
            }

            uint idEvent = uint.Parse(pStc[0]);
            uint idType = uint.Parse(pStc[1]);
            byte mode = byte.Parse(pStc[2]);

            if (value < 0)
            {
                return false;
            }

            DbStatistic dbStc = user.Statistic.GetStc(idEvent, idType);
            if (dbStc?.Timestamp == null)
            {
                return true;
            }

            var currentStcTimestamp = UnixTimestamp.ToDateTime(dbStc.Timestamp);

            switch (mode)
            {
                case 0: // seconds
                    {
                        DateTime timeStamp = DateTime.Now;
                        var nDiff = (int)(timeStamp - currentStcTimestamp).TotalSeconds;
                        switch (opt)
                        {
                            case "==": return nDiff == value;
                            case "<": return nDiff < value;
                            case ">": return nDiff > value;
                            case "<=": return nDiff <= value;
                            case ">=": return nDiff >= value;
                            case "<>":
                            case "!=": return nDiff != value;
                        }

                        return false;
                    }

                case 1: // days
                    int interval = int.Parse(DateTime.Now.ToString("yyyyMMdd")) -
                                   int.Parse(currentStcTimestamp.ToString("yyyyMMdd"));
                    switch (opt)
                    {
                        case "==": return interval == value;
                        case "<": return interval < value;
                        case ">": return interval > value;
                        case "<=": return interval <= value;
                        case ">=": return interval >= value;
                        case "!=":
                        case "<>": return interval != value;
                    }

                    return false;
                default:
                    logger.LogWarning(
                                            $"Unhandled Time mode ({mode}) on action (id:{action.Id})");
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserStcTimeOpeAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user?.Statistic == null)
            {
                return false;
            }

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
            {
                return false;
            }

            string szStc = GetParenthesys(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);

            string[] pStc = szStc.Trim().Split(',');

            uint idEvent = uint.Parse(pStc[0]);
            uint idType = uint.Parse(pStc[1]);

            switch (opt)
            {
                case "set":
                    {
                        if (value > 0)
                        {
                            return await user.Statistic.SetTimestampAsync(idEvent, idType, DateTime.Now);
                        }

                        return await user.Statistic.SetTimestampAsync(idEvent, idType, DateTime.Now);
                    }
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserAttachStatusAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            // self add 64 200 900 0
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 6)
            {
                return false;
            }

            string target = pszParam[0].ToLower();
            string opt = pszParam[1].ToLower();
            int status = StatusSet.GetRealStatus(int.Parse(pszParam[2]));
            int multiply = int.Parse(pszParam[3]);
            uint seconds = uint.Parse(pszParam[4]);
            int times = int.Parse(pszParam[5]);
            // last param unknown

            if (target == "team" && user.Team == null)
            {
                return false;
            }

            if (target == "self")
            {
                if (opt == "add")
                {
                    await user.AttachStatusAsync(user, status, multiply, (int)seconds, times, null);
                }
                else if (opt == "del")
                {
                    await user.DetachStatusAsync(status);
                }

                return true;
            }

            if (target == "team")
            {
                foreach (Character member in user.Team.Members)
                {
                    if (opt == "add")
                    {
                        await member.AttachStatusAsync(member, status, multiply, (int)seconds, times, null);
                    }
                    else if (opt == "del")
                    {
                        await member.DetachStatusAsync(status);
                    }
                }

                return true;
            }

            if (target == "couple")
            {
                Character mate = RoleManager.GetUser(user.MateIdentity);
                if (mate == null)
                {
                    return false;
                }

                if (opt == "add")
                {
                    await user.AttachStatusAsync(user, status, multiply, (int)seconds, times, null);
                    await mate.AttachStatusAsync(user, status, multiply, (int)seconds, times, null);
                }
                else if (opt == "del")
                {
                    await user.DetachStatusAsync(status);
                    await mate.DetachStatusAsync(status);
                }

                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserGodTimeAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            string[] pszParma = SplitParam(param);

            if (pszParma.Length < 2)
            {
                return false;
            }

            string opt = pszParma[0];
            int value = int.Parse(pszParma[1]);

            switch (opt)
            {
                case "+=":
                    {
                        return await user.AddBlessingAsync((uint)value);
                    }
                case "chk":
                    {
                        if (value == 1)
                        {
                            return user.QueryStatus(StatusSet.CURSED) != null;
                        }
                        return true;
                    }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserCalExpAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParams = SplitParam(param);
            if (splitParams.Length < 3)
            {
                logger.LogWarning("Invalid num of params to ExecuteActionUserCalExpAsync: {}", param);
                return false;
            }

            ulong experience = ulong.Parse(splitParams[0]);
            int newLevelVar = int.Parse(splitParams[1]);
            int percentualVar = int.Parse(splitParams[2]);

            if (newLevelVar < 0 || newLevelVar >= user.VarData.Length)
            {
                logger.LogWarning("Index to new level must be valid range for user var");
                return false;
            }

            if (percentualVar < 0 || percentualVar >= user.VarData.Length)
            {
                logger.LogWarning("Index to new percentual exp must be valid range for user var");
                return false;
            }

            ExperiencePreview experiencePreview = user.PreviewExperienceIncrement(experience);
            user.VarData[newLevelVar] = experiencePreview.Level;
            user.VarData[percentualVar] = (long)experiencePreview.Percent;
            return true;
        }

        private static async Task<bool> ExecuteActionUserPureProfessionalAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            if (user.Metempsychosis < 2)
            {
                return false;
            }

            int profession = user.ProfessionSort * 10;
            int professionPrevious = user.PreviousProfession / 10 * 10;
            int professionFirst = user.FirstProfession / 10 * 10;
            return action.Data == profession && action.Data == professionPrevious && action.Data == professionFirst;
        }


        private static async Task<bool> ExecuteActionUserExpballExpAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 2)
            {
                return false;
            }

            int dwExpTimes = int.Parse(pszParam[0]);
            byte idData = byte.Parse(pszParam[1]);

            if (idData >= user.VarData.Length)
            {
                return false;
            }

            long exp = user.CalculateExpBall(dwExpTimes);
            user.VarData[idData] = exp;
            return true;
        }

        private static async Task<bool> ExecuteActionUserStatusCreateAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            // sort leave_times remain_time end_time interval_time
            // 200 0 604800 0 604800 1
            if (action.Data == 0)
            {
                logger.LogWarning($"ERROR: invalid data num {action.Id}");
                return false;
            }

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 5)
            {
                logger.LogWarning($"ERROR: invalid param num {action.Id}");
                return false;
            }

            int power = int.Parse(pszParam[0]);
            int leaveTimes = int.Parse(pszParam[1]);
            int remainTime = int.Parse(pszParam[2]);
            int intervalTime = int.Parse(pszParam[4]);
            bool save = pszParam[5] != "0"; // ??

            switch (action.Data)
            {
                case 8330:
                case 8331:
                case 8332:
                case 8333:
                case 8334:
                case 8335:
                case 8336:
                case 8337:
                case 8338:
                case 8339:
                case 8340:
                    {
                        if (!user.Map.IsRaceTrack())
                        {
                            return false;
                        }

                        if (await user.AwardRaceItemAsync((HorseRacing.ItemType)action.Data))
                        {
                            await user.AttachStatusAsync(user, (int)action.Data, power, int.MaxValue, 0, null, save);
                        }
                        break;
                    }
                default:
                    {
                        await user.AttachStatusAsync(user, StatusSet.GetRealStatus((int)action.Data), power, remainTime, leaveTimes, null, save);
                        break;
                    }
            }
            return true;
        }

        private static async Task<bool> ExecuteActionUserStatusCheckAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user?.StatusSet == null)
            {
                return false;
            }

            string[] status = SplitParam(param);
            List<int> statuses = new();
            foreach (var stt in status)
            {
                int realStatus = StatusSet.GetRealStatus(int.Parse(stt));
                statuses.Add(realStatus);
            }

            switch (action.Data)
            {
                case 0: // check
                    foreach (var st in statuses)
                    {
                        if (user.QueryStatus(st) == null)
                        {
                            return false;
                        }
                    }

                    return true;

                case 1:
                    foreach (var st in statuses)
                    {
                        if (user.QueryStatus(st) != null)
                        {
                            await user.DetachStatusAsync(st);
                            DbStatus db = (await StatusRepository.GetAsync(user.Identity)).FirstOrDefault(x => x.Status == st);
                            if (db != null)
                            {
                                await ServerDbContext.DeleteAsync(db);
                            }
                        }
                    }

                    return true;
            }

            return false;
        }

        #endregion

        #region Team 1100-1199

        private static async Task<bool> ExecuteActionTeamBroadcastAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.Team == null || user.Team.MemberCount < 1)
            {
                logger.LogWarning($"ExecuteActionTeamBroadcast user or team is null {action.Id}");
                return false;
            }

            if (!user.Team.IsLeader(user.Identity))
            {
                return false;
            }

            await user.Team.SendAsync(new MsgTalk(user.Identity, TalkChannel.Team, Color.White, param));
            return true;
        }

        private static async Task<bool> ExecuteActionTeamAttrAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item, string input)
        {
            if (user?.Team == null)
            {
                logger.LogWarning($"ExecuteActionTeamAttr user or team is null {action.Id}");
                return false;
            }

            string[] splitParams = SplitParam(param, 3);
            if (splitParams.Length < 3) // invalid param num
            {
                return false;
            }

            string cmd = splitParams[0].ToLower();
            string opt = splitParams[1];
            long.TryParse(splitParams[2], out long value);

            if (cmd.Equals("count"))
            {
                if (opt.Equals("<"))
                {
                    return user.Team.MemberCount < value;
                }

                if (opt.Equals("=="))
                {
                    return user.Team.MemberCount == value;
                }
            }

            foreach (Character member in user.Team.Members)
            {
                if (cmd.Equals("money"))
                {
                    if (opt.Equals("+="))
                    {
                        await member.ChangeMoneyAsync(value);
                    }
                    else if (opt.Equals("<"))
                    {
                        return member.Silvers < (ulong)value;
                    }
                    else if (opt.Equals("=="))
                    {
                        return member.Silvers == (ulong)value;
                    }
                    else if (opt.Equals(">"))
                    {
                        return member.Silvers > (ulong)value;
                    }
                }
                else if (cmd.Equals("emoney"))
                {
                    if (opt.Equals("+="))
                    {
                        await member.ChangeConquerPointsAsync((int)value);
                    }
                    else if (opt.Equals("<"))
                    {
                        return member.ConquerPoints < value;
                    }
                    else if (opt.Equals("=="))
                    {
                        return member.ConquerPoints == value;
                    }
                    else if (opt.Equals(">"))
                    {
                        return member.ConquerPoints > value;
                    }
                }
                else if (cmd.Equals("level"))
                {
                    if (opt.Equals("<"))
                    {
                        return member.Level < value;
                    }

                    if (opt.Equals("=="))
                    {
                        return member.Level == value;
                    }

                    if (opt.Equals(">"))
                    {
                        return member.Level > value;
                    }
                }
                else if (cmd.Equals("vip"))
                {
                    if (opt.Equals("<"))
                    {
                        return member.BaseVipLevel < value;
                    }

                    if (opt.Equals("=="))
                    {
                        return member.BaseVipLevel == value;
                    }

                    if (opt.Equals(">"))
                    {
                        return member.BaseVipLevel > value;
                    }
                }
                else if (cmd.Equals("mate"))
                {
                    if (member.Identity == user.Identity)
                    {
                        continue;
                    }

                    if (member.MateIdentity != user.Identity)
                    {
                        return false;
                    }
                }
                else if (cmd.Equals("friend"))
                {
                    if (member.Identity == user.Identity)
                    {
                        continue;
                    }

                    if (!user.IsFriend(member.Identity))
                    {
                        return false;
                    }
                }
                else if (cmd.Equals("count_near"))
                {
                    if (member.Identity == user.Identity)
                    {
                        continue;
                    }

                    if (!(member.MapIdentity == user.MapIdentity && member.IsAlive))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionTeamLeavespaceAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                logger.LogWarning($"ExecuteActionTeamLeavespace user or team is null {action.Id}");
                return false;
            }

            if (!int.TryParse(param, out int space))
            {
                return false;
            }

            foreach (Character member in user.Team.Members)
            {
                if (!member.UserPackage.IsPackSpare(space))
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionTeamItemAddAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                logger.LogWarning($"ExecuteActionTeamItemAdd user or team is null {action.Id}");
                return false;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionTeamItemDelAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                logger.LogWarning($"ExecuteActionTeamItemDel user or team is null {action.Id}");
                return false;
            }

            foreach (Character member in user.Team.Members)
            {
                await member.UserPackage.AwardItemAsync(action.Data);
            }

            return true;
        }

        private static async Task<bool> ExecuteActionTeamItemCheckAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                logger.LogWarning($"ExecuteActionTeamItemCheck user or team is null {action.Id}");
                return false;
            }

            foreach (Character member in user.Team.Members)
            {
                await member.UserPackage.SpendItemAsync(action.Data);
            }

            return true;
        }

        private static async Task<bool> ExecuteActionTeamChgmapAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                logger.LogWarning($"ExecuteActionTeamChgmap user or team is null {action.Id}");
                return false;
            }

            foreach (Character member in user.Team.Members)
            {
                if (member.UserPackage.GetItemByType(action.Data) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionTeamChkIsleaderAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                logger.LogWarning($"ExecuteActionTeamChkIsleader user or team is null {action.Id}");
                return false;
            }

            return user.Team.IsLeader(user.Identity);
        }

        private static async Task<bool> ExecuteActionTeamCreateDynamapAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user == null)
            {
                logger.LogWarning($"ExecuteActionTeamCreateDynamapAsync user is null {action.Id}");
                return false;
            }

            DbInstanceType instanceType = InstanceTypeRepository.Get(action.Data);
            if (instanceType == null)
            {
                logger.LogWarning($"Could not find instance type {action.Data}");
                return false;
            }

            GameMap baseGameMap = MapManager.GetMap(instanceType.MapId);
            if (baseGameMap == null)
            {
                logger.LogWarning($"Base map not found for instance [{instanceType.Name}] type [{action.Data}] map [{instanceType.MapId}]");
                return false;
            }

            InstanceMap currentInstance = MapManager.FindInstanceByUser(action.Data, user.Identity);
            if (currentInstance == null)
            {
                var dynamicMap = new DbDynamap
                {
                    Identity = (uint)IdentityManager.Instances.GetNextIdentity,
                    Name = instanceType.Name,
                    Description = $"{user.Name}`s map",
                    Type = (uint)baseGameMap.Type,
                    OwnerIdentity = user.Identity,
                    LinkMap = user.MapIdentity,
                    LinkX = user.X,
                    LinkY = user.Y,
                    MapDoc = baseGameMap.MapDoc,
                    OwnerType = 1
                };

                currentInstance = new InstanceMap(dynamicMap, instanceType)
                {
                    BaseMapId = instanceType.MapId
                };

                if (!await currentInstance.InitializeAsync())
                {
                    logger.LogError($"Could not initialize instance!");
                    return false;
                }

                var npcs = baseGameMap.QueryRoles().Where(x => x is BaseNpc).Cast<BaseNpc>();
                foreach (var npc in npcs)
                {
                    await currentInstance.AddAsync(npc);
                }

                await MapManager.AddMapAsync(currentInstance);
            }

            uint requestId = user.MapIdentity;
            ushort x = user.X;
            ushort y = user.Y;

            List<Character> targets = new();
            if (user.Team != null)
            {
                foreach (var member in user.Team.Members)
                {
                    if (member.MapIdentity != requestId) continue;
                    if (member.GetDistance(x, y) > Screen.VIEW_SIZE) continue;
                    targets.Add(member);
                }
            }
            else
            {
                targets.Add(user);
            }

            foreach (Character target in targets)
            {
                Point pos = await currentInstance.QueryRandomPositionAsync();
                await target.FlyMapAsync(currentInstance.Identity, pos.X, pos.Y);
            }
            return true;
        }

        #endregion

        #region ??? 1200-1299

        private static async Task<bool> ExecuteActionFrozenGrottoEntranceChkDaysAsync(DbAction action, string param, Character user,
                                                                    Role role, Item item, string input)
        {
            if (user == null)
            {
                logger.LogWarning($"Invalid entity for action type {action.Type} [{action.Id}]");
                return false;
            }

            int days = int.Parse(param);
            DbVipMineTime vipMineTime = await VipMineTimeRepository.GetAsync(user.Identity);
            if (vipMineTime == null)
            {
                vipMineTime = new DbVipMineTime
                {
                    UserId = user.Identity
                };
            }
            else
            {
                int today = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
                int lastEntrance = int.Parse(UnixTimestamp.ToDateTime((int)vipMineTime.LastEnterTime).ToString("yyyyMMdd"));
                if (today - lastEntrance < days)
                {
                    return false;
                }
            }

            vipMineTime.LastEnterTime = (uint)UnixTimestamp.Now;
            await ServerDbContext.SaveAsync(vipMineTime);
            return true;
        }

        private static async Task<bool> ExecuteActionUserCheckHpFullAsync(DbAction action, string param, Character user,
                                                                    Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            return user.Life >= user.MaxLife;
        }

        private static async Task<bool> ExecuteActionUserCheckHpManaFullAsync(DbAction action, string param, Character user,
                                                                    Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            return user.Mana >= user.MaxMana;
        }

        #endregion

        #region Elite PK 1300-1309

        /**
         * ActionElitePKValidateUser = 1301,
         * ActionElitePKUserInscribed = 1302,
         * ActionElitePKCheck = 1303,
         */

        private static async Task<bool> ExecuteActionElitePKValidateUserAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
            {
                logger.LogWarning($"Invalid user entity for action {action.Id}, {action.Type}");
                return false;
            }

            ElitePkTournament elitePk = EventManager.GetEvent<ElitePkTournament>();
            if (elitePk == null)
            {
                logger.LogWarning($"Trying to fetch invalid event ElitePkTournament.");
                return false;
            }

            return elitePk.IsAllowedToJoin(user, (int)action.Data);
        }

        private static async Task<bool> ExecuteActionElitePKUserInscribedAsync(DbAction action, string param, Character user, Role role,
           Item item, string input)
        {
            if (user == null)
            {
                logger.LogWarning($"Invalid user entity for action {action.Id}, {action.Type}");
                return false;
            }

            ElitePkTournament elitePk = EventManager.GetEvent<ElitePkTournament>();
            if (elitePk == null)
            {
                logger.LogWarning($"Trying to fetch invalid event ElitePkTournament.");
                return false;
            }

            return elitePk.IsActive;
        }

        private static async Task<bool> ExecuteActionElitePKCheckAsync(DbAction action, string param, Character user, Role role,
           Item item, string input)
        {
            if (user == null)
            {
                logger.LogWarning($"Invalid user entity for action {action.Id}, {action.Type}");
                return false;
            }

            ElitePkTournament elitePk = EventManager.GetEvent<ElitePkTournament>();
            if (elitePk == null)
            {
                logger.LogWarning($"Trying to fetch invalid event ElitePkTournament.");
                return false;
            }

            return true;
        }

        #endregion

        #region Team PK 1310-1319

        #endregion

        #region Skill Team PK 1320-1329

        #endregion

        #region General 1500-1999

        private static async Task<bool> ExecuteGeneralLotteryAsync(DbAction action, string param, Character user, Role role,
            Item item,
            string input)
        {
            if (user == null)
            {
                logger.LogError($"No user for ExecuteGeneralLottery, {action.Id}");
                return false;
            }

            if (user.Metempsychosis < 2)
            {
                await user.UserPackage.AwardItemAsync(Item.SMALL_LOTTERY_TICKET);
                await user.UserPackage.AwardItemAsync(Item.SMALL_LOTTERY_TICKET);
                await user.UserPackage.AwardItemAsync(Item.SMALL_LOTTERY_TICKET);
                await user.SendAsync(StrNotEnoughLevel);
                return false;
            }

            var stc = user.Statistic.GetStc(22);
            if (stc != null)
            {
                int maxAttempts = LotteryManager.GetMaxAttempts(user);
                bool lastAttemptToday = UnixTimestamp.ToDateTime(stc.Timestamp).Date == DateTime.Now.Date;
                if (lastAttemptToday && stc.Data >= maxAttempts)
                {
                    await user.UserPackage.AwardItemAsync(Item.SMALL_LOTTERY_TICKET);
                    await user.UserPackage.AwardItemAsync(Item.SMALL_LOTTERY_TICKET);
                    await user.UserPackage.AwardItemAsync(Item.SMALL_LOTTERY_TICKET);
                    await user.SendAsync(StrLotteryLimit);
                    return false;
                }
                else if (!lastAttemptToday || stc.Data >= maxAttempts)
                {
                    await user.Statistic.AddOrUpdateAsync(22, 0, 0, true);
                }
            }

            await LotteryManager.QueryPrizeAsync(user, (int)action.Data, false);
            await user.UpdateTaskActivityAsync(ActivityManager.ActivityType.Lottery);
            return true;
        }

        private static async Task<bool> ExecuteActionChgMapSquareAsync(DbAction action, string param, Character user,
            Role role, Item item,
            string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParams = SplitParam(param, 6);
            if (!uint.TryParse(splitParams[0], out uint idMap))
            {
                logger.LogWarning("Invalid map id for ExecuteActionChgMapSquareAsync [{}]", param);
                return false;
            }

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                logger.LogWarning($"GameAction::ExecuteActionChgMapSquare invalid map {idMap}");
                return false;
            }

            ushort x, y, cx, cy;
            if (splitParams.Length >= 5)
            {
                if (!ushort.TryParse(splitParams[1], out x)
                    || !ushort.TryParse(splitParams[2], out y)
                    || !ushort.TryParse(splitParams[3], out cx)
                    || !ushort.TryParse(splitParams[4], out cy))
                {
                    return false;
                }
            }
            else
            {
                x = 0;
                y = 0;
                cx = (ushort)map.Width;
                cy = (ushort)map.Height;
            }

            int saveLocation = 0;
            if (splitParams.Length > 5)
            {
                int.TryParse(splitParams[5], out saveLocation);
            }

            Point point = await map.QueryRandomPositionAsync(x, y, cx, cy);
            if (!point.Equals(default))
            {
                await user.FlyMapAsync(idMap, point.X, point.Y);
                if (saveLocation != 0)
                {
                    await user.SavePositionAsync(idMap, (ushort)point.X, (ushort)point.Y);
                }
                return true;
            }
            return false;
        }

        private static async Task<bool> ExecuteActionUserDecLifeAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null || !user.IsAlive)
            {
                return false;
            }

            if (!int.TryParse(param, out var percent))
            {
                return false;
            }

            switch (action.Data)
            {
                case 0:
                    {
                        int reduceLife = (int)(user.MaxLife * percent / 100d);
                        await user.SetAttributesAsync(ClientUpdateType.Hitpoints, (ulong)Math.Max(1, user.Life - reduceLife));
                        return true;
                    }
                case 1:
                    {
                        int reduceLife = (int)(user.Life * percent / 100d);
                        await user.SetAttributesAsync(ClientUpdateType.Hitpoints, (ulong)Math.Max(1, user.Life - reduceLife));
                        return true;
                    }
            }
            return false;
        }

        private static async Task<bool> ExecuteActionSubclassLearnAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user?.AstProf == null)
            {
                logger.LogWarning($"Action {action.Id} called without an actor");
                return false;
            }

            if (!int.TryParse(param, out var intType))
            {
                logger.LogWarning($"Action {action.Id} invalid param");
                return false;
            }

            AstProfType type = (AstProfType)intType;
            return await user.AstProf.LearnAsync(type, true);
        }

        private static async Task<bool> ExecuteActionSubclassLevelAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user?.AstProf == null)
            {
                logger.LogWarning($"Action {action.Id} called without an actor");
                return false;
            }

            string[] splitParams = SplitParam(param, 3);
            if (splitParams.Length < 3)
            {
                logger.LogWarning($"Action {action.Id} param '{param}' needs at least 3 params");
                return false;
            }

            AstProfType type = (AstProfType)int.Parse(splitParams[0]);
            string opt = splitParams[1];
            int level = int.Parse(splitParams[2]);

            if (opt.Equals("<"))
            {
                return user.AstProf.GetLevel(type) < level;
            }
            else if (opt.Equals("=="))
            {
                return user.AstProf.GetLevel(type) == level;
            }

            logger.LogWarning($"Invalid operation for action {action.Id}! Operation: {opt}");
            return false;
        }

        private static async Task<bool> ExecuteActionSubclassPromotionAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user?.AstProf == null)
            {
                logger.LogWarning($"Action {action.Id} called without an actor");
                return false;
            }

            string[] splitParams = SplitParam(param, 3);
            if (splitParams.Length < 3)
            {
                logger.LogWarning($"Action {action.Id} param '{param}' needs at least 3 params");
                return false;
            }

            AstProfType type = (AstProfType)int.Parse(splitParams[0]);
            string opt = splitParams[1];
            int rank = int.Parse(splitParams[2]);

            if (opt.Equals("<"))
            {
                return user.AstProf.GetPromotion(type) < rank;
            }
            else if (opt.Equals("=="))
            {
                return user.AstProf.GetPromotion(type) == rank;
            }
            else if (opt.Equals("set"))
            {
                return await user.AstProf.PromoteAsync(type, rank);
            }
            logger.LogWarning($"Invalid operation for action {action.Id}! Operation: {opt}");
            return false;
        }

        private static async Task<bool> ExecuteActionOpenShopAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            await user.SendAsync(new MsgAction
            {
                Identity = user.Identity,
                Command = 32,
                Action = ActionType.OpenShop,
                Timestamp = role.Identity
            });
            return true;
        }

        private static async Task<bool> ExecuteActionJiangHuInscribedAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                logger.LogWarning($"ExecuteActionJiangHuInscribedAsync[{action.Id}] called with no actor");
                return false;
            }

            return user.JiangHu.HasJiangHu;
        }

        private static async Task<bool> ExecuteActionJiangHuLevelAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                logger.LogWarning($"ExecuteActionJiangHuLevelAsync[{action.Id}] called with no actor");
                return false;
            }

            if (!user.JiangHu.HasJiangHu)
            {
                return false;
            }

            return user.JiangHu.Grade >= action.Data;
        }

        private static async Task<bool> ExecuteActionJiangHuExpProtectionAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                logger.LogWarning($"ExecuteActionJiangHuExpProtectionAsync[{action.Id}] called with no actor");
                return false;
            }

            if (!user.JiangHu.HasJiangHu)
            {
                return false;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionJiangHuAttributesAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                logger.LogWarning($"ExecuteActionJiangHuAttributesAsync[{action.Id}] called with no actor");
                return false;
            }

            if (user.JiangHu == null || !user.JiangHu.HasJiangHu)
            {
                return false;
            }

            string[] splitParams = SplitParam(param, 3);
            if (splitParams.Length < 3)
            {
                return false;
            }

            string query = splitParams[0];
            string opt = splitParams[1];
            int value = int.Parse(splitParams[2]);

            int compare = 0;
            if (query.Equals("genuineqi"))
            {
                if (opt.Equals("+="))
                {
                    if (value < 0)
                    {
                        return await user.JiangHu.SpendTalentAsync((byte)value);
                    }

                    await user.JiangHu.AwardTalentAsync((byte)value);
                    return true;
                }

                compare = user.JiangHu.Talent;
            }
            else if (query.Equals("freecultivateparam"))
            {
                if (opt.Equals("+="))
                {
                    if (value < 0)
                    {
                        if (user.JiangHu.FreeCaltivateParam < value)
                        {
                            return false;
                        }
                        user.JiangHu.FreeCaltivateParam -= (uint)value;
                        return true;
                    }

                    user.JiangHu.FreeCaltivateParam += (uint)value;
                    return true;
                }

                compare = (int)user.JiangHu.FreeCaltivateParam;
            }

            switch (opt)
            {
                case "==": return compare == value;
                case "<=": return compare <= value;
                case ">=": return compare >= value;
                case "<": return compare < value;
                case ">": return compare > value;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionAchievementsAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                logger.LogWarning("ExecuteActionAchievementsAsync> Invalid actor for action [{}]", action.Id);
                return false;
            }

            if (param.Equals("chk"))
            {
                return user.Achievements.HasAchievement((int)action.Data);
            }
            else if (param.Equals("add"))
            {
                if (user.Achievements.HasAchievement((int)action.Data))
                {
                    return false;
                }

                return await user.Achievements.AwardAchievementAsync((int)action.Data);
            }
            return false;
        }

        private static async Task<bool> ExecuteActionAttachBuffStatusAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParams = SplitParam(param);
            if (splitParams.Length < 4)
            {
                logger.LogWarning($"ExecuteActionAttachBuffStatusAsync invalid param count: {param}");
                return false;
            }

            int statusEffect = int.Parse(splitParams[0]);
            int statusPower = int.Parse(splitParams[1]);
            int originalPower = statusPower;
            int statusDuration = int.Parse(splitParams[2]);

            switch (statusEffect)
            {
                case StatusSet.BUFF_PSTRIKE:
                case StatusSet.BUFF_MSTRIKE:
                case StatusSet.BUFF_IMMUNITY:
                    {
                        statusPower *= 100;
                        break;
                    }

                case StatusSet.BUFF_BREAK:
                case StatusSet.BUFF_COUNTERACTION:
                    {
                        statusPower *= 10;
                        break;
                    }
            }

            await user.AttachStatusAsync(statusEffect + 1, statusPower, statusDuration, 0);

            ClientUpdateType updateType;
            switch (statusEffect)
            {
                case StatusSet.BUFF_PSTRIKE:
                    {
                        updateType = ClientUpdateType.PhysicalCritPct;
                        break;
                    }
                case StatusSet.BUFF_MSTRIKE:
                    {
                        updateType = ClientUpdateType.MagicCritPct;
                        break;
                    }
                case StatusSet.BUFF_IMMUNITY:
                    {
                        updateType = ClientUpdateType.Immunity;
                        break;
                    }
                case StatusSet.BUFF_BREAK:
                    {
                        updateType = ClientUpdateType.Break;
                        break;
                    }
                case StatusSet.BUFF_COUNTERACTION:
                    {
                        updateType = ClientUpdateType.Counteraction;
                        break;
                    }
                case StatusSet.BUFF_MAX_HEALTH:
                    {
                        updateType = ClientUpdateType.HpMod;
                        break;
                    }
                case StatusSet.BUFF_PATTACK:
                    {
                        updateType = ClientUpdateType.PhDmgMod;
                        break;
                    }
                case StatusSet.BUFF_MATTACK:
                    {
                        updateType = ClientUpdateType.MAttkMod;
                        break;
                    }
                case StatusSet.BUFF_FINAL_PDMGREDUCTION:
                    {
                        updateType = ClientUpdateType.PhDmgTakenMod;
                        break;
                    }
                case StatusSet.BUFF_FINAL_MDMGREDUCTION:
                    {
                        updateType = ClientUpdateType.MaDmgTakenMod;
                        break;
                    }
                case StatusSet.BUFF_FINAL_PDAMAGE:
                    {
                        updateType = ClientUpdateType.FinalPhDmgMod;
                        break;
                    }
                case StatusSet.BUFF_FINAL_MDAMAGE:
                    {
                        updateType = ClientUpdateType.FinalMaDmgMod;
                        break;
                    }
                default:
                    return true;
            }

            await user.SynchroAttributesAsync(updateType, (uint)statusDuration, (uint)statusEffect, 0u, (uint)originalPower);
            return true;
        }

        private static async Task<bool> ExecuteActionDetachBuffStatusesAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            for (int i = StatusSet.BUFF_PSTRIKE + 1; i <= StatusSet.BUFF_FINAL_MDMGREDUCTION + 1; i++)
            {
                await user.DetachStatusAsync(i);
            }
            return true;
        }

        private static async Task<bool> ExecuteActionUserReturnAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            if (action.Data > user.VarData.Length)
            {
                return false;
            }

            int totalOfflineDays = 0;
            if (user.PreviousLoginTime.Year != 1970)
            {
                totalOfflineDays = (int)(DateTime.Now - user.PreviousLoginTime).TotalDays;
            }
            user.VarData[(int)action.Data] = totalOfflineDays;
            return true;
        }

        private static async Task<bool> ExecuteActionMouseWaitClickAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParams = param.Split('-');
            uint mouseFace = uint.Parse(splitParams[0]);
            user.InteractingMouseAction = action.Data;
            await user.SendAsync(new MsgAction
            {
                Action = ActionType.MouseSetFace,
                X = user.X,
                Y = user.Y,
                Identity = user.Identity,
                Command = mouseFace
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMouseJudgeTypeAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null || role == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(param))
            {
                return false;
            }

            bool success = true;
            switch (action.Data)
            {
                case 1:
                    {
                        if (role is not Npc npc || !npc.Name.Equals(param))
                        {
                            success = false;
                        }
                        break;
                    }
                case 2:
                    {
                        if (role is not Monster monster || monster.Type != uint.Parse(param))
                        {
                            success = false;
                        }
                        break;
                    }
                case 3:
                    {
                        if (role is not Character targetUser || targetUser.Gender != byte.Parse(param))
                        {
                            success = false;
                        }
                        break;
                    }

                default:
                    {
                        success = false;
                        break;
                    }
            }

            if (success)
            {
                user.InteractingNpc = role.Identity;
                await user.SendAsync(new MsgAction
                {
                    Action = ActionType.MouseResetFace,
                    X = user.X,
                    Y = user.Y,
                    Identity = user.Identity,
                });
            }
            else
            {
                await user.SendAsync(new MsgAction
                {
                    Action = ActionType.MouseResetClick,
                    X = user.X,
                    Y = user.Y,
                    Identity = user.Identity,
                });
            }
            return true;
        }

        private static async Task<bool> ExecuteActionMouseClearStatusAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            user.InteractingMouseAction = 0;
            user.InteractingMouseFunction = string.Empty;

            await user.SendAsync(new MsgAction
            {
                Action = ActionType.MouseResetFace,
                X = user.X,
                Y = user.Y,
                Identity = user.Identity,
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMouseDeleteChosenAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (role == null)
            {
                return false;
            }
            await role.LeaveMapAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionAutoHuntIsActiveAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }
            return user.IsAutoHangUp;
        }

        private static async Task<bool> ExecuteActionAddProcessActivityTaskAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            await user.UpdateTaskActivityAsync((ActivityManager.ActivityType)action.Data);
            return true;
        }

        private static async Task<bool> ExecuteActionCheckUserAttributeLimitAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            int totalPoints = user.Strength + user.Speed + user.Vitality + user.Spirit + user.AttributePoints;
            return totalPoints < Role.MAX_USER_ATTRIB_POINTS;
        }

        private static async Task<bool> ExecuteActionAddProcessTaskSchedleAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            string[] splitParams = SplitParam(param, 2);
            int.TryParse(splitParams[0], out var condition);
            long.TryParse(splitParams[1], out var value);

            await ProcessGoalManager.IncreaseProgressAsync(user, (ProcessGoalManager.GoalType)action.Data, value);
            return true;
        }

        #endregion

        #region Event 2000-2099

        private static async Task<bool> ExecuteActionEventSetstatusAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 3)
            {
                return false;
            }

            uint idMap = uint.Parse(pszParam[0]);
            ulong nStatus = ulong.Parse(pszParam[1]);
            int nFlag = int.Parse(pszParam[2]);

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                return false;
            }

            if (nFlag == 0)
            {
                map.Flag &= ~nStatus;
            }
            else
            {
                map.Flag |= nStatus;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionEventDelnpcGenidAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            foreach (Role monster in RoleManager
                         .QueryRoles(x => x is Monster monster && monster.GeneratorId == action.Data))
            {
                await monster.LeaveMapAsync();
            }
            return true;
        }

        private static async Task<bool> ExecuteActionEventCompareAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 3)
            {
                return false;
            }

            long nData1 = long.Parse(pszParam[0]), nData2 = long.Parse(pszParam[2]);
            string szOpt = pszParam[1];

            switch (szOpt)
            {
                case "==":
                    return nData1 == nData2;
                case "<":
                    return nData1 < nData2;
                case ">":
                    return nData1 > nData2;
                case "<=":
                    return nData1 <= nData2;
                case ">=":
                    return nData1 >= nData2;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionEventCompareUnsignedAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 3)
            {
                return false;
            }

            ulong nData1 = ulong.Parse(pszParam[0]), nData2 = ulong.Parse(pszParam[2]);
            string szOpt = pszParam[1];

            switch (szOpt)
            {
                case "==":
                    return nData1 == nData2;
                case "<":
                    return nData1 < nData2;
                case ">":
                    return nData1 > nData2;
                case "<=":
                    return nData1 <= nData2;
                case ">=":
                    return nData1 >= nData2;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionEventChangeweatherAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {


            return true;
        }

        private static async Task<bool> ExecuteActionEventCreatepetAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 7) return false;

            uint dwOwnerType = uint.Parse(pszParam[0]);
            uint idOwner = uint.Parse(pszParam[1]);
            uint idMap = uint.Parse(pszParam[2]);
            ushort usPosX = ushort.Parse(pszParam[3]);
            ushort usPosY = ushort.Parse(pszParam[4]);
            uint idGen = uint.Parse(pszParam[5]);
            uint idType = uint.Parse(pszParam[6]);
            uint dwData = 0;
            var szName = "";

            if (pszParam.Length >= 8)
                dwData = uint.Parse(pszParam[7]);
            if (pszParam.Length >= 9)
                szName = pszParam[8];

            DbMonstertype monstertype = RoleManager.GetMonstertype(idType);
            GameMap map = MapManager.GetMap(idMap);

            if (monstertype == null || map == null)
            {
                logger.LogWarning($"ExecuteActionEventCreatepet [{action.Id}] invalid monstertype or map: {param}");
                return false;
            }

            var msg = new MsgAiSpawnNpc
            {
                Mode = AiSpawnNpcMode.Spawn
            };
            msg.List.Add(new MsgAiSpawnNpc<AiClient>.SpawnNpc
            {
                GeneratorId = idGen,
                MapId = idMap,
                MonsterType = idType,
                OwnerId = idOwner,
                X = usPosX,
                Y = usPosY,
                OwnerType = dwOwnerType,
                Data = dwData
            });
            await BroadcastNpcMsgAsync(msg);
            return true;
        }

        private static async Task<bool> ExecuteActionEventCreatenewNpcAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 9)
            {
                return false;
            }

            string szName = pszParam[0];
            ushort nType = ushort.Parse(pszParam[1]);
            ushort nSort = ushort.Parse(pszParam[2]);
            ushort nLookface = ushort.Parse(pszParam[3]);
            uint nOwnerType = uint.Parse(pszParam[4]);
            uint nOwner = uint.Parse(pszParam[5]);
            uint idMap = uint.Parse(pszParam[6]);
            ushort nPosX = ushort.Parse(pszParam[7]);
            ushort nPosY = ushort.Parse(pszParam[8]);
            uint nLife = 0;
            uint idBase = 0;
            uint idLink = 0;
            uint setTask0 = 0;
            uint setTask1 = 0;
            uint setTask2 = 0;
            uint setTask3 = 0;
            uint setTask4 = 0;
            uint setTask5 = 0;
            uint setTask6 = 0;
            uint setTask7 = 0;
            int setData0 = 0;
            int setData1 = 0;
            int setData2 = 0;
            int setData3 = 0;
            string szData = "";
            if (pszParam.Length > 9)
            {
                nLife = uint.Parse(pszParam[9]);
                if (pszParam.Length > 10)
                {
                    idBase = uint.Parse(pszParam[10]);
                }

                if (pszParam.Length > 11)
                {
                    idLink = uint.Parse(pszParam[11]);
                }

                if (pszParam.Length > 12)
                {
                    setTask0 = uint.Parse(pszParam[12]);
                }

                if (pszParam.Length > 13)
                {
                    setTask1 = uint.Parse(pszParam[13]);
                }

                if (pszParam.Length > 14)
                {
                    setTask2 = uint.Parse(pszParam[14]);
                }

                if (pszParam.Length > 15)
                {
                    setTask3 = uint.Parse(pszParam[15]);
                }

                if (pszParam.Length > 16)
                {
                    setTask4 = uint.Parse(pszParam[16]);
                }

                if (pszParam.Length > 17)
                {
                    setTask5 = uint.Parse(pszParam[17]);
                }

                if (pszParam.Length > 18)
                {
                    setTask6 = uint.Parse(pszParam[18]);
                }

                if (pszParam.Length > 19)
                {
                    setTask7 = uint.Parse(pszParam[19]);
                }

                if (pszParam.Length > 20)
                {
                    setData0 = int.Parse(pszParam[20]);
                }

                if (pszParam.Length > 21)
                {
                    setData1 = int.Parse(pszParam[21]);
                }

                if (pszParam.Length > 22)
                {
                    setData2 = int.Parse(pszParam[22]);
                }

                if (pszParam.Length > 23)
                {
                    setData3 = int.Parse(pszParam[23]);
                }

                if (pszParam.Length > 24)
                {
                    szData = pszParam[24];
                }
            }

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                logger.LogWarning($"ExecuteActionEventCreatenewNpc invalid {idMap} map identity for action {action.Id}");
                return false;
            }

            var npc = new DbDynanpc
            {
                Name = szName,
                Base = idBase,
                Cellx = nPosX,
                Celly = nPosY,
                Data0 = setData0,
                Data1 = setData1,
                Data2 = setData2,
                Data3 = setData3,
                Datastr = szData,
                Defence = 0,
                Life = nLife,
                Maxlife = nLife,
                Linkid = idLink,
                Task0 = setTask0,
                Task1 = setTask1,
                Task2 = setTask2,
                Task3 = setTask3,
                Task4 = setTask4,
                Task5 = setTask5,
                Task6 = setTask6,
                Task7 = setTask7,
                Ownerid = nOwner,
                OwnerType = nOwnerType,
                Lookface = nLookface,
                Type = nType,
                Mapid = idMap,
                Sort = nSort
            };

            if (!await ServerDbContext.SaveAsync(npc))
            {
                logger.LogWarning($"ExecuteActionEventCreatenewNpc could not save dynamic npc");
                return false;
            }

            DynamicNpc dynaNpc = new(npc);
            if (!await dynaNpc.InitializeAsync())
            {
                return false;
            }

            Task gameActionCreateNpcTask() => dynaNpc.EnterMapAsync();
            if (user != null && map.Partition == user.Map.Partition) // if there is an actor, then the action is already queued! (EXPECTED)
            {
                await gameActionCreateNpcTask();
            }
            else
            {
                Kernel.Services.Processor.Queue(map.Partition, gameActionCreateNpcTask);
            }
            return true;
        }

        private static async Task<bool> ExecuteActionEventCountmonsterAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 5)
                return false;

            uint idMap = uint.Parse(pszParam[0]);
            string szField = pszParam[1];
            string szData = pszParam[2];
            string szOpt = pszParam[3];
            int nNum = int.Parse(pszParam[4]);
            var nCount = 0;

            switch (szField.ToLowerInvariant())
            {
                case "name":
                    nCount += RoleManager
                              .QueryRoles(x => x is Monster mob && mob.MapIdentity == idMap && mob.Name.Equals(szData) && mob.IsAlive)
                              .Count;
                    break;
                case "gen_id":
                    nCount += RoleManager
                              .QueryRoles(x => x is Monster mob && mob.GeneratorId == uint.Parse(szData) && mob.IsAlive)
                              .Count;
                    break;
            }

            switch (szOpt)
            {
                case "==":
                    return nCount == nNum;
                case "<":
                    return nCount < nNum;
                case ">":
                    return nCount > nNum;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionEventDeletemonsterAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 2)
                return false;

            uint idMap = uint.Parse(pszParam[0]);
            uint idType = uint.Parse(pszParam[1]);
            var nData = 0;
            var szName = "";

            if (pszParam.Length >= 3)
                nData = int.Parse(pszParam[2]);
            if (pszParam.Length >= 4)
                szName = pszParam[3];

            var ret = false;


            if (!string.IsNullOrEmpty(szName))
            {
                foreach (Role monster in RoleManager.QueryRoles(
                             x => x is Monster && x.MapIdentity == idMap && x.Name.Equals(szName)))
                {
                    await monster.LeaveMapAsync();
                    ret = true;
                }
            }

            if (idType != 0)
            {
                foreach (Role monster in RoleManager.QueryRoles(
                             x => x is Monster mob && x.MapIdentity == idMap && mob.Type == idType))
                {
                    await monster.LeaveMapAsync();
                    ret = true;
                }
            }

            return ret;
        }

        private static async Task<bool> ExecuteActionEventBbsAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            await BroadcastWorldMsgAsync(param, TalkChannel.System);
            return true;
        }

        private static async Task<bool> ExecuteActionEventEraseAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
            {
                return false;
            }

            uint npcType = uint.Parse(pszParam[2]);
            foreach (var dynaNpc in RoleManager.QueryRoleByMap<DynamicNpc>(uint.Parse(pszParam[0])))
            {
                if (dynaNpc.Type == npcType)
                {
                    await dynaNpc.DelNpcAsync();
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionEventTeleportAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 4)
            {
                return false;
            }

            if (!uint.TryParse(pszParam[0], out var idSource) || !uint.TryParse(pszParam[1], out var idTarget) ||
                !ushort.TryParse(pszParam[2], out var usMapX) || !ushort.TryParse(pszParam[3], out var usMapY))
            {
                return false;
            }

            GameMap sourceMap = MapManager.GetMap(idSource);
            GameMap targetMap = MapManager.GetMap(idTarget);

            if (sourceMap == null || targetMap == null)
            {
                return false;
            }

            if (sourceMap.IsTeleportDisable())
            {
                return false;
            }

            if (!sourceMap[usMapX, usMapY].IsAccessible())
            {
                return false;
            }

            foreach (var player in RoleManager.QueryRoleByType<Character>()
                .Where(x => x.MapIdentity == sourceMap.Identity))
            {
                await player.FlyMapAsync(idTarget, usMapX, usMapY);
            }

            return true;
        }

        private static async Task<bool> ExecuteActionEventMassactionAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
            {
                return false;
            }

            if (!uint.TryParse(pszParam[0], out var idMap) || !uint.TryParse(pszParam[1], out var idAction)
                                                           || !int.TryParse(pszParam[2], out var nAmount))
            {
                return false;
            }

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                return false;
            }

            if (nAmount <= 0)
            {
                nAmount = int.MaxValue;
            }

            foreach (var player in RoleManager.QueryRoleByMap<Character>(idMap))
            {
                if (nAmount-- <= 0)
                {
                    break;
                }

                await ExecuteActionAsync(idAction, player, role, null, input);
            }

            return true;
        }

        private static async Task<bool> ExecuteActionEventRegisterAsync(DbAction action, string param, Character user,
            Role role,
            Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            GameEvent baseEvent;
            switch (param.ToLower())
            {
                case "lineskillpk":
                    {
                        baseEvent = EventManager.GetEvent<LineSkillPK>();
                        break;
                    }
                default:
                    return false;
            }

            if (baseEvent == null)
            {
                return false;
            }

            return await user.SignInEventAsync(baseEvent);
        }

        private static async Task<bool> ExecuteActionEventExitAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
            {
                return false;
            }

            GameEvent baseEvent;
            switch (param.ToLower())
            {
                case "lineskillpk":
                    {
                        baseEvent = EventManager.GetEvent<LineSkillPK>();
                        break;
                    }
                default:
                    return false;
            }

            if (baseEvent == null)
            {
                return false;
            }

            return await user.SignOutEventAsync(baseEvent);
        }

        private static async Task<bool> ExecuteActionEventCmdAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            GameEvent.EventType eventType = (GameEvent.EventType)action.Data;
            if (eventType == GameEvent.EventType.None || eventType >= GameEvent.EventType.Limit)
            {
                return false;
            }

            var @event = EventManager.GetEvent(eventType);
            if (@event == null)
            {
                return false;
            }

            return await @event.OnActionCommandAsync(param, user, role, item, input);
        }

        #endregion

        #region Trap 2100-2199

        /**
         * 
         * ActionTrapCreate = 2101,
            ActionTrapErase = 2102,
            ActionTrapCount = 2103,
            ActionTrapAttr = 2104, 
            ActionTrapInstanceDelete = 2105,
        */

        private static async Task<bool> ExecuteActionTrapCreateAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            string[] splitParams = SplitParam(param, 9);
            if (splitParams.Length < 7)
            {
                logger.LogError($"Invalid param length ExecuteActionTrapCreate {action.Id}");
                return false;
            }

            uint type = uint.Parse(splitParams[0]),
                look = uint.Parse(splitParams[1]),
                owner = uint.Parse(splitParams[2]),
                idMap = uint.Parse(splitParams[3]);
            ushort posX = ushort.Parse(splitParams[4]),
                posY = ushort.Parse(splitParams[5]),
                data = ushort.Parse(splitParams[6]);

            if (MapManager.GetMap(idMap) == null)
            {
                logger.LogError($"Invalid map for ExecuteActionTrapCreate {idMap}:{action.Id}");
                return false;
            }

            MapTrap trap = new MapTrap(new DbTrap
            {
                TypeId = type,
                Look = look,
                OwnerId = owner,
                Data = data,
                MapId = idMap,
                PosX = posX,
                PosY = posY,
                Id = (uint)IdentityManager.Traps.GetNextIdentity
            });

            if (!await trap.InitializeAsync())
            {
                logger.LogError($"could not start trap for ExecuteActionTrapCreate {action.Id}");
                return false;
            }

            trap.QueueAction(trap.EnterMapAsync);
            return true;
        }

        private static async Task<bool> ExecuteActionTrapEraseAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            MapTrap trap = role as MapTrap;
            if (trap == null)
                return false;

            trap.QueueAction(trap.LeaveMapAsync);
            return true;
        }

        private static async Task<bool> ExecuteActionTrapCountAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            return true;
        }

        private static async Task<bool> ExecuteActionTrapAttrAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            return true;
        }

        private static async Task<bool> ExecuteActionTrapTypeDeleteAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            string[] splitParams = SplitParam(" ");
            if (splitParams.Length < 2)
            {

                return false;
            }

            uint idMap = uint.Parse(splitParams[0]);
            uint trapType = uint.Parse(splitParams[1]);

            GameMap gameMap = MapManager.GetMap(idMap);
            if (gameMap == null)
            {
                return false;
            }

            foreach (var mapTrap in gameMap.QueryRoles(x => x is MapTrap trap && trap.Type == trapType))
            {
                mapTrap.QueueAction(mapTrap.LeaveMapAsync);
            }
            return true;
        }

        #endregion

        #region Detain 2200-2299

        private static async Task<bool> ExecuteActionDetainDialogAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (param.Equals("target"))
            {
                await user.SendAsync(new MsgAction
                {
                    Action = ActionType.ClientDialog,
                    X = user.X,
                    Y = user.Y,
                    Identity = user.Identity,
                    Data = 336
                });
                return true;
            }

            if (param.Equals("hunter"))
            {
                await user.SendAsync(new MsgAction
                {
                    Action = ActionType.ClientDialog,
                    X = user.X,
                    Y = user.Y,
                    Identity = user.Identity,
                    Data = 337
                });
                return true;
            }

            return false;
        }

        #endregion

        #region Family 3500-3599

        private static async Task<bool> ExecuteActionFamilyAttrAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param, 3);
            if (splitParam.Length < 3)
            {
                return false;
            }

            string field = splitParam[0],
                   opt = splitParam[1];
            long value = long.Parse(splitParam[2]);

            long data = -1;
            if (user?.Family != null)
            {
                if (field.Equals("money"))
                {
                    if (opt.Equals("+="))
                    {
                        if (value < 0)
                        {
                            var temp = (ulong)(value * -1);
                            if (user.Family.Money < temp)
                            {
                                return false;
                            }

                            user.Family.Money -= temp;
                        }
                        else
                        {
                            var temp = (ulong)value;
                            user.Family.Money += temp;
                        }

                        return await user.Family.SaveAsync();
                    }

                    data = (long)user.Family.Money;
                }
                else if (field.Equals("rank"))
                {
                    if (opt.Equals("="))
                    {
                        user.Family.Rank = (byte)Math.Min(4, value);
                        return await user.Family.SaveAsync();
                    }

                    data = user.Family.Rank;
                }
                else if (field.Equals("star_tower"))
                {
                    if (opt.Equals("="))
                    {
                        user.Family.BattlePowerTower = (byte)Math.Min(4, value);
                        return await user.Family.SaveAsync();
                    }

                    data = user.Family.BattlePowerTower;
                }
            }

            switch (opt)
            {
                case "==": return data == value;
                case ">=": return data >= value;
                case "<=": return data <= value;
                case ">": return data > value;
                case "<": return data < value;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionFamilyMemberAttrAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param, 3);
            if (splitParam.Length < 3)
            {
                return false;
            }

            string field = splitParam[0],
                   opt = splitParam[1];
            long value = long.Parse(splitParam[2]);
            long data = 0;

            if (user?.Family != null)
            {
                if (field.Equals("rank"))
                {
                    data = (long)user.FamilyPosition;
                }
            }

            switch (opt)
            {
                case "==": return data == value;
                case ">=": return data >= value;
                case "<=": return data <= value;
                case ">": return data > value;
                case "<": return data < value;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionFamilyWarActivityCheckAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Family == null)
            {
                return false;
            }

            var npc = RoleManager.FindRole<DynamicNpc>(x => x.Identity == action.Data);
            if (npc == null)
            {
                return false;
            }

            if (npc.Identity != user.Family.Challenge && npc.Identity != user.Family.Occupy)
            {
                return false;
            }

            var war = EventManager.GetEvent<FamilyWar>();
            if (war == null)
            {
                return false;
            }

            return await war.ValidateResultAsync(user, npc.Identity);
        }

        private static async Task<bool> ExecuteActionFamilyWarAuthorityCheckAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            var war = EventManager.GetEvent<FamilyWar>();
            if (war == null)
            {
                return false;
            }

            if (user?.Family == null)
            {
                return false;
            }

            var npc = RoleManager.FindRole<DynamicNpc>(x => x.Identity == action.Data);
            if (npc == null)
            {
                return false;
            }

            if (npc.Identity != user.Family.Challenge)
            {
                return false;
            }

            return true;
        }

        private static async Task<bool> ActionFamilyWarRegisterCheckAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            var war = EventManager.GetEvent<FamilyWar>();
            if (war == null)
            {
                return false;
            }

            if (user?.Family == null)
            {
                return false;
            }

            var npc = RoleManager.FindRole<DynamicNpc>(x => x.Identity == action.Data);
            if (npc == null)
            {
                return false;
            }

            if (npc.Identity != user.Family.Occupy)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Horse Racing 3600-3699

        private static async Task<bool> ExecuteActionMountRacingEventResetAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            HorseRacing horseRacing = EventManager.GetEvent<HorseRacing>();
            if (horseRacing == null)
            {
                logger.LogCritical($"Cannot start horse racing! event not initialized");
                return false;
            }

            await horseRacing.PrepareStartupAsync(uint.Parse(param));
            return true;
        }

        #endregion

        #region Progress Bar? 3700-3799

        private static async Task<bool> ExecuteActionProgressBarAsync(DbAction action, string param, Character user, Role role,
            Item item,
            string input)
        {
            if (user == null)
            {
                logger.LogWarning("Progress bar action cannot have null user [ActionID: {Action}]", action.Id);
                return false;
            }

            string[] splitParams = param.Split(' ');
            if (splitParams.Length < 3)
            {
                logger.LogWarning("Progress bar action parameters less than 3");
                return false;
            }

            int seconds = int.Parse(splitParams[0]);
            string message = splitParams[1];
            int unknown = int.Parse(splitParams[2]);

            ProgressBar progressBar = new(seconds + 1)
            {
                Command = (uint)unknown,
                IdNext = action.IdNext,
                IdNextFail = action.IdNextfail
            };
            user.AwaitingProgressBar = progressBar;

            await user.SendAsync(new MsgAction
            {
                Action = ActionType.ProgressBar,
                Identity = user.Identity,
                Command = (uint)unknown,
                Direction = 1,
                MapColor = (uint)seconds,
                Strings = new List<string>
                {
                    message.Replace('~', ' ')
                }
            });
            return true;
        }

        #endregion

        #region Capture The Flag 3900-3999

        private static async Task<bool> ExecuteActionCaptureTheFlagCheckAsync(DbAction action, Character user, Role role, Item item, string input)
        {
            CaptureTheFlag captureTheFlag = EventManager.GetEvent<CaptureTheFlag>();
            if (captureTheFlag == null)
            {
                return false;
            }
            return captureTheFlag.IsActive;
        }

        private static async Task<bool> ExecuteActionCaptureTheFlagExitAsync(DbAction action, Character user, Role role, Item item, string input)
        {
            CaptureTheFlag captureTheFlag = EventManager.GetEvent<CaptureTheFlag>();
            if (captureTheFlag == null)
            {
                return false;
            }
            // ???
            return true;
        }

        #endregion

        #region Lua > 20000

        private static async Task<bool> ExecuteActionLuaLinkMainAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            string[] splitParams = SplitParam(param);
            string script;

            if (splitParams[0].Equals("LinkMonsterMain"))
            {
                return true;
            }
            if (splitParams.Length > 1)
            {
                script = $"{splitParams[0]}({string.Join(',', splitParams[1..(splitParams.Length)])})";
            }
            else
            {
                script = $"{splitParams[0]}()";
            }
            LuaScriptManager.Run(user, role, item, input, script);
            return true;
        }

        private static async Task<bool> ExecuteActionLuaExecuteAsync(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            string[] splitParams = SplitParam(param, 2);
            if (user != null)
            {
                LuaScriptManager.Run(user, role, item, input, $"{splitParams[0]}({splitParams[1]},{user.Identity})");
            }
            else
            {
                LuaScriptManager.Run(user, role, item, input, $"{splitParams[0]}({splitParams[1]})");
            }
            return true;
        }

        #endregion

        private static string FormatLogString(DbAction action, string param, Character user, Role role, Item item, string input)
        {
            List<string> append = new()
            {
                $"ActionType:{action?.Type}"
            };
            if (user != null)
            {
                append.Add($"User:{user.Identity},{user.Name}");
            }
            if (role != null)
            {
                append.Add($"Role:{role.Identity},{role.Name}");
            }
            if (item != null)
            {
                append.Add($"(Item: type {item.Type},{item.Name};id {item.Identity})");
            }
            if (action != null)
            {
                return $"[{string.Join(',', append.ToArray())}] {action.Id}: [{action.IdNext},{action.IdNextfail}]. type[{action.Type}], data[{action.Data}], param:[{param ?? action.Param}][input:{input}].";
            }
            return $"[{string.Join(',', append.ToArray())}]";
        }

        private static async Task<string> FormatParamAsync(DbAction action, Character user, Role role, Item item, string input)
        {
            string result = action.Param;

            result = result.Replace("%user_name", user?.Name ?? StrNone)
                .Replace("%user_id", user?.Identity.ToString() ?? "0")
                .Replace("%user_lev", user?.Level.ToString() ?? "0")
                .Replace("%user_mete", user?.Metempsychosis.ToString() ?? "0")
                .Replace("%user_meto", user?.Metempsychosis.ToString() ?? "0")
                .Replace("%user_mate", user?.MateName ?? StrNone)
                .Replace("%user_pro", user?.Profession.ToString() ?? "0")
                .Replace("%user_map_id", user?.Map?.Identity.ToString() ?? "0")
                .Replace("%user_map_name", user?.Map?.Name ?? StrNone)
                .Replace("%user_map_x", user?.X.ToString() ?? "0")
                .Replace("%user_map_y", user?.Y.ToString() ?? "0")
                .Replace("%map_owner_id", user?.Map?.OwnerIdentity.ToString() ?? "0")
                .Replace("%user_nobility_rank", ((int)(user?.NobilityRank ?? 0)).ToString())
                .Replace("%user_nobility_position", user?.NobilityPosition.ToString() ?? "0")
                .Replace("%user_home_id", user?.HomeIdentity.ToString() ?? "0")
                .Replace("%syn_id", user?.SyndicateIdentity.ToString() ?? "0")
                .Replace("%syn_name", user?.SyndicateName ?? StrNone)
                .Replace("%account_id", user?.Client?.AccountIdentity.ToString() ?? "0")
                .Replace("%user_virtue", user?.VirtuePoints.ToString() ?? "0")
                .Replace("%map_owner_id", user?.Map?.OwnerIdentity.ToString() ?? "0")
                .Replace("%last_add_item_id", user?.LastAddItemIdentity.ToString() ?? "0")
                .Replace("%online_time", $"{user?.OnlineTime.TotalDays:0} days, {user?.OnlineTime.Hours:00} hours, {user?.OnlineTime.Minutes} minutes and {user?.OnlineTime.Seconds} seconds")
                .Replace("%session_time", $"{user?.SessionOnlineTime.TotalDays:0} days, {user?.SessionOnlineTime.Hours:00} hours, {user?.SessionOnlineTime.Minutes} minutes and {user?.SessionOnlineTime.Seconds} seconds")
                .Replace("%businessman_days", $"{user?.BusinessManDays ?? 0}")
                .Replace("%user_vip", user?.BaseVipLevel.ToString())
                .Replace("%user_team", user?.Team?.MemberCount.ToString() ?? "0");

            result = result.Replace("%accept0", input);

            if (result.Contains("%levelup_exp"))
            {
                DbLevelExperience db = ExperienceManager.GetLevelExperience(user?.Level ?? 0);
                result = result.Replace("%levelup_exp", db != null ? db.Exp.ToString() : "0");
            }

            if (user != null)
            {
                while (result.Contains("%stc("))
                {
                    int start = result.IndexOf("%stc(", StringComparison.InvariantCultureIgnoreCase);
                    string strEvent = "", strStage = "";
                    bool comma = false;
                    for (int i = start + 5; i < result.Length; i++)
                    {
                        if (!comma)
                        {
                            if (result[i] == ',')
                            {
                                comma = true;
                                continue;
                            }

                            strEvent += result[i];
                        }
                        else
                        {
                            if (result[i] == ')')
                            {
                                break;
                            }

                            strStage += result[i];
                        }
                    }

                    uint.TryParse(strEvent, out var stcEvent);
                    uint.TryParse(strStage, out var stcStage);

                    DbStatistic stc = user.Statistic.GetStc(stcEvent, stcStage);
                    result = result.Replace($"%stc({strEvent},{strStage})", stc?.Data.ToString() ?? "0");
                }

                while (result.Contains("%stc_daily("))
                {
                    int start = result.IndexOf("%stc_daily(", StringComparison.InvariantCultureIgnoreCase);
                    string strEvent = "", strStage = "";
                    bool comma = false;
                    for (int i = start + 11; i < result.Length; i++)
                    {
                        if (!comma)
                        {
                            if (result[i] == ',')
                            {
                                comma = true;
                                continue;
                            }

                            strEvent += result[i];
                        }
                        else
                        {
                            if (result[i] == ')')
                            {
                                break;
                            }

                            strStage += result[i];
                        }
                    }

                    uint.TryParse(strEvent, out var stcEvent);
                    uint.TryParse(strStage, out var stcStage);

                    DbStatisticDaily stc = user.Statistic.GetDailyStc(stcEvent, stcStage);
                    result = result.Replace($"%stc_daily({strEvent},{strStage})", stc?.Data.ToString() ?? "0");
                }

                while (result.Contains("%iter_var"))
                {
                    for (int i = Role.MAX_VAR_AMOUNT - 1; i >= 0; i--)
                    {
                        result = result.Replace($"%iter_var_data{i}", user.VarData[i].ToString());
                        result = result.Replace($"%iter_var_str{i}", user.VarString[i]);
                    }
                }

                while (result.Contains("%taskdata"))
                {
                    int start = result.IndexOf("%taskdata(", StringComparison.InvariantCultureIgnoreCase);
                    string taskId = "", taskDataIdx = "";
                    bool comma = false;
                    for (int i = start + 10; i < result.Length; i++)
                    {
                        if (!comma)
                        {
                            if (result[i] == ',')
                            {
                                comma = true;
                                continue;
                            }

                            taskId += result[i];
                        }
                        else
                        {
                            if (result[i] == ')')
                            {
                                break;
                            }

                            taskDataIdx += result[i];
                        }
                    }

                    uint.TryParse(taskId, out var evt);
                    int.TryParse(taskDataIdx, out var idx);

                    var value = user.TaskDetail.GetData(evt, $"data{idx}");
                    result = ReplaceFirst(result, $"%taskdata({evt},{idx})", value.ToString());
                }

                while (result.Contains("%emoney_card1"))
                {
                    int start = result.IndexOf("%emoney_card1(", StringComparison.InvariantCultureIgnoreCase);
                    string cardTypeString = "";
                    for (int i = start + 14; i < result.Length; i++)
                    {
                        if (result[i] == ')')
                        {
                            break;
                        }

                        cardTypeString += result[i];
                    }

                    uint.TryParse(cardTypeString, out var cardType);
                    result = ReplaceFirst(result, $"%emoney_card1({cardType})", "0");
                }

                while (result.Contains("%emoney_card2"))
                {
                    result = ReplaceFirst(result, $"%emoney_card2", "0");
                }
            }

            if (role != null)
            {
                if (role is BaseNpc npc)
                {
                    result = result.Replace("%data0", npc.GetData("data0").ToString())
                        .Replace("%data1", npc.GetData("data1").ToString())
                        .Replace("%data2", npc.GetData("data2").ToString())
                        .Replace("%data3", npc.GetData("data3").ToString())
                        .Replace("%npc_ownerid", npc.OwnerIdentity.ToString())
                        .Replace("%map_owner_id", role.Map.OwnerIdentity.ToString() ?? "0")
                        .Replace("%id", npc.Identity.ToString())
                        .Replace("%npc_x", npc.X.ToString())
                        .Replace("%npc_y", npc.Y.ToString());
                }

                result = result.Replace("%map_owner_id", role.Map?.OwnerIdentity.ToString());
            }

            if (item != null)
            {
                result = result.Replace("%item_data", item.Identity.ToString())
                    .Replace("%item_name", item.Name)
                    .Replace("%item_type", item.Type.ToString())
                    .Replace("%item_id", item.Identity.ToString());
            }

            while (result.Contains("%random"))
            {
                int start = result.IndexOf("%random(", StringComparison.InvariantCultureIgnoreCase);
                string rateStr = "";
                for (int i = start + 8; i < result.Length; i++)
                {
                    if (result[i] == ')')
                    {
                        break;
                    }
                    rateStr += result[i];
                }

                int rate = int.Parse(rateStr);
                result = ReplaceFirst(result, $"%random({rateStr})", (await NextAsync(rate)).ToString());
            }

            while (result.Contains("%global_dyna_data_str"))
            {
                int start = result.IndexOf("%global_dyna_data_str(", StringComparison.InvariantCultureIgnoreCase);
                string strEvent = "", strNum = "";
                bool comma = false;
                for (int i = start + 21; i < result.Length; i++)
                {
                    if (!comma)
                    {
                        if (result[i] == ',')
                        {
                            comma = true;
                            continue;
                        }

                        strEvent += result[i];
                    }
                    else
                    {
                        if (result[i] == ')')
                        {
                            break;
                        }

                        strNum += result[i];
                    }
                }

                uint.TryParse(strEvent, out var evt);
                int.TryParse(strNum, out var idx);

                var data = await DynamicGlobalDataManager.GetAsync(evt);
                string value = DynamicGlobalDataManager.GetStringData(data, idx);
                result = ReplaceFirst(result, $"%global_dyna_data_str({evt},{idx})", value.ToString());
            }

            while (result.Contains("%global_dyna_data"))
            {
                int start = result.IndexOf("%global_dyna_data(", StringComparison.InvariantCultureIgnoreCase);
                string strEvent = "", strNum = "";
                bool comma = false;
                for (int i = start + 18; i < result.Length; i++)
                {
                    if (!comma)
                    {
                        if (result[i] == ',')
                        {
                            comma = true;
                            continue;
                        }

                        strEvent += result[i];
                    }
                    else
                    {
                        if (result[i] == ')')
                        {
                            break;
                        }

                        strNum += result[i];
                    }
                }

                uint.TryParse(strEvent, out var evt);
                int.TryParse(strNum, out var idx);

                var data = await DynamicGlobalDataManager.GetAsync(evt);
                long value = DynamicGlobalDataManager.GetData(data, idx);
                result = ReplaceFirst(result, $"%global_dyna_data({evt},{idx})", value.ToString());
            }

            while (result.Contains("%sysdatastr"))
            {
                int start = result.IndexOf("%sysdatastr(", StringComparison.InvariantCultureIgnoreCase);
                string strEvent = "", strNum = "";
                bool comma = false;
                for (int i = start + 12; i < result.Length; i++)
                {
                    if (!comma)
                    {
                        if (result[i] == ',')
                        {
                            comma = true;
                            continue;
                        }

                        strEvent += result[i];
                    }
                    else
                    {
                        if (result[i] == ')')
                        {
                            break;
                        }

                        strNum += result[i];
                    }
                }

                uint.TryParse(strEvent, out var evt);
                int.TryParse(strNum, out var idx);

                var data = await DynamicGlobalDataManager.GetAsync(evt);
                string value = DynamicGlobalDataManager.GetStringData(data, idx);
                result = ReplaceFirst(result, $"%sysdatastr({evt},{idx})", value);
            }

            while (result.Contains("%sysdata"))
            {
                int start = result.IndexOf("%sysdata(", StringComparison.InvariantCultureIgnoreCase);
                string strEvent = "", strNum = "";
                bool comma = false;
                for (int i = start + 9; i < result.Length; i++)
                {
                    if (!comma)
                    {
                        if (result[i] == ',')
                        {
                            comma = true;
                            continue;
                        }

                        strEvent += result[i];
                    }
                    else
                    {
                        if (result[i] == ')')
                        {
                            break;
                        }

                        strNum += result[i];
                    }
                }

                uint.TryParse(strEvent, out var evt);
                int.TryParse(strNum, out var idx);

                var data = await DynamicGlobalDataManager.GetAsync(evt);
                long value = DynamicGlobalDataManager.GetData(data, idx);
                result = ReplaceFirst(result, $"%sysdata({evt},{idx})", value.ToString());
            }

            if (result.Contains("%iter_upquality_gem"))
            {
                Item pItem = user?.UserPackage[(Item.ItemPosition)user.Iterator];
                if (pItem != null)
                {
                    result = result.Replace("%iter_upquality_gem", pItem.GetUpQualityGemAmount().ToString());
                }
                else
                {
                    result = result.Replace("%iter_upquality_gem", "0");
                }
            }

            if (result.Contains("%iter_itembound"))
            {
                Item pItem = user?.UserPackage[(Item.ItemPosition)user.Iterator];
                if (pItem != null)
                {
                    result = result.Replace("%iter_itembound", pItem.IsBound ? "1" : "0");
                }
                else
                {
                    result = result.Replace("%iter_itembound", "0");
                }
            }

            if (result.Contains("%iter_uplevel_gem"))
            {
                Item pItem = user?.UserPackage[(Item.ItemPosition)user.Iterator];
                if (pItem != null)
                {
                    result = result.Replace("%iter_uplevel_gem", pItem.GetUpgradeGemAmount().ToString());
                }
                else
                {
                    result = result.Replace("%iter_uplevel_gem", "0");
                }
            }

            result = result.Replace("%map_name", user?.Map?.Name ?? role?.Map?.Name ?? StrNone)
                .Replace("%iter_time", UnixTimestamp.Now.ToString())
                .Replace("%%", "%")
                .Replace("%last_del_item_id", "0");
            return result;
        }

        private static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return string.Concat(text.AsSpan(0, pos), replace, text.AsSpan(pos + search.Length));
        }

        private static string[] SplitParam(string param, int count = 0)
        {
            return count > 0
                ? param.Split(new[] { ' ' }, count, StringSplitOptions.RemoveEmptyEntries)
                : param.Split(' ');
        }

        private static string GetParenthesys(string szParam)
        {
            int varIdx = szParam.IndexOf("(", StringComparison.CurrentCulture) + 1;
            int endIdx = szParam.IndexOf(")", StringComparison.CurrentCulture);
            return szParam[varIdx..endIdx];
        }

        private static byte VarId(string szParam)
        {
            int start = szParam.IndexOf("%var(", StringComparison.InvariantCultureIgnoreCase);
            string rateStr = "";
            for (int i = start + 5; i < szParam.Length; i++)
            {
                if (szParam[i] == ')')
                {
                    break;
                }
                rateStr += szParam[i];
            }
            return byte.Parse(rateStr);
        }

        public enum TaskActionType
        {
            // System
            ActionSysFirst = 100,
            ActionMenutext = 101,
            ActionMenulink = 102,
            ActionMenuedit = 103,
            ActionMenupic = 104,
            ActionMenuMessage = 105,
            ActionMenubutton = 110,
            ActionMenulistpart = 111,
            ActionMenuTaskClear = 113,
            ActionMenucreate = 120,
            ActionRand = 121,
            ActionRandaction = 122,
            ActionChktime = 123,
            ActionPostcmd = 124,
            ActionBrocastmsg = 125,
            ActionSysExecAction = 126,
            ActionExecutequery = 127,
            ActionSysDoSomethingUnknown = 128,
            ActionSysInviteFilter = 129,
            ActionSysInviteTrans = 130,
            ActionSysPathFinding = 131,
            ActionVipFunctionCheck = 144, // data is flag data << 1UL
            ActionDynaGlobalData = 150,
            ActionSysLimit = 199,

            //NPC
            ActionNpcFirst = 200,
            ActionNpcAttr = 201,
            ActionNpcErase = 205,
            ActionNpcModify = 206,
            ActionNpcResetsynowner = 207,
            ActionNpcFindNextTable = 208,
            ActionNpcFamilyCreate = 218,
            ActionNpcFamilyDestroy = 219,
            ActionNpcFamilyChangeName = 220,
            ActionNpcChangePos = 223,
            ActionNpcLimit = 299,

            // Map
            ActionMapFirst = 300,
            ActionMapMovenpc = 301,
            ActionMapMapuser = 302,
            ActionMapBrocastmsg = 303,
            ActionMapDropitem = 304,
            ActionMapSetstatus = 305,
            ActionMapAttrib = 306,
            ActionMapRegionMonster = 307,
            ActionMapDropMultiItems = 308,
            ActionMapChangeweather = 310,
            ActionMapChangelight = 311,
            ActionMapMapeffect = 312,
            ActionMapFireworks = 314,
            ActionMapFireworks2 = 315,
            ActionMapAbleExp = 332,
            ActionMapLimit = 399,

            // Lay item
            ActionItemonlyFirst = 400,
            ActionItemRequestlaynpc = 401,
            ActionItemCountnpc = 402,
            ActionItemLaynpc = 403,
            ActionItemDelthis = 498,
            ActionItemonlyLimit = 499,

            // Item
            ActionItemFirst = 500,
            ActionItemAdd = 501,
            ActionItemDel = 502,
            ActionItemCheck = 503,
            ActionItemHole = 504,
            ActionItemRepair = 505,
            ActionItemMultidel = 506,
            ActionItemMultichk = 507,
            ActionItemLeavespace = 508,
            ActionItemUpequipment = 509,
            ActionItemEquiptest = 510,
            ActionItemEquipexist = 511,
            ActionItemEquipcolor = 512,
            ActionItemTransform = 513,
            ActionItemCheckrand = 516,
            ActionItemModify = 517,
            ActionItemAdd1 = 518,
            ActionItemDelAll = 519,
            ActionItemJarCreate = 528,
            ActionItemJarVerify = 529,
            ActionItemUnequip = 530,
            ActionItemRefineryAdd = 532,
            ActionItemAdd2 = 542,
            ActionItemCheck2 = 543,
            ActionItemSuperFlag = 544,
            ActionItemWeaponRChangeSubtype = 545,
            ActionItemAddSpecial = 550,
            ActionItemLimit = 599,

            // Dyn NPCs
            ActionNpconlyFirst = 600,
            ActionNpconlyCreatenewPet = 601,
            ActionNpconlyDeletePet = 602,
            ActionNpconlyMagiceffect = 603,
            ActionNpconlyMagiceffect2 = 604,
            ActionNpconlyLimit = 699,

            // Syndicate
            ActionSynFirst = 700,
            ActionSynCreate = 701,
            ActionSynDestroy = 702,
            ActionSynSetAssistant = 705,
            ActionSynClearRank = 706,
            ActionSynChangeLeader = 709,
            ActionSynAntagonize = 711,
            ActionSynClearAntagonize = 712,
            ActionSynAlly = 713,
            ActionSynClearAlly = 714,
            ActionSynAttr = 717,
            ActionSynChangeName = 732,
            ActionSynLimit = 799,

            //Monsters
            ActionMstFirst = 800,
            ActionMstDropitem = 801,
            ActionMstTeamReward = 802,
            ActionMstRefinery = 803,
            ActionMstLimit = 899,

            //User
            ActionUserFirst = 1000,
            ActionUserAttr = 1001,
            ActionUserFull = 1002, // Fill the user attributes. param is the attribute name. life/mana/xp/sp
            ActionUserChgmap = 1003, // Mapid Mapx Mapy savelocation
            ActionUserRecordpoint = 1004, // Records the user location, so he can be teleported back there later.
            ActionUserHair = 1005,
            ActionUserChgmaprecord = 1006,
            ActionUserChglinkmap = 1007,
            ActionUserTransform = 1008,
            ActionUserIspure = 1009,
            ActionUserTalk = 1010,
            ActionUserMagicEffect = 1011,
            ActionUserMagic = 1020,
            ActionUserWeaponskill = 1021,
            ActionUserLog = 1022,
            ActionUserBonus = 1023,
            ActionUserDivorce = 1024,
            ActionUserMarriage = 1025,
            ActionUserSex = 1026,
            ActionUserEffect = 1027,
            ActionUserTaskmask = 1028,
            ActionUserMediaplay = 1029,
            ActionUserSupermanlist = 1030,
            ActionUserAddTitle = 1031,
            ActionUserRemoveTitle = 1032,
            ActionUserCreatemap = 1033,
            ActionUserEnterHome = 1034,
            ActionUserEnterMateHome = 1035,
            ActionUserChkinCard2 = 1036,
            ActionUserChkoutCard2 = 1037,
            ActionUserFlyNeighbor = 1038,
            ActionUserUnlearnMagic = 1039,
            ActionUserRebirth = 1040,
            ActionUserWebpage = 1041,
            ActionUserBbs = 1042,
            ActionUserUnlearnSkill = 1043,
            ActionUserDropMagic = 1044,
            ActionUserFixAttr = 1045,
            ActionUserOpenDialog = 1046,
            ActionUserPointAllot = 1047,
            ActionUserPlusExp = 1048,
            ActionUserDelWpgBadge = 1049,
            ActionUserChkWpgBadge = 1050,
            ActionUserTakestudentexp = 1051,
            ActionUserWhPassword = 1052,
            ActionUserSetWhPassword = 1053,
            ActionUserOpeninterface = 1054,
            ActionUserTaskManager = 1056,
            ActionUserTaskOpe = 1057,
            ActionUserTaskLocaltime = 1058,
            ActionUserTaskFind = 1059,
            ActionUserVarCompare = 1060,
            ActionUserVarDefine = 1061,
            ActionUserVarCompareString = 1062,
            ActionUserVarDefineString = 1063,
            ActionUserVarCalc = 1064,
            ActionUserTestEquipment = 1065,
            ActionUserDailyStcCompare = 1067,
            ActionUserDailyStcOpe = 1068,
            ActionUserExecAction = 1071,
            ActionUserTestPos = 1072,
            ActionUserStcCompare = 1073,
            ActionUserStcOpe = 1074,
            ActionUserDataSync = 1075,
            ActionUserSelectToData = 1077,
            ActionUserStcTimeOperation = 1080,
            ActionUserStcTimeCheck = 1081,
            ActionUserAttachStatus = 1082,
            ActionUserGodTime = 1083,
            ActionUserCalExp = 1084,
            ActionUserLogEvent = 1085,
            ActionUserTimeToExp = 1086,
            ActionUserPureProfessional = 1094,
            ActionSomethingRelatedToRebirth = 1095,
            ActionUserStatusCreate = 1096,
            ActionUserStatusCheck = 1098,

            //User -> Team
            ActionTeamBroadcast = 1101,
            ActionTeamAttr = 1102,
            ActionTeamLeavespace = 1103,
            ActionTeamItemAdd = 1104,
            ActionTeamItemDel = 1105,
            ActionTeamItemCheck = 1106,
            ActionTeamChgmap = 1107,
            ActionTeamChkIsleader = 1501,
            ActionTeamCreateDynamap = 1520,

            ActionFrozenGrottoEntranceChkDays = 1202,
            ActionUserCheckHpFull = 1203,
            ActionUserCheckHpManaFull = 1204,
            // 1205-1215 > Transfer server actions
            ActionIsChangeServerEnable = 1205,
            ActionCheckServerName = 1213,
            ActionIsChangeServerIdle = 1214,
            ActionIsAccountServerNormal = 1215,

            // User -> Events???
            ActionElitePKValidateUser = 1301,
            ActionElitePKUserInscribed = 1302,
            ActionElitePKCheck = 1303,

            ActionTeamPKInscribe = 1311,
            ActionTeamPKExit = 1312,
            ActionTeamPKCheck = 1313,
            ActionTeamPKUnknown1314 = 1314,
            ActionTeamPKUnknown1315 = 1315,

            ActionSkillTeamPKInscribe = 1321,
            ActionSkillTeamPKExit = 1322,
            ActionSkillTeamPKCheck = 1323,
            ActionSkillTeamPKUnknown1324 = 1324,
            ActionSkillTeamPKUnknown1325 = 1325,

            // User -> General
            ActionGeneralLottery = 1508,
            ActionUserRandTrans = 1509,
            ActionUserDecLife = 1510,
            ActionOpenShop = 1511,
            ActionSubclassLearn = 1550,
            ActionSubclassPromotion = 1551,
            ActionSubclassLevel = 1552,
            ActionAchievements = 1554,
            ActionAttachBuffStatus = 1555,
            ActionDetachBuffStatuses = 1556,
            ActionUserReturn = 1557, // data = opt ? ; param iterator index to save the value

            ActionMouseWaitClick = 1650, // 发消息通知客户端点选目标,data=后面操作的action_id，param=[鼠标图片id，对应客户端Cursor.ini的记录]
            ActionMouseJudgeType = 1651, // 判断点选目标的类型 data：1表示点npc，param=‘npc名字’;data：2表示点怪物param=‘怪物id’;data：3表示判断点选玩家性别判断param=‘性别id’ 1男，2女
            ActionMouseClearStatus = 1652, // 清除玩家当前指针选取状态 服务器新增清除玩家当前指针选取状态的action，服务器执行该action后，下发消息给客户端
            ActionMouseDeleteChosen = 1654, // 

            /// <summary>
            /// genuineqi set 3                 >= += == set Talent Status
            /// freecultivateparam              >= += set
            /// </summary>
            ActionJiangHuAttributes = 1705,
            ActionJiangHuInscribed = 1706,
            ActionJiangHuLevel = 1707, // data level to check
            ActionJiangHuExpProtection = 1709,  // param "+= 3600" seconds

            ActionAutoHuntIsActive = 1721,
            ActionCheckUserAttributeLimit = 1723,
            ActionAddProcessActivityTask = 1724,
            ActionAddProcessTaskSchedle = 1725, // Increase the progress of staged tasks (data fill task type)

            ActionUserLimit = 1999,

            //Events
            ActionEventFirst = 2000,
            ActionEventSetstatus = 2001,
            ActionEventDelnpcGenid = 2002,
            ActionEventCompare = 2003,
            ActionEventCompareUnsigned = 2004,
            ActionEventChangeweather = 2005,
            ActionEventCreatepet = 2006,
            ActionEventCreatenewNpc = 2007,
            ActionEventCountmonster = 2008,
            ActionEventDeletemonster = 2009,
            ActionEventBbs = 2010,
            ActionEventErase = 2011,
            ActionEventMapUserChgMap = 2012,
            ActionEventMapUserExeAction = 2013,

            ActionEventRegister = 2050,
            ActionEventExit = 2051,
            ActionEventCmd = 2052,
            ActionEventLimit = 2099,

            //Traps
            ActionTrapFirst = 2100,
            ActionTrapCreate = 2101,
            ActionTrapErase = 2102,
            ActionTrapCount = 2103,
            ActionTrapAttr = 2104,
            ActionTrapTypeDelete = 2105,
            ActionTrapLimit = 2199,

            // Detained Item
            ActionDetainFirst = 2200,
            ActionDetainDialog = 2205,
            ActionDetainLimit = 2299,

            //Wanted
            ActionWantedFirst = 3000,
            ActionWantedNext = 3001,
            ActionWantedName = 3002,
            ActionWantedBonuty = 3003,
            ActionWantedNew = 3004,
            ActionWantedOrder = 3005,
            ActionWantedCancel = 3006,
            ActionWantedModifyid = 3007,
            ActionWantedSuperadd = 3008,
            ActionPolicewantedNext = 3010,
            ActionPolicewantedOrder = 3011,
            ActionPolicewantedCheck = 3012,
            ActionWantedLimit = 3099,

            // Family
            ActionFamilyFirst = 3500,
            ActionFamilyAttr = 3501,
            ActionFamilyMemberAttr = 3510,
            ActionFamilyWarActivityCheck = 3521,
            ActionFamilyWarAuthorityCheck = 3523,
            ActionFamilyWarRegisterCheck = 3524,
            ActionFamilyLast = 3599,

            ActionMountRacingEventReset = 3601,

            // Progress
            ActionProgressBar = 3701,

            ActionCaptureTheFlagCheck = 3901,
            ActionCaptureTheFlagExit = 3902,

            //Magic
            ActionMagicFirst = 4000,
            ActionMagicAttachstatus = 4001,
            ActionMagicAttack = 4002,
            ActionMagicLimit = 4099,

            ActionLuaLinkMain = 20001,
            ActionLuaExecute = 20002,
        }

        public enum OpenWindow
        {
            Compose = 1,
            Craft = 2,
            Warehouse = 4,
            ClanWindow = 64,
            DetainRedeem = 336,
            DetainClaim = 337,
            VipWarehouse = 341,
            Breeding = 368,
            PurificationWindow = 455,
            StabilizationWindow = 459,
            TalismanUpgrade = 347,
            GemComposing = 422,
            OpenSockets = 425,
            Blessing = 426,
            TortoiseGemComposing = 438,
            HorseRacingStore = 464,
            EditCharacterName = 489,
            GarmentTrade = 502,
            DegradeEquipment = 506,
            VerifyPassword = 568,
            SetNewPassword = 569,
            ModifyPassword = 570,
            BrowseAuction = 572,
            EmailInbox = 576,
            EmailIcon = 578,
            GiftRanking = 584,
            FriendRequest = 606,
            JiangHuJoinIcon = 618
        }
    }
}
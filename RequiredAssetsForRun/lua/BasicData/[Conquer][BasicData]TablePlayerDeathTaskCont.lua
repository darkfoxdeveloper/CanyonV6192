----------------------------------------------------------------------------
--Name:		[征服][基础数据]玩家死亡触发关联表.lua
--Purpose:	玩家死亡触发关联表
--Creator: 	郑江文
--Created:	2014/12/18
----------------------------------------------------------------------------
--玩家死亡触发关联表
tUserKilled = {}

--例：
--tUserKilled["tFunction"] = tUserKilled["tFunction"] or {}
--table.insert(tUserKilled["tFunction"],func)

-- 玩家复活触发关联表
tUserSave = {}
--例：
--tUserSave["tFunction"] = tUserSave["tFunction"] or {}
--table.insert(tUserSave["tFunction"],func)

-- 玩家锁魂触发关联表
tKeepGhost = {}
--例：
--tKeepGhost["tFunction"] = tKeepGhost["tFunction"] or {}
--table.insert(tKeepGhost["tFunction"],func)

-- 玩家解锁触发关联表
tClearKeepGhost = {}
--例：
--tClearKeepGhost["tFunction"] = tClearKeepGhost["tFunction"] or {}
--table.insert(tClearKeepGhost["tFunction"],func)

-- 个人排位赛：	赢场：
tArenicWins = {}
--例：
--tArenicWins["tFunction"] = tArenicWins["tFunction"] or {}
--table.insert(tArenicWins["tFunction"],func)

-- 个人排位赛：	参赛场
tArenicCompetes = {}
--例：
--tArenicCompetes["tFunction"] = tArenicCompetes["tFunction"] or {}
--table.insert(tArenicCompetes["tFunction"],func)

-- 组队排位赛：	赢场：
tTeamArenicWins = {}
--例：
--tTeamArenicWins["tFunction"] = tTeamArenicWins["tFunction"] or {}
--table.insert(tTeamArenicWins["tFunction"],func)

-- 组队排位赛：	参赛场
tTeamArenicCompetes = {}
--例：
--tTeamArenicCompetes["tFunction"] = tTeamArenicCompetes["tFunction"] or {}
--table.insert(tTeamArenicCompetes["tFunction"],func)

-- 骑宠，玩家冲过终点是触发
tRideArrive = {}
--例：
--tRideArrive["tFunction"] = tRideArrive["tFunction"] or {}
--table.insert(tRideArrive["tFunction"],func)

-- 武功，每次修炼时触发：
tTrainGongFu = {}
--例：
--tTrainGongFu["tFunction"] = tTrainGongFu["tFunction"] or {}
--table.insert(tTrainGongFu["tFunction"],func)

-- 服务器启动完成触发：
tServerStart = {}
--例：
--tServerStart["tFunction"] = tServerStart["tFunction"] or {}
--table.insert(tServerStart["tFunction"],func)

-- 添加内功转换
tStrengthExchange = {}
--例：
--tStrengthExchange["tFunction"] = tStrengthExchange["tFunction"] or {}
--table.insert(tStrengthExchange["tFunction"],func)
----------------------------------------------------------------------------
--Name:		[征服][基础数据]Trap任务关联表.lua
--Purpose:	Trap任务关联表
--Creator: 	郑江文
--Created:	2014/12/10
----------------------------------------------------------------------------
--陷阱关联表
--tTrap[陷阱ID]={任务ID,...}
--定义
tTrap = {}


--[[
tTrap[陷阱ID]["Function"]								--触发陷阱后调用的函数


--]]

--例：
--tTrap[2080] = tTrap[2080] or {}
--tTrap[2080]["Function"] = function (nTrapId,nTrapType)
	
--end
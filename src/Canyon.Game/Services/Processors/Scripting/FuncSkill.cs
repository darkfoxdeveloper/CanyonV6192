using Canyon.Game.Scripting.Attributes;
using Canyon.Game.Services.Managers;
using Canyon.Game.States.Magics;
using Canyon.Game.States.User;
using System.Reflection.Emit;

namespace Canyon.Game.Services.Processors.Scripting
{
    public sealed partial class LuaProcessor
    {
        [LuaFunction]
        public bool MagicCheckLev(int userId, int magicType, int magicLevel)
        {
            Character user = GetUser(userId);
            if (user == null)
            {
                return false;
            }

            return user.MagicData.CheckLevel((ushort)magicType, (ushort)magicLevel);
        }

        [LuaFunction]
        public bool MagicCheckType(int userId, int magicType)
        {
            Character user = GetUser(userId);
            if (user == null)
            {
                return false;
            }
            return user.MagicData[(ushort)magicType] != null;
        }

        [LuaFunction]
        public bool LearnMagic(int userId, int magicType)
        {
            Character user = GetUser(userId);
            if (user == null)
            {
                return false;
            }

            return user.MagicData.CreateAsync((ushort)magicType, 0).GetAwaiter().GetResult();
        }

        [LuaFunction]
        public bool MagicUpLev(int userId, int magicType)
        {
            Character user = GetUser(userId);
            if (user == null)
            {
                return false;
            }

            return user.MagicData.UpLevelByTaskAsync((ushort)magicType).GetAwaiter().GetResult();
        }

        [LuaFunction]
        public bool MagicAddExp(int userId, int magicType, int exp)
        {
            Character user = GetUser(userId);
            if (user?.MagicData[(ushort)magicType] == null)
            {
                return false;
            }
            return user.MagicData.AwardExpAsync(0, 0, exp, user.MagicData[(ushort)magicType]).GetAwaiter().GetResult();
        }

        [LuaFunction]
        public bool MagicAddLevTime(int userId, int magicType, int expLevTime)
        {
            Character user = GetUser(userId);
            if (user == null)
            {
                return false;
            }

            Magic magic = user.MagicData[(ushort)magicType];
            if (magic == null)
            {
                return false;
            }

            // TODO
            logger.LogWarning("MagicAddLevTime(int userId, int magicType, int expLevTime) not implemented");
            return false;
        }
    }
}

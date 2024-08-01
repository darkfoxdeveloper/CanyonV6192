using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Syndicates;
using Canyon.Game.States.User;

namespace Canyon.Game.States
{
    public sealed class Tutor
    {
        public const int BETRAYAL_FLAG_TIMEOUT = 60 * 60 * 24 * 3;
        public const int STUDENT_BETRAYAL_VALUE = 50000;

        private readonly TimeOut betrayalCheck = new();

        private DbTutor tutor;
        private DbTutorContribution access;

        private Tutor()
        {
        }

        public static async Task<Tutor> CreateAsync(DbTutor tutor)
        {
            var guide = new Tutor
            {
                tutor = tutor,
                access = await TutorContributionRepository.GetGuideAsync(tutor.StudentId)
            };
            guide.access ??= new DbTutorContribution
            {
                TutorIdentity = tutor.GuideId,
                StudentIdentity = tutor.StudentId
            };

            DbCharacter dbMentor = await CharacterRepository.FindByIdentityAsync(tutor.GuideId);
            if (dbMentor == null)
            {
                return null;
            }

            guide.GuideName = dbMentor.Name;

            dbMentor = await CharacterRepository.FindByIdentityAsync(tutor.StudentId);
            if (dbMentor == null)
            {
                return null;
            }

            guide.StudentName = dbMentor.Name;

            if (guide.Betrayed)
            {
                guide.betrayalCheck.Startup(60);
            }

            return guide;
        }

        public uint GuideIdentity => tutor.GuideId;
        public string GuideName { get; private set; }

        public uint StudentIdentity => tutor.StudentId;
        public string StudentName { get; private set; }

        public bool Betrayed => tutor.BetrayalFlag != 0;
        public bool BetrayalCheck => Betrayed && betrayalCheck.IsActive() && betrayalCheck.ToNextTime();

        public Character Guide => RoleManager.GetUser(tutor.GuideId);
        public Character Student => RoleManager.GetUser(tutor.StudentId);

        public async Task<bool> AwardTutorExperienceAsync(uint addExpTime)
        {
            access.Experience += addExpTime;

            Character user = RoleManager.GetUser(access.TutorIdentity);
            if (user != null)
            {
                user.MentorExpTime += addExpTime;
            }
            else
            {
                DbTutorAccess tutorAccess = await TutorAccessRepository.GetAsync(access.TutorIdentity);
                tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = GuideIdentity
                };
                tutorAccess.Experience += addExpTime;
                await ServerDbContext.SaveAsync(tutorAccess);
            }

            return await SaveAsync();
        }

        public async Task<bool> AwardTutorGodTimeAsync(ushort addGodTime)
        {
            access.GodTime += addGodTime;

            Character user = RoleManager.GetUser(access.TutorIdentity);
            if (user != null)
            {
                user.MentorGodTime += addGodTime;
            }
            else
            {
                DbTutorAccess tutorAccess = await TutorAccessRepository.GetAsync(access.TutorIdentity);
                tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = GuideIdentity
                };
                tutorAccess.Blessing += addGodTime;
                await ServerDbContext.SaveAsync(tutorAccess);
            }

            return await SaveAsync();
        }

        public async Task<bool> AwardOpportunityAsync(ushort addTime)
        {
            access.PlusStone += addTime;

            Character user = RoleManager.GetUser(access.TutorIdentity);
            if (user != null)
            {
                user.MentorAddLevexp += addTime;
            }
            else
            {
                DbTutorAccess tutorAccess = await TutorAccessRepository.GetAsync(access.TutorIdentity);
                tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = GuideIdentity
                };
                tutorAccess.Composition += addTime;
                await ServerDbContext.SaveAsync(tutorAccess);
            }

            return await SaveAsync();
        }

        public int SharedBattlePower
        {
            get
            {
                Character mentor = Guide;
                Character student = Student;
                if (mentor == null || student == null)
                {
                    return 0;
                }

                if (mentor.PureBattlePower < student.PureBattlePower)
                {
                    return 0;
                }

                DbTutorBattleLimitType limit = TutorManager.GetTutorBattleLimitType(student.PureBattlePower);
                if (limit == null)
                {
                    return 0;
                }

                DbTutorType type = TutorManager.GetTutorType(mentor.Level);
                if (type == null)
                {
                    return 0;
                }

                return (int)Math.Min(limit.BattleLevelLimit,
                                      (mentor.PureBattlePower - student.PureBattlePower) *
                                      (type.BattleLevelShare / 100f));
            }
        }

        public async Task BetrayAsync()
        {
            tutor.BetrayalFlag = UnixTimestamp.Now;
            Student?.Guide?.SetBetray();
            await SaveAsync();
        }

        public void SetBetray()
        {
            tutor.BetrayalFlag = UnixTimestamp.Now;
        }

        public async Task SendTutorAsync()
        {
            if (Student == null)
            {
                return;
            }

            int betrayalHours = 0;
            if (Betrayed)
            {
                betrayalHours = (int)(48 - (DateTime.Now - UnixTimestamp.ToDateTime(tutor.BetrayalFlag)).TotalHours);
            }

            await Student.SendAsync(new MsgGuideInfo
            {
                Identity = StudentIdentity,
                Level = Guide?.Level ?? 0,
                Blessing = access.GodTime,
                Composition = (ushort)access.PlusStone,
                Experience = access.Experience,
                IsOnline = Guide != null,
                Mesh = Guide?.Mesh ?? 0,
                Mode = MsgGuideInfo.RequestMode.Mentor,
                Syndicate = Guide?.SyndicateIdentity ?? 0,
                SyndicatePosition = (ushort)(Guide?.SyndicateRank ?? SyndicateMember.SyndicateRank.None),
                Names = new List<string>
                {
                    GuideName,
                    StudentName,
                    Guide?.MateName ?? StrNone
                },
                EnroleDate = uint.Parse(UnixTimestamp.ToDateTime((int)tutor.Date).ToString("yyyyMMdd") ?? "0"),
                PkPoints = Guide?.PkPoints ?? 0,
                Profession = Guide?.Profession ?? 0,
                SharedBattlePower = (uint)SharedBattlePower,
                SenderIdentity = GuideIdentity,
                BetrayHour = (uint)(Betrayed ? betrayalHours : 999999)
            });
        }

        public async Task SendStudentAsync()
        {
            if (Guide == null)
            {
                return;
            }

            int betrayalHours = 0;
            if (Betrayed)
            {
                betrayalHours = (int)(48 - (DateTime.Now - UnixTimestamp.ToDateTime(tutor.BetrayalFlag)).TotalHours);
            }

            await Guide.SendAsync(new MsgGuideInfo
            {
                Identity = StudentIdentity,
                Level = Student?.Level ?? 0,
                Blessing = access.GodTime,
                Composition = (ushort)access.PlusStone,
                Experience = access.Experience,
                IsOnline = Student != null,
                Mesh = Student?.Mesh ?? 0,
                Mode = MsgGuideInfo.RequestMode.Apprentice,
                Syndicate = Student?.SyndicateIdentity ?? 0,
                SyndicatePosition = (ushort)(Student?.SyndicateRank ?? SyndicateMember.SyndicateRank.None),
                Names = new List<string>
                {
                    GuideName,
                    StudentName,
                    Student?.MateName ?? StrNone
                },
                EnroleDate = uint.Parse(UnixTimestamp.ToDateTime((int)tutor.Date).ToString("yyyyMMdd") ?? "0"),
                PkPoints = Student?.PkPoints ?? 0,
                Profession = Student?.Profession ?? 0,
                SharedBattlePower = 0,
                SenderIdentity = GuideIdentity,
                BetrayHour = (uint)(Betrayed ? betrayalHours : 999999)
            });
        }

        public async Task BetrayalTimerAsync()
        {
            /*
             * Since this will be called in a queue, it might be called twice per run, so we will trigger the TimeOut
             * to see it can be checked.
             */
            if (tutor.BetrayalFlag != 0)
            {
                if (tutor.BetrayalFlag + BETRAYAL_FLAG_TIMEOUT < UnixTimestamp.Now) // expired, leave mentor
                {
                    if (Guide != null)
                    {
                        await Guide.SendAsync(string.Format(StrGuideExpelTutor, StudentName));
                        Guide.RemoveApprentice(StudentIdentity);
                    }

                    if (Student != null)
                    {
                        await Student.SendAsync(string.Format(StrGuideExpelStudent, GuideName));
                        await Student.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower, 0, 0);
                        Student.Guide = null;
                    }

                    await DeleteAsync();
                }
            }

            if (betrayalCheck.IsActive())
            {
                betrayalCheck.Update();
            }
        }

        public async Task<bool> SaveAsync()
        {
            return await ServerDbContext.SaveAsync(tutor) && await ServerDbContext.SaveAsync(access);
        }

        public async Task<bool> DeleteAsync()
        {
            await ServerDbContext.DeleteAsync(tutor);
            await ServerDbContext.DeleteAsync(access);
            return true;
        }
    }
}

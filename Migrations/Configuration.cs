using FluentMY.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Migrations;
using System.Linq;

namespace FluentMY.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<FluentMY.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "FluentMY.Models.ApplicationDbContext";
        }

        protected override void Seed(FluentMY.Models.ApplicationDbContext context)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

            SeedRoles(roleManager);
            SeedAdmin(userManager);

            var fluentMyEducator = SeedFluentMyEducator(userManager);

            BackfillSeedKeys(context);

            SeedWebsiteOwnedCourses(context, fluentMyEducator.Id);
            SeedBahasaMalaysiaStarterOfficialPack(context, fluentMyEducator.Id);

            context.SaveChanges();
        }

        private void SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Student", "Educator", "Admin" };

            foreach (var role in roles)
            {
                if (!roleManager.RoleExists(role))
                {
                    roleManager.Create(new IdentityRole(role));
                }
            }
        }

        private void SeedAdmin(UserManager<ApplicationUser> userManager)
        {
            string adminEmail = "admin@fluentmy.com";
            string adminPassword = "Admin@12345";

            var adminUser = userManager.FindByEmail(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    JoinedAt = DateTime.UtcNow
                };

                var result = userManager.Create(adminUser, adminPassword);

                if (!result.Succeeded)
                {
                    throw new Exception("Failed to create default admin: " + string.Join("; ", result.Errors));
                }
            }

            if (!userManager.IsInRole(adminUser.Id, "Admin"))
            {
                userManager.AddToRole(adminUser.Id, "Admin");
            }
        }

        private ApplicationUser SeedFluentMyEducator(UserManager<ApplicationUser> userManager)
        {
            string email = "fluentmy.educator@fluentmy.my";
            string password = "FluentMY@12345";

            var educatorUser = userManager.FindByEmail(email);

            if (educatorUser == null)
            {
                educatorUser = new ApplicationUser
                {
                    UserName = "fluentmy.educator",
                    Email = email,
                    FullName = "FluentMY Content Team",
                    EmailConfirmed = true,
                    JoinedAt = DateTime.UtcNow
                };

                var result = userManager.Create(educatorUser, password);

                if (!result.Succeeded)
                {
                    throw new Exception("Failed to create FluentMY educator: " + string.Join("; ", result.Errors));
                }
            }

            if (!userManager.IsInRole(educatorUser.Id, "Educator"))
            {
                userManager.AddToRole(educatorUser.Id, "Educator");
            }

            return educatorUser;
        }

        private void BackfillSeedKeys(ApplicationDbContext context)
        {
            var dailyEnglish = context.Courses.FirstOrDefault(c => c.Title == "Daily English" && c.SeedKey == null);
            if (dailyEnglish != null) dailyEnglish.SeedKey = "course.daily-english";

            var mandarinBasics = context.Courses.FirstOrDefault(c => c.Title == "Mandarin Basics" && c.SeedKey == null);
            if (mandarinBasics != null) mandarinBasics.SeedKey = "course.mandarin-basics";

            var tamilStarter = context.Courses.FirstOrDefault(c => c.Title == "Tamil Starter" && c.SeedKey == null);
            if (tamilStarter != null) tamilStarter.SeedKey = "course.tamil-starter";

            var bmStarter = context.Courses.FirstOrDefault(c => c.Title == "Bahasa Malaysia Starter" && c.SeedKey == null);
            if (bmStarter != null) bmStarter.SeedKey = "course.bm-starter";

            context.SaveChanges();

            var bmStarterCourse = context.Courses.FirstOrDefault(c => c.SeedKey == "course.bm-starter");
            if (bmStarterCourse != null)
            {
                var lesson1 = context.Lessons.FirstOrDefault(l =>
                    l.CourseId == bmStarterCourse.CourseId &&
                    l.Title == "Vocabulary and Grammar Basics" &&
                    l.SeedKey == null);
                if (lesson1 != null) lesson1.SeedKey = "lesson.bm-starter.vocab-grammar";

                var lesson2 = context.Lessons.FirstOrDefault(l =>
                    l.CourseId == bmStarterCourse.CourseId &&
                    l.Title == "Game Practice and Listening" &&
                    l.SeedKey == null);
                if (lesson2 != null) lesson2.SeedKey = "lesson.bm-starter.game-listening";

                var lesson3 = context.Lessons.FirstOrDefault(l =>
                    l.CourseId == bmStarterCourse.CourseId &&
                    l.Title == "Speaking Practice" &&
                    l.SeedKey == null);
                if (lesson3 != null) lesson3.SeedKey = "lesson.bm-starter.speaking";

                context.SaveChanges();

                if (lesson1 != null)
                {
                    BackfillExerciseSeedKey(context, lesson1.LessonId, "MultipleChoice", 1, "exercise.bm-starter.lesson1.mcq-ba");
                    BackfillExerciseSeedKey(context, lesson1.LessonId, "MultipleChoice", 2, "exercise.bm-starter.lesson1.mcq-home");
                    BackfillExerciseSeedKey(context, lesson1.LessonId, "FillBlank", 3, "exercise.bm-starter.lesson1.fill-makan");
                    BackfillExerciseSeedKey(context, lesson1.LessonId, "FillBlank", 4, "exercise.bm-starter.lesson1.fill-buku");
                    BackfillExerciseSeedKey(context, lesson1.LessonId, "MultipleSelect", 5, "exercise.bm-starter.lesson1.multi-read");
                }

                if (lesson2 != null)
                {
                    BackfillExerciseSeedKey(context, lesson2.LessonId, "Matching", 1, "exercise.bm-starter.lesson2.matching");
                    BackfillExerciseSeedKey(context, lesson2.LessonId, "Reorder", 2, "exercise.bm-starter.lesson2.reorder");
                    BackfillExerciseSeedKey(context, lesson2.LessonId, "ListeningChoice", 3, "exercise.bm-starter.lesson2.listening");
                }

                if (lesson3 != null)
                {
                    BackfillExerciseSeedKey(context, lesson3.LessonId, "Speaking", 1, "exercise.bm-starter.lesson3.speaking-word");
                    BackfillExerciseSeedKey(context, lesson3.LessonId, "Speaking", 2, "exercise.bm-starter.lesson3.speaking-sentence");
                }
            }

            context.SaveChanges();
        }

        private void BackfillExerciseSeedKey(ApplicationDbContext context, int lessonId, string exerciseType, int displayOrder, string seedKey)
        {
            var exercise = context.Exercises.FirstOrDefault(e =>
                e.LessonId == lessonId &&
                e.ExerciseType == exerciseType &&
                e.DisplayOrder == displayOrder &&
                e.SeedKey == null);

            if (exercise != null)
            {
                exercise.SeedKey = seedKey;
            }
        }

        private void SeedWebsiteOwnedCourses(ApplicationDbContext context, string educatorId)
        {
            AddCourseIfMissing(
                context,
                "course.daily-english",
                "Daily English",
                "English",
                "Mixed Level",
                "2-3 weeks",
                "Improve everyday English for study, work, and communication.",
                educatorId);

            AddCourseIfMissing(
                context,
                "course.mandarin-basics",
                "Mandarin Basics",
                "Mandarin",
                "Beginner",
                "2 weeks",
                "Start with pronunciation, simple dialogues, and basic characters.",
                educatorId);

            AddCourseIfMissing(
                context,
                "course.tamil-starter",
                "Tamil Starter",
                "Tamil",
                "Beginner",
                "2 weeks",
                "Learn common words and phrases used in daily Malaysian life.",
                educatorId);
        }

        private Course AddCourseIfMissing(
            ApplicationDbContext context,
            string seedKey,
            string title,
            string language,
            string level,
            string estimatedDuration,
            string description,
            string educatorId)
        {
            var course = context.Courses.FirstOrDefault(c => c.SeedKey == seedKey);

            if (course != null)
            {
                return course;
            }

            course = new Course
            {
                SeedKey = seedKey,
                Title = title,
                Language = language,
                Level = level,
                EstimatedDuration = estimatedDuration,
                Description = description,
                EducatorId = educatorId,
                CreatedAt = DateTime.UtcNow
            };

            context.Courses.Add(course);
            context.SaveChanges();

            return course;
        }

        private void SeedBahasaMalaysiaStarterOfficialPack(ApplicationDbContext context, string educatorId)
        {
            var starterCourse = AddCourseIfMissing(
                context,
                "course.bm-starter",
                "Bahasa Malaysia Starter",
                "Bahasa Melayu",
                "Beginner / A1",
                "1 week",
                "BM: Kursus rasmi FluentMY untuk kosa kata, tatabahasa, mendengar, permainan dan sebutan.\n\n" +
                "EN: Official FluentMY course for vocabulary, grammar, listening, game practice, and speaking.\n\n" +
                "中文：FluentMY 官方课程，用于词汇、语法、听力、游戏练习与口语测试。",
                educatorId);

            var lesson1 = AddLessonIfMissing(
                context,
                "lesson.bm-starter.vocab-grammar",
                starterCourse.CourseId,
                educatorId,
                "Vocabulary and Grammar Basics",
                "A1",
                "BM:\n" +
                "Dalam pelajaran ini, pelajar akan berlatih kosa kata dan tatabahasa asas.\n\n" +
                "1. Kosa kata mudah\n" +
                "- basikal\n- buku\n- rumah\n- susu\n\n" +
                "2. Ayat mudah\n" +
                "- Saya makan nasi.\n- Ali membaca buku.\n- Ini rumah saya.\n\n" +
                "EN:\n" +
                "In this lesson, students will practice basic vocabulary and grammar.\n\n" +
                "1. Simple vocabulary\n" +
                "- bicycle\n- book\n- house\n- milk\n\n" +
                "2. Simple sentences\n" +
                "- I eat rice.\n- Ali reads a book.\n- This is my house.\n\n" +
                "中文：\n" +
                "在这一课中，学生将练习基础词汇和语法。\n\n" +
                "1. 简单词汇\n" +
                "- 脚踏车\n- 书\n- 房子\n- 牛奶\n\n" +
                "2. 简单句子\n" +
                "- 我吃饭。\n- Ali 读书。\n- 这是我的家。"
            );

            var lesson2 = AddLessonIfMissing(
                context,
                "lesson.bm-starter.game-listening",
                starterCourse.CourseId,
                educatorId,
                "Game Practice and Listening",
                "A1",
                "BM:\n" +
                "Dalam pelajaran ini, pelajar akan bermain aktiviti bahasa berbentuk latihan interaktif.\n\n" +
                "1. Matching\nPadankan suku kata dengan perkataan yang betul.\n\n" +
                "2. Reorder\nSusun perkataan supaya menjadi ayat yang lengkap.\n\n" +
                "3. Listening\nDengar audio dan pilih jawapan yang betul.\n\n" +
                "EN:\n" +
                "In this lesson, students will do interactive language game practice.\n\n" +
                "1. Matching\nMatch syllables with the correct words.\n\n" +
                "2. Reorder\nRearrange words into a complete sentence.\n\n" +
                "3. Listening\nListen to the audio and choose the correct answer.\n\n" +
                "中文：\n" +
                "在这一课中，学生将通过互动游戏练习语言。\n\n" +
                "1. 配对\n把音节和正确词语配对。\n\n" +
                "2. 句子排序\n把词语排列成完整句子。\n\n" +
                "3. 听力\n听音频并选择正确答案。"
            );

            var lesson3 = AddLessonIfMissing(
                context,
                "lesson.bm-starter.speaking",
                starterCourse.CourseId,
                educatorId,
                "Speaking Practice",
                "A1",
                "BM:\n" +
                "Dalam pelajaran ini, pelajar akan berlatih sebutan.\n\n" +
                "1. Sebut perkataan dengan jelas.\n" +
                "2. Ikut panduan suku kata.\n" +
                "3. Dengar audio contoh jika ada.\n" +
                "4. Cuba beberapa kali untuk memperbaiki sebutan.\n\n" +
                "EN:\n" +
                "In this lesson, students will practice pronunciation.\n\n" +
                "1. Pronounce the word clearly.\n" +
                "2. Follow the syllable guide.\n" +
                "3. Listen to the model audio if available.\n" +
                "4. Try several times to improve pronunciation.\n\n" +
                "中文：\n" +
                "在这一课中，学生将练习发音。\n\n" +
                "1. 清楚地读出单词。\n" +
                "2. 跟着音节提示。\n" +
                "3. 如果有示范音频，可以先听。\n" +
                "4. 多试几次来改善发音。"
            );

            AddExerciseIfMissing(
                context,
                "exercise.bm-starter.lesson1.mcq-ba",
                lesson1.LessonId,
                "MultipleChoice",
                1,
                "BM: Pilih perkataan yang bermula dengan suku kata \"ba\".\nEN: Choose the word that starts with the syllable \"ba\".\n中文：选择以音节“ba”开头的词语。",
                "Vocabulary",
                "A",
                "basikal",
                "buku",
                "rumah",
                "susu",
                null,
                null,
                null,
                null,
                null,
                1,
                10,
                true
            );

            AddExerciseIfMissing(
                context,
                "exercise.bm-starter.lesson1.mcq-home",
                lesson1.LessonId,
                "MultipleChoice",
                2,
                "BM: Pilih perkataan yang merujuk kepada tempat tinggal.\nEN: Choose the word that refers to a place to live.\n中文：选择表示居住地方的词语。",
                "Vocabulary",
                "B",
                "susu",
                "rumah",
                "basikal",
                "buku",
                null,
                null,
                null,
                null,
                null,
                1,
                10,
                true
            );

            AddExerciseIfMissing(
                context,
                "exercise.bm-starter.lesson1.fill-makan",
                lesson1.LessonId,
                "FillBlank",
                3,
                "BM: Isi tempat kosong: Saya ___ nasi.\nEN: Fill in the blank: I ___ rice.\n中文：填空：我 ___ 饭。",
                "Grammar",
                "makan",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                1,
                12,
                true
            );

            AddExerciseIfMissing(
                context,
                "exercise.bm-starter.lesson1.fill-buku",
                lesson1.LessonId,
                "FillBlank",
                4,
                "BM: Isi tempat kosong: Ali membaca ___.\nEN: Fill in the blank: Ali reads a ___.\n中文：填空：Ali 读 ___。",
                "Grammar",
                "buku",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                1,
                12,
                true
            );

            AddExerciseIfMissing(
                context,
                "exercise.bm-starter.lesson1.multi-read",
                lesson1.LessonId,
                "MultipleSelect",
                5,
                "BM: Pilih semua benda yang boleh dibaca.\nEN: Choose all items that can be read.\n中文：选择所有可以阅读的东西。",
                "Vocabulary",
                "A,B",
                "buku",
                "surat",
                "susu",
                "basikal",
                null,
                null,
                null,
                null,
                null,
                2,
                15,
                true
            );

            AddExerciseIfMissing(
                context,
                "exercise.bm-starter.lesson2.matching",
                lesson2.LessonId,
                "Matching",
                1,
                "BM: Padankan suku kata dengan perkataan yang betul.\nEN: Match the syllable with the correct word.\n中文：把音节和正确词语配对。",
                "Vocabulary",
                null,
                null,
                null,
                null,
                null,
                "{\"ba\":\"basikal\",\"bu\":\"buku\",\"ru\":\"rumah\",\"su\":\"susu\"}",
                null,
                null,
                null,
                null,
                2,
                15,
                true
            );

            AddExerciseIfMissing(
                context,
                "exercise.bm-starter.lesson2.reorder",
                lesson2.LessonId,
                "Reorder",
                2,
                "BM: Susun perkataan supaya menjadi ayat yang betul.\nEN: Arrange the words into the correct sentence.\n中文：把词语排列成正确句子。",
                "Grammar",
                null,
                null,
                null,
                null,
                null,
                null,
                "[\"Saya\",\"makan\",\"nasi\"]",
                null,
                null,
                null,
                2,
                15,
                true
            );

            AddExerciseIfMissing(
                context,
                "exercise.bm-starter.lesson2.listening",
                lesson2.LessonId,
                "ListeningChoice",
                3,
                "BM: Dengar audio dan pilih perkataan yang betul.\nEN: Listen to the audio and choose the correct word.\n中文：听音频并选择正确的词语。",
                "Listening",
                "A",
                "basikal",
                "bola",
                "baju",
                "buku",
                null,
                null,
                null,
                null,
                null,
                2,
                20,
                true
            );

            AddExerciseIfMissing(
                context,
                "exercise.bm-starter.lesson3.speaking-word",
                lesson3.LessonId,
                "Speaking",
                1,
                "BM: Sebut perkataan ini dengan jelas.\nEN: Pronounce this word clearly.\n中文：请清楚地读出这个词。",
                "Speaking",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "basikal",
                "ba-si-kal",
                2,
                25,
                true,
                "BM: Sebut setiap suku kata dengan jelas.\nEN: Pronounce each syllable clearly.\n中文：清楚读出每个音节。"
            );

            AddExerciseIfMissing(
                context,
                "exercise.bm-starter.lesson3.speaking-sentence",
                lesson3.LessonId,
                "Speaking",
                2,
                "BM: Baca ayat ini dengan lancar dan jelas.\nEN: Read this sentence clearly and fluently.\n中文：请流利并清楚地读出这句话。",
                "Speaking",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "Saya makan nasi.",
                "sa-ya / ma-kan / na-si",
                3,
                30,
                true,
                "BM: Baca perlahan dahulu, kemudian ulang dengan lebih lancar.\nEN: Read slowly first, then repeat more fluently.\n中文：先慢慢读，再更流利地重读一次。"
            );

            context.SaveChanges();
        }

        private Lesson AddLessonIfMissing(
            ApplicationDbContext context,
            string seedKey,
            int courseId,
            string educatorId,
            string title,
            string difficultyLevel,
            string content)
        {
            var lesson = context.Lessons.FirstOrDefault(l => l.SeedKey == seedKey);

            if (lesson != null)
            {
                return lesson;
            }

            lesson = new Lesson
            {
                SeedKey = seedKey,
                CourseId = courseId,
                EducatorId = educatorId,
                Title = title,
                DifficultyLevel = difficultyLevel,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            context.Lessons.Add(lesson);
            context.SaveChanges();

            return lesson;
        }

        private void AddExerciseIfMissing(
            ApplicationDbContext context,
            string seedKey,
            int lessonId,
            string exerciseType,
            int displayOrder,
            string promptText,
            string practiceArea,
            string correctAnswer,
            string optionA,
            string optionB,
            string optionC,
            string optionD,
            string matchingPairsJson,
            string sentenceTokensJson,
            string audioPromptPath,
            string referenceText,
            string syllableGuide,
            int marks,
            int xpReward,
            bool allowRetry,
            string pronunciationHint = null)
        {
            var exists = context.Exercises.Any(e => e.SeedKey == seedKey);

            if (exists)
            {
                return;
            }

            var exercise = new Exercise
            {
                SeedKey = seedKey,
                LessonId = lessonId,
                PracticeArea = practiceArea,
                ExerciseType = exerciseType,
                PromptText = promptText,
                QuestionText = promptText,
                CorrectAnswer = correctAnswer,
                OptionA = optionA,
                OptionB = optionB,
                OptionC = optionC,
                OptionD = optionD,
                MatchingPairsJson = matchingPairsJson,
                SentenceTokensJson = sentenceTokensJson,
                AudioPromptPath = audioPromptPath,
                ReferenceText = referenceText,
                SyllableGuide = syllableGuide,
                PronunciationHint = pronunciationHint,
                Marks = marks,
                DisplayOrder = displayOrder,
                XPReward = xpReward,
                AllowRetry = allowRetry,
                CreatedAt = DateTime.UtcNow
            };

            context.Exercises.Add(exercise);
        }
    }
}
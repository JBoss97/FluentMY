using FluentMY.Models;
using FluentMY.ViewModels;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Educator")]
    public class EducatorExercisesController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public async Task<ActionResult> Index(int lessonId)
        {
            var educatorId = User.Identity.GetUserId();

            var lesson = await db.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId && l.EducatorId == educatorId);

            if (lesson == null)
                return HttpNotFound();

            var exercises = await db.Exercises
                .Where(e => e.LessonId == lessonId)
                .OrderBy(e => e.DisplayOrder)
                .ThenByDescending(e => e.UpdatedAt ?? e.CreatedAt)
                .ThenBy(e => e.ExerciseId)
                .ToListAsync();

            ViewBag.LessonId = lesson.LessonId;
            ViewBag.LessonTitle = lesson.Title;
            ViewBag.CourseTitle = lesson.Course != null ? lesson.Course.Title : "";
            ViewBag.CourseId = lesson.CourseId;

            return View(exercises);
        }

        [HttpGet]
        public async Task<ActionResult> Create(int lessonId)
        {
            var lesson = await GetOwnedLessonAsync(lessonId);
            if (lesson == null)
                return HttpNotFound();

            var nextDisplayOrder = await db.Exercises
                .Where(e => e.LessonId == lessonId)
                .Select(e => (int?)e.DisplayOrder)
                .MaxAsync() ?? 0;

            var model = new EducatorExerciseEditViewModel
            {
                LessonId = lessonId,
                PracticeArea = "Vocabulary",
                ExerciseType = "MultipleChoice",
                Marks = 1,
                XPReward = 10,
                AllowRetry = true,
                DisplayOrder = nextDisplayOrder + 1
            };

            await PopulateEditorContextAsync(lesson, model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(EducatorExerciseEditViewModel model)
        {
            var lesson = await GetOwnedLessonAsync(model.LessonId);
            if (lesson == null)
                return HttpNotFound();

            NormalizeModel(model);
            ValidateAudioUpload(model.AudioPromptFile);
            ValidateExerciseModel(model);

            if (model.AudioPromptFile != null && model.AudioPromptFile.ContentLength > 0)
            {
                try
                {
                    model.AudioPromptPath = SaveAudioPromptFile(model.AudioPromptFile);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("AudioPromptFile", ex.Message);
                }
            }

            ValidateAudioRequirement(model);

            if (!ModelState.IsValid)
            {
                await PopulateEditorContextAsync(lesson, model);
                return View(model);
            }

            var entity = new Exercise
            {
                LessonId = model.LessonId,
                CreatedAt = DateTime.UtcNow
            };

            ApplyToEntity(entity, model);

            db.Exercises.Add(entity);
            await db.SaveChangesAsync();

            TempData["ExerciseMessage"] = "Exercise created successfully.";
            return RedirectToAction("Index", new { lessonId = model.LessonId });
        }

        [HttpGet]
        public async Task<ActionResult> Edit(int id)
        {
            var exercise = await db.Exercises
                .Include(e => e.Lesson)
                .Include(e => e.Lesson.Course)
                .FirstOrDefaultAsync(e => e.ExerciseId == id);

            if (exercise == null)
                return HttpNotFound();

            var educatorId = User.Identity.GetUserId();
            if (exercise.Lesson == null || exercise.Lesson.EducatorId != educatorId)
                return HttpNotFound();

            var model = new EducatorExerciseEditViewModel
            {
                ExerciseId = exercise.ExerciseId,
                LessonId = exercise.LessonId,
                PracticeArea = exercise.PracticeArea,
                ExerciseType = string.IsNullOrWhiteSpace(exercise.ExerciseType) ? "MultipleChoice" : exercise.ExerciseType,
                PromptText = string.IsNullOrWhiteSpace(exercise.PromptText) ? exercise.QuestionText : exercise.PromptText,
                OptionA = exercise.OptionA,
                OptionB = exercise.OptionB,
                OptionC = exercise.OptionC,
                OptionD = exercise.OptionD,
                CorrectAnswer = exercise.CorrectAnswer,
                MatchingPairsJson = exercise.MatchingPairsJson,
                SentenceTokensJson = exercise.SentenceTokensJson,
                AudioPromptPath = exercise.AudioPromptPath,
                ExistingAudioPromptPath = exercise.AudioPromptPath,
                ReferenceText = exercise.ReferenceText,
                SyllableGuide = exercise.SyllableGuide,
                PronunciationHint = exercise.PronunciationHint,
                Marks = exercise.Marks,
                DisplayOrder = exercise.DisplayOrder,
                XPReward = exercise.XPReward,
                AllowRetry = exercise.AllowRetry
            };

            await PopulateEditorContextAsync(exercise.Lesson, model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EducatorExerciseEditViewModel model)
        {
            var existingExercise = await db.Exercises
                .Include(e => e.Lesson)
                .Include(e => e.Lesson.Course)
                .FirstOrDefaultAsync(e => e.ExerciseId == model.ExerciseId);

            if (existingExercise == null)
                return HttpNotFound();

            var educatorId = User.Identity.GetUserId();
            if (existingExercise.Lesson == null || existingExercise.Lesson.EducatorId != educatorId)
                return HttpNotFound();

            NormalizeModel(model);
            ValidateAudioUpload(model.AudioPromptFile);
            ValidateExerciseModel(model);

            string newUploadedAudioPath = null;

            if (model.AudioPromptFile != null && model.AudioPromptFile.ContentLength > 0)
            {
                try
                {
                    newUploadedAudioPath = SaveAudioPromptFile(model.AudioPromptFile);
                    model.AudioPromptPath = newUploadedAudioPath;
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("AudioPromptFile", ex.Message);
                }
            }
            else
            {
                model.AudioPromptPath = existingExercise.AudioPromptPath;
            }

            model.ExistingAudioPromptPath = existingExercise.AudioPromptPath;
            ValidateAudioRequirement(model);

            if (!ModelState.IsValid)
            {
                await PopulateEditorContextAsync(existingExercise.Lesson, model);
                return View(model);
            }

            var oldAudioPath = existingExercise.AudioPromptPath;

            ApplyToEntity(existingExercise, model);
            existingExercise.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(newUploadedAudioPath) &&
                !string.IsNullOrWhiteSpace(oldAudioPath) &&
                !string.Equals(newUploadedAudioPath, oldAudioPath, StringComparison.OrdinalIgnoreCase))
            {
                DeleteAudioFileIfExists(oldAudioPath);
            }

            TempData["ExerciseMessage"] = "Exercise updated successfully.";
            return RedirectToAction("Index", new { lessonId = existingExercise.LessonId });
        }

        [HttpGet]
        public async Task<ActionResult> Delete(int id)
        {
            var exercise = await db.Exercises
                .Include(e => e.Lesson)
                .Include(e => e.Lesson.Course)
                .FirstOrDefaultAsync(e => e.ExerciseId == id);

            if (exercise == null)
                return HttpNotFound();

            var educatorId = User.Identity.GetUserId();
            if (exercise.Lesson == null || exercise.Lesson.EducatorId != educatorId)
                return HttpNotFound();

            return View(exercise);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var exercise = await db.Exercises
                .Include(e => e.Lesson)
                .FirstOrDefaultAsync(e => e.ExerciseId == id);

            if (exercise == null)
                return HttpNotFound();

            var educatorId = User.Identity.GetUserId();
            if (exercise.Lesson == null || exercise.Lesson.EducatorId != educatorId)
                return HttpNotFound();

            int lessonId = exercise.LessonId;

            bool hasSubmissionAnswers = await db.SubmissionAnswers.AnyAsync(a => a.ExerciseId == id);
            bool hasSpeakingAttempts = await db.SpeakingAttempts.AnyAsync(a => a.ExerciseId == id);

            if (hasSubmissionAnswers || hasSpeakingAttempts)
            {
                TempData["ExerciseMessage"] = "You cannot delete an exercise that has already been used in student activity.";
                return RedirectToAction("Index", new { lessonId });
            }

            var oldAudioPath = exercise.AudioPromptPath;

            db.Exercises.Remove(exercise);
            await db.SaveChangesAsync();

            DeleteAudioFileIfExists(oldAudioPath);

            TempData["ExerciseMessage"] = "Exercise deleted successfully.";
            return RedirectToAction("Index", new { lessonId });
        }

        private void ApplyToEntity(Exercise entity, EducatorExerciseEditViewModel model)
        {
            entity.PracticeArea = model.PracticeArea;
            entity.ExerciseType = model.ExerciseType;
            entity.PromptText = model.PromptText;
            entity.QuestionText = model.PromptText;

            entity.Marks = model.Marks;
            entity.DisplayOrder = model.DisplayOrder;
            entity.XPReward = model.XPReward;
            entity.AllowRetry = model.AllowRetry;
            entity.AudioPromptPath = model.AudioPromptPath;

            var type = NormalizeKey(model.ExerciseType);

            entity.OptionA = null;
            entity.OptionB = null;
            entity.OptionC = null;
            entity.OptionD = null;
            entity.CorrectAnswer = null;
            entity.MatchingPairsJson = null;
            entity.SentenceTokensJson = null;
            entity.ReferenceText = null;
            entity.SyllableGuide = null;
            entity.PronunciationHint = null;

            switch (type)
            {
                case "multiplechoice":
                case "listeningchoice":
                    entity.OptionA = model.OptionA;
                    entity.OptionB = model.OptionB;
                    entity.OptionC = model.OptionC;
                    entity.OptionD = model.OptionD;
                    entity.CorrectAnswer = NormalizeChoiceAnswer(model.CorrectAnswer, false);
                    break;

                case "multipleselect":
                    entity.OptionA = model.OptionA;
                    entity.OptionB = model.OptionB;
                    entity.OptionC = model.OptionC;
                    entity.OptionD = model.OptionD;
                    entity.CorrectAnswer = NormalizeChoiceAnswer(model.CorrectAnswer, true);
                    break;

                case "fillblank":
                    entity.CorrectAnswer = model.CorrectAnswer;
                    break;

                case "matching":
                    entity.MatchingPairsJson = model.MatchingPairsJson;
                    break;

                case "reorder":
                    entity.SentenceTokensJson = model.SentenceTokensJson;
                    break;

                case "speaking":
                    entity.ReferenceText = model.ReferenceText;
                    entity.SyllableGuide = model.SyllableGuide;
                    entity.PronunciationHint = model.PronunciationHint;
                    break;
            }
        }

        private void NormalizeModel(EducatorExerciseEditViewModel model)
        {
            if (model == null) return;

            model.PracticeArea = model.PracticeArea?.Trim();
            model.ExerciseType = model.ExerciseType?.Trim();
            model.PromptText = model.PromptText?.Trim();

            model.OptionA = model.OptionA?.Trim();
            model.OptionB = model.OptionB?.Trim();
            model.OptionC = model.OptionC?.Trim();
            model.OptionD = model.OptionD?.Trim();

            model.CorrectAnswer = model.CorrectAnswer?.Trim();
            model.MatchingPairsJson = model.MatchingPairsJson?.Trim();
            model.SentenceTokensJson = model.SentenceTokensJson?.Trim();
            model.AudioPromptPath = model.AudioPromptPath?.Trim();
            model.ReferenceText = model.ReferenceText?.Trim();
            model.SyllableGuide = model.SyllableGuide?.Trim();
            model.PronunciationHint = model.PronunciationHint?.Trim();
        }

        private void ValidateChoiceOptions(EducatorExerciseEditViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.OptionA))
                ModelState.AddModelError("OptionA", "Option A is required.");

            if (string.IsNullOrWhiteSpace(model.OptionB))
                ModelState.AddModelError("OptionB", "Option B is required.");

            if (string.IsNullOrWhiteSpace(model.OptionC))
                ModelState.AddModelError("OptionC", "Option C is required.");

            if (string.IsNullOrWhiteSpace(model.OptionD))
                ModelState.AddModelError("OptionD", "Option D is required.");
        }

        private string NormalizeChoiceAnswer(string value, bool allowMultiple)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            var parts = value
                .Split(',')
                .Select(x => (x ?? "").Trim().ToUpperInvariant())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (!allowMultiple)
                return parts.FirstOrDefault() ?? "";

            return string.Join(",", parts);
        }

        private bool IsValidSingleChoiceAnswer(string value)
        {
            return new[] { "A", "B", "C", "D" }.Contains((value ?? "").Trim().ToUpperInvariant());
        }

        private bool IsValidMultipleSelectAnswer(string value)
        {
            var parts = (value ?? "")
                .Split(',')
                .Select(x => (x ?? "").Trim().ToUpperInvariant())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (!parts.Any())
                return false;

            return parts.All(x => new[] { "A", "B", "C", "D" }.Contains(x));
        }

        private void ValidateExerciseModel(EducatorExerciseEditViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.PracticeArea))
                ModelState.AddModelError("PracticeArea", "Please select a practice area.");

            if (string.IsNullOrWhiteSpace(model.ExerciseType))
                ModelState.AddModelError("ExerciseType", "Please select an exercise type.");

            if (string.IsNullOrWhiteSpace(model.PromptText))
                ModelState.AddModelError("PromptText", "Prompt / question is required.");

            if (model.Marks <= 0)
                ModelState.AddModelError("Marks", "Marks must be greater than 0.");

            if (model.XPReward < 0)
                ModelState.AddModelError("XPReward", "XP reward cannot be negative.");

            if (model.DisplayOrder < 0)
                ModelState.AddModelError("DisplayOrder", "Display order cannot be negative.");

            var type = NormalizeKey(model.ExerciseType);

            switch (type)
            {
                case "multiplechoice":
                case "listeningchoice":
                    ValidateChoiceOptions(model);
                    model.CorrectAnswer = NormalizeChoiceAnswer(model.CorrectAnswer, false);

                    if (!IsValidSingleChoiceAnswer(model.CorrectAnswer))
                    {
                        ModelState.AddModelError("CorrectAnswer", "Correct answer must be A, B, C, or D.");
                    }
                    break;

                case "multipleselect":
                    ValidateChoiceOptions(model);
                    model.CorrectAnswer = NormalizeChoiceAnswer(model.CorrectAnswer, true);

                    if (!IsValidMultipleSelectAnswer(model.CorrectAnswer))
                    {
                        ModelState.AddModelError("CorrectAnswer", "For Multiple Select, use values like A,C or B,D.");
                    }
                    break;

                case "fillblank":
                    if (string.IsNullOrWhiteSpace(model.CorrectAnswer))
                        ModelState.AddModelError("CorrectAnswer", "Correct answer is required for fill in the blank.");
                    break;

                case "matching":
                    if (string.IsNullOrWhiteSpace(model.MatchingPairsJson))
                    {
                        ModelState.AddModelError("MatchingPairsJson", "Matching pairs JSON is required.");
                    }
                    else
                    {
                        try
                        {
                            serializer.Deserialize<Dictionary<string, string>>(model.MatchingPairsJson);
                        }
                        catch
                        {
                            ModelState.AddModelError("MatchingPairsJson", "Matching pairs JSON format is invalid.");
                        }
                    }
                    break;

                case "reorder":
                    if (string.IsNullOrWhiteSpace(model.SentenceTokensJson))
                    {
                        ModelState.AddModelError("SentenceTokensJson", "Sentence tokens JSON is required.");
                    }
                    else
                    {
                        try
                        {
                            serializer.Deserialize<List<string>>(model.SentenceTokensJson);
                        }
                        catch
                        {
                            ModelState.AddModelError("SentenceTokensJson", "Sentence tokens JSON format is invalid.");
                        }
                    }
                    break;

                case "speaking":
                    if (string.IsNullOrWhiteSpace(model.ReferenceText))
                        ModelState.AddModelError("ReferenceText", "Reference text is required for speaking exercises.");
                    break;
            }
        }

        private void ValidateAudioRequirement(EducatorExerciseEditViewModel model)
        {
            var type = NormalizeKey(model.ExerciseType);

            if (type == "listeningchoice" && string.IsNullOrWhiteSpace(model.AudioPromptPath))
            {
                ModelState.AddModelError("AudioPromptFile", "An audio file is required for Listening + Choice exercises.");
            }
        }

        private void ValidateAudioUpload(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength <= 0)
                return;

            var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a" };
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("AudioPromptFile", "Only MP3, WAV, OGG, and M4A files are allowed.");
            }

            if (file.ContentLength > 10 * 1024 * 1024)
            {
                ModelState.AddModelError("AudioPromptFile", "Audio file size must be 10MB or less.");
            }
        }

        private string SaveAudioPromptFile(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength <= 0)
                return null;

            var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a" };
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
                throw new InvalidOperationException("Only MP3, WAV, OGG, and M4A files are allowed.");

            var folderPath = Server.MapPath("~/Content/audio-prompts/");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = Guid.NewGuid().ToString("N") + extension;
            var fullPath = Path.Combine(folderPath, fileName);

            file.SaveAs(fullPath);

            return "~/Content/audio-prompts/" + fileName;
        }

        private void DeleteAudioFileIfExists(string virtualPath)
        {
            if (string.IsNullOrWhiteSpace(virtualPath))
                return;

            try
            {
                var fullPath = Server.MapPath(virtualPath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch
            {
            }
        }

        private string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? ""
                : value.Trim().ToLowerInvariant();
        }

        private async Task PopulateEditorContextAsync(Lesson lesson, EducatorExerciseEditViewModel model)
        {
            ViewBag.LessonTitle = lesson.Title;
            ViewBag.CourseTitle = lesson.Course != null ? lesson.Course.Title : "";

            ViewBag.PracticeAreas = new SelectList(new[]
            {
                new { Value = "Vocabulary", Text = "Vocabulary" },
                new { Value = "Grammar", Text = "Grammar" },
                new { Value = "Listening", Text = "Listening" },
                new { Value = "Speaking", Text = "Speaking" }
            }, "Value", "Text", model.PracticeArea);

            ViewBag.ExerciseTypes = new SelectList(new[]
            {
                new { Value = "MultipleChoice", Text = "Multiple Choice (Single Answer)" },
                new { Value = "MultipleSelect", Text = "Multiple Select (Multiple Answers)" },
                new { Value = "FillBlank", Text = "Fill in the Blank" },
                new { Value = "Matching", Text = "Matching" },
                new { Value = "Reorder", Text = "Sentence Reorder" },
                new { Value = "ListeningChoice", Text = "Listening + Choice" },
                new { Value = "Speaking", Text = "Speaking" }
            }, "Value", "Text", model.ExerciseType);

            await Task.CompletedTask;
        }

        private async Task<Lesson> GetOwnedLessonAsync(int lessonId)
        {
            var educatorId = User.Identity.GetUserId();

            return await db.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId && l.EducatorId == educatorId);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
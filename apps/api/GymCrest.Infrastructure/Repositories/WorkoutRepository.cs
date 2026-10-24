using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class WorkoutRepository : IWorkoutRepository
    {
        private readonly AppDbContext _dbContext;

        public WorkoutRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<string>?> GetCategories()
        {
            List<string> categories = await _dbContext.Exercises
                .Select(e => e.Category)
                .Distinct()
                .ToListAsync();

            if (categories == null || categories.Count == 0)
                return null;

            return [.. categories.OrderBy(c => c)];
        }

        public async Task<List<Exercise>> GetExercises(string? category, string? equipment, string? search)
        {
            IQueryable<Exercise> query = _dbContext.Exercises.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(e => e.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(equipment))
            {
                query = query.Where(e => e.Equipment.ToLower() == equipment.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.Name.ToLower().Contains(search.ToLower()));
            }

            return await query.OrderBy(e => e.Name).ToListAsync();
        }

        public async Task<List<Exercise>> GetExercisesByCategory(string category)
        {
            return await _dbContext.Exercises
                .AsNoTracking()
                .Where(e => e.Category.ToLower() == category.ToLower())
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        public async Task<Exercise?> GetExerciseBySlug(string slug)
        {
            Exercise? exercise = await _dbContext.Exercises
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Slug.ToLower() == slug.ToLower());

            return exercise;
        }

        public async Task<Dictionary<string, string>> GetCategoriesForNamesAsync(IEnumerable<string> names)
        {
            List<string> lowerNames = [.. names.Select(n => n.Trim().ToLower()).Distinct()];
            Dictionary<string, string> result = [];

            void AddMatches(IEnumerable<(string Name, string Category)> matches)
            {
                foreach ((string name, string category) in matches)
                {
                    string key = name.Trim().ToLower();
                    if (!string.IsNullOrWhiteSpace(category) && !result.ContainsKey(key))
                        result[key] = category;
                }
            }

            AddMatches((await _dbContext.Exercises
                .AsNoTracking()
                .Where(e => lowerNames.Contains(e.Name.ToLower()))
                .Select(e => new { e.Name, e.Category })
                .ToListAsync()).Select(e => (e.Name, e.Category)));

            List<string> remaining = [.. lowerNames.Where(n => !result.ContainsKey(n))];
            if (remaining.Count > 0)
            {
                AddMatches((await _dbContext.WorkoutPlanExercises
                    .AsNoTracking()
                    .Where(p => p.Exercise != null && remaining.Contains(p.ExerciseName.ToLower()))
                    .Select(p => new { p.ExerciseName, p.Exercise!.Category })
                    .ToListAsync()).Select(p => (p.ExerciseName, p.Category)));
            }

            remaining = [.. lowerNames.Where(n => !result.ContainsKey(n))];
            if (remaining.Count > 0)
            {
                AddMatches((await _dbContext.MasterExercises
                    .AsNoTracking()
                    .Where(m => remaining.Contains(m.Name.ToLower()))
                    .Select(m => new { m.Name, m.Category })
                    .ToListAsync()).Select(m => (m.Name, m.Category)));
            }

            return result;
        }
    }
}

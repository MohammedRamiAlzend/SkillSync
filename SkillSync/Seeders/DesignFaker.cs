using Bogus;
using SkillSync.Data.Entities;

namespace SkillSync.Seeders
{
    public static class DesignFaker
    {
        private static Faker<Design> _designFaker = new Faker<Design>()
            .RuleFor(d => d.Id, f => 0)


            .RuleFor(d => d.UserId, f => f.Random.Int(1, 5))

            .RuleFor(d => d.Title, f => f.Commerce.ProductAdjective() + " " + f.Commerce.Product())

            .RuleFor(d => d.Description, f => f.Lorem.Sentences(2))

            .RuleFor(d => d.Status, f => f.PickRandom(new[] { "Pending", "Reviewed", "Approved", "Rejected" }))

            .RuleFor(d => d.CreatedAt, f => f.Date.Past(1))
            .RuleFor(d => d.UpdatedAt, f => f.Date.Past(0).OrNull(f, 0.5f)) 
            .RuleFor(d => d.IsDeleted, f => f.Random.Bool(0.1f)); 


        public static List<Design> Generate(int count)
        {
            return _designFaker.Generate(count);
        }
    }
}
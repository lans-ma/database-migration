namespace Kata.Db.Console.Model
{
    using System;

    public class User : Entity
    {
        public string Login { get; set; }
        public string Email { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public decimal Height { get; set; }
    }
}
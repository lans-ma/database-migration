namespace Kata.Db.Console.Model
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Rental : Entity
    {
        public DateTime RentalDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        public Book Book { get; set; }

        public User User { get; set; }
    }
}

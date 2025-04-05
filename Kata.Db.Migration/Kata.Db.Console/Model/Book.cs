namespace Kata.Db.Console.Model
{
    using System;

    public class Book : Entity
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string ISBN { get; set; }
        public DateTime PublishedDate { get; set; }
    }
}
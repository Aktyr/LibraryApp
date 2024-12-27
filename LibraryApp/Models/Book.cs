using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LibraryApp.Models
{
    public class Book(string name, string autor, int year, string publisher)
    {
        public Guid id {  get; set; } = Guid.NewGuid();
        public string name { get; set; } = name;
        public string autor { get; set; } = autor;
        public int year { get; set; } = year;
        public string publisher { get; set; } = publisher;


        public override string ToString() =>
          $@"Название: {name}
Автор: {autor}
Год: {year}\n"; //Копий на складе: {bookCount}";
    
    
    }
}
